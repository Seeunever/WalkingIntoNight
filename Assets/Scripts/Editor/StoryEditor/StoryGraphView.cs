using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using WalkingIntoNight.TRPG.Narrative;

namespace WalkingIntoNight.TRPG.Editor
{
    public class StoryNodeView : Node
    {
        public StoryNodeData Data { get; }
        public Port InputPort { get; }
        public Port NextPort { get; }
        public Port SuccessPort { get; }
        public Port FailurePort { get; }
        readonly StoryGraphView m_graph;

        public StoryNodeView(StoryGraphView graph, StoryNodeData data)
        {
            m_graph = graph;
            Data = data;
            title = string.IsNullOrEmpty(data.id) ? "(no id)" : data.id;
            viewDataKey = data.id;

            var type = StoryNodeTypeParser.Parse(data.type);
            titleContainer.style.backgroundColor = GetTypeColor(type);

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            if (type == StoryNodeType.Check)
            {
                SuccessPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                SuccessPort.portName = "Success";
                outputContainer.Add(SuccessPort);

                FailurePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                FailurePort.portName = "Failure";
                outputContainer.Add(FailurePort);
            }
            else
            {
                NextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                NextPort.portName = "Next";
                outputContainer.Add(NextPort);
            }

            RefreshSummary(type);
            RefreshExpandedState();
            RefreshPorts();

            this.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 1)
                    m_graph.SelectNode(Data);
            });
        }

        static Color GetTypeColor(StoryNodeType type)
        {
            switch (type)
            {
                case StoryNodeType.Check: return new Color(0.45f, 0.25f, 0.15f);
                case StoryNodeType.Combat: return new Color(0.5f, 0.15f, 0.15f);
                case StoryNodeType.GiveItem: return new Color(0.2f, 0.4f, 0.25f);
                case StoryNodeType.SetFlag: return new Color(0.25f, 0.3f, 0.45f);
                case StoryNodeType.Location: return new Color(0.2f, 0.35f, 0.4f);
                case StoryNodeType.AdvanceTime: return new Color(0.35f, 0.3f, 0.2f);
                case StoryNodeType.End: return new Color(0.3f, 0.15f, 0.3f);
                default: return new Color(0.22f, 0.22f, 0.28f);
            }
        }

        public void RefreshSummary(StoryNodeType? typeOverride = null)
        {
            var type = typeOverride ?? StoryNodeTypeParser.Parse(Data.type);
            var summary = type.ToString();
            if (!string.IsNullOrEmpty(Data.speaker)) summary += $" | {Data.speaker}";
            if (!string.IsNullOrEmpty(Data.text))
            {
                var t = Data.text.Length > 40 ? Data.text.Substring(0, 40) + "..." : Data.text;
                summary += $"\n{t}";
            }
            if (type == StoryNodeType.Check && !string.IsNullOrEmpty(Data.skillId))
                summary += $"\n[{Data.skillId} diff={Data.difficulty}]";

            var label = this.Q<Label>("summary");
            if (label == null)
            {
                label = new Label { name = "summary" };
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.fontSize = 10;
                label.style.marginTop = 4;
                extensionContainer.Add(label);
            }
            label.text = summary;
        }
    }

    public class StoryGraphView : GraphView
    {
        readonly StoryEditorWindow m_window;
        readonly Dictionary<string, StoryNodeView> m_nodeViews = new Dictionary<string, StoryNodeView>();

        public StoryGraphView(StoryEditorWindow window)
        {
            m_window = window;
            SetupZoom(0.15f, 2f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphViewChanged;

            var menu = new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("创建/对话 dialogue", _ => CreateNode("dialogue", evt.localMousePosition));
                evt.menu.AppendAction("创建/检定 check", _ => CreateNode("check", evt.localMousePosition));
                evt.menu.AppendAction("创建/旗标 setflag", _ => CreateNode("setflag", evt.localMousePosition));
                evt.menu.AppendAction("创建/物品 giveitem", _ => CreateNode("giveitem", evt.localMousePosition));
                evt.menu.AppendAction("创建/地点 location", _ => CreateNode("location", evt.localMousePosition));
                evt.menu.AppendAction("创建/时间 advancetime", _ => CreateNode("advancetime", evt.localMousePosition));
                evt.menu.AppendAction("创建/战斗 combat", _ => CreateNode("combat", evt.localMousePosition));
                evt.menu.AppendAction("创建/结局 end", _ => CreateNode("end", evt.localMousePosition));
            });
            this.AddManipulator(menu);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(p =>
                p.node != startPort.node &&
                p.direction != startPort.direction).ToList();
        }

        public void Rebuild()
        {
            m_nodeViews.Clear();
            DeleteElements(graphElements.ToList());

            var project = m_window.Project;
            if (project?.scenario?.nodes == null) return;

            var positions = project.GetPositionMap();
            var x = 0f;
            foreach (var node in project.scenario.nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) continue;
                if (!positions.TryGetValue(node.id, out var pos))
                {
                    pos = new Vector2(x, 0);
                    x += 280;
                }
                AddNodeView(node, pos);
            }

            BuildEdges();
        }

        void AddNodeView(StoryNodeData data, Vector2 pos)
        {
            var view = new StoryNodeView(this, data);
            view.SetPosition(new Rect(pos, new Vector2(220, 0)));
            AddElement(view);
            m_nodeViews[data.id] = view;
        }

        void BuildEdges()
        {
            foreach (var pair in m_nodeViews)
            {
                var view = pair.Value;
                var data = view.Data;
                var type = StoryNodeTypeParser.Parse(data.type);

                if (type == StoryNodeType.Check)
                {
                    Connect(view.SuccessPort, data.successNodeId);
                    Connect(view.FailurePort, data.failureNodeId);
                }
                else
                {
                    Connect(view.NextPort, data.nextNodeId);
                    if (data.choices != null)
                    {
                        foreach (var choice in data.choices)
                        {
                            if (choice != null)
                                Connect(view.NextPort, choice.nextNodeId);
                        }
                    }
                }
            }
        }

        void Connect(Port fromPort, string targetId)
        {
            if (fromPort == null || string.IsNullOrEmpty(targetId)) return;
            if (!m_nodeViews.TryGetValue(targetId, out var targetView)) return;
            var edge = fromPort.ConnectTo(targetView.InputPort);
            AddElement(edge);
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                    ApplyEdge(edge);
            }

            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                        ClearEdge(edge);
                }
            }

            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is StoryNodeView nodeView)
                    {
                        var rect = nodeView.GetPosition();
                        m_window.Project.SetPosition(nodeView.Data.id, rect.position);
                    }
                }
            }

            return change;
        }

        void ApplyEdge(Edge edge)
        {
            if (edge.output?.node is not StoryNodeView source) return;
            if (edge.input?.node is not StoryNodeView target) return;

            if (source.SuccessPort == edge.output)
                source.Data.successNodeId = target.Data.id;
            else if (source.FailurePort == edge.output)
                source.Data.failureNodeId = target.Data.id;
            else
                source.Data.nextNodeId = target.Data.id;
        }

        void ClearEdge(Edge edge)
        {
            if (edge.output?.node is not StoryNodeView source) return;
            if (edge.input?.node is not StoryNodeView target) return;
            var targetId = target.Data.id;

            if (source.SuccessPort == edge.output && source.Data.successNodeId == targetId)
                source.Data.successNodeId = null;
            else if (source.FailurePort == edge.output && source.Data.failureNodeId == targetId)
                source.Data.failureNodeId = null;
            else if (source.Data.nextNodeId == targetId)
                source.Data.nextNodeId = null;
            else if (source.Data.choices != null)
            {
                foreach (var choice in source.Data.choices)
                {
                    if (choice != null && choice.nextNodeId == targetId)
                        choice.nextNodeId = null;
                }
            }
        }

        void CreateNode(string type, Vector2 localPos)
        {
            var id = GenerateNodeId(type);
            var node = new StoryNodeData
            {
                id = id,
                type = type,
                speaker = type == "check" ? "检定" : "旁白",
                text = "",
                choices = new List<StoryChoiceData>()
            };
            if (type == "check")
            {
                node.skillId = "spot_hidden";
                node.difficulty = 0;
            }

            m_window.Project.scenario.nodes.Add(node);
            var worldPos = contentViewContainer.WorldToLocal(localPos);
            m_window.Project.SetPosition(id, worldPos);
            AddNodeView(node, worldPos);
            m_window.SelectNode(node);
        }

        string GenerateNodeId(string type)
        {
            var prefix = type + "_";
            var index = 1;
            var ids = new HashSet<string>(m_window.Project.scenario.nodes.Select(n => n.id));
            while (ids.Contains(prefix + index)) index++;
            return prefix + index;
        }

        public StoryNodeView FindNodeView(string id)
        {
            return id != null && m_nodeViews.TryGetValue(id, out var view) ? view : null;
        }

        public void RefreshNode(StoryNodeData node)
        {
            var view = FindNodeView(node.id);
            view?.RefreshSummary();
        }

        public void DeleteNode(StoryNodeData node)
        {
            var view = FindNodeView(node.id);
            if (view != null)
                DeleteElements(new GraphElement[] { view });
            m_window.Project.scenario.nodes.Remove(node);
            m_window.Project.editorMeta.nodePositions?.RemoveAll(e => e.id == node.id);
        }

        public void SelectNode(StoryNodeData node)
        {
            m_window.SelectNode(node);
        }
    }
}
