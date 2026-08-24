using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Narrative;

namespace WalkingIntoNight.TRPG.Editor
{
    public class StoryEditorWindow : EditorWindow
    {
        public StoryProjectData Project { get; private set; } = new StoryProjectData();
        public StoryGraphView GraphView { get; private set; }
        StoryNodeInspector m_inspector;
        EntityListPanel m_entityPanel;
        string m_scenarioId = ScenarioRegistry.DefaultScenarioId;

        [MenuItem("WalkingIntoNight/剧情编辑器")]
        public static void Open()
        {
            var window = GetWindow<StoryEditorWindow>();
            window.titleContent = new GUIContent("剧情编辑器");
            window.minSize = new Vector2(1100, 600);
            window.Show();
        }

        void OnEnable()
        {
            LoadProject();
            BuildUI();
        }

        void BuildUI()
        {
            rootVisualElement.Clear();

            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = 4, paddingBottom = 4, paddingLeft = 8, paddingRight = 8,
                    backgroundColor = new Color(0.18f, 0.18f, 0.2f)
                }
            };

            toolbar.Add(new Label("剧本:") { style = { marginRight = 4 } });
            var scenarioField = new TextField { value = m_scenarioId };
            scenarioField.style.width = 140;
            scenarioField.RegisterValueChangedCallback(evt => m_scenarioId = evt.newValue);
            toolbar.Add(scenarioField);

            toolbar.Add(MakeToolbarButton("重新加载", () => { LoadProject(); RebuildAll(); }));
            toolbar.Add(MakeToolbarButton("保存", SaveProject));
            toolbar.Add(MakeToolbarButton("验证", ValidateProject));

            if (Project?.scenario != null)
            {
                toolbar.Add(new Label($" | {Project.scenario.title} | 节点 {Project.scenario.nodes?.Count ?? 0}")
                    { style = { marginLeft = 12, color = new Color(0.75f, 0.75f, 0.75f) } });
            }

            rootVisualElement.Add(toolbar);

            var body = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };
            rootVisualElement.Add(body);

            m_entityPanel = new EntityListPanel(this);
            m_entityPanel.style.width = 260;
            m_entityPanel.style.borderRightWidth = 1;
            m_entityPanel.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f);
            body.Add(m_entityPanel);

            GraphView = new StoryGraphView(this);
            GraphView.style.flexGrow = 1;
            body.Add(GraphView);

            var rightPanel = new VisualElement { style = { width = 300, flexShrink = 0 } };
            rightPanel.style.borderLeftWidth = 1;
            rightPanel.style.borderLeftColor = new Color(0.15f, 0.15f, 0.15f);
            rightPanel.Add(new Label("节点 Inspector") { style = { unityFontStyleAndWeight = FontStyle.Bold, paddingTop = 6, paddingLeft = 8 } });
            m_inspector = new StoryNodeInspector(this);
            m_inspector.style.flexGrow = 1;
            rightPanel.Add(m_inspector);
            body.Add(rightPanel);

            GraphView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == GraphView)
                    SelectNode(null);
            });

            RebuildAll();
        }

        static Button MakeToolbarButton(string text, System.Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.marginLeft = 4;
            return btn;
        }

        void LoadProject()
        {
            Project = StoryJsonIO.Load(m_scenarioId);
            if (string.IsNullOrEmpty(Project.scenario.title))
                Project.scenario.title = m_scenarioId;
        }

        void RebuildAll()
        {
            GraphView?.Rebuild();
            m_entityPanel?.Refresh();
            m_inspector?.Bind(null);
        }

        void SaveProject()
        {
            SyncNodePositions();
            SyncLocationNpcLinks();
            StoryJsonIO.Save(Project);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("保存", "已写入 Assets/Resources/Data/", "确定");
        }

        void ValidateProject()
        {
            var issues = StoryValidator.Validate(Project);
            var hasError = issues.Exists(i => i.isError);
            EditorUtility.DisplayDialog(hasError ? "验证失败" : "验证完成", StoryValidator.FormatIssues(issues), "确定");
        }

        void SyncNodePositions()
        {
            if (GraphView == null) return;
            foreach (var element in GraphView.graphElements)
            {
                if (element is StoryNodeView nodeView)
                {
                    var rect = nodeView.GetPosition();
                    Project.SetPosition(nodeView.Data.id, rect.position);
                }
            }
        }

        void SyncLocationNpcLinks()
        {
            foreach (var loc in Project.locations)
            {
                if (loc.npcIds == null) loc.npcIds = new System.Collections.Generic.List<string>();
            }

            foreach (var npc in Project.npcs)
            {
                if (npc.locationIds == null) continue;
                foreach (var locId in npc.locationIds)
                {
                    var loc = Project.locations.Find(l => l.id == locId);
                    if (loc != null && !loc.npcIds.Contains(npc.id))
                        loc.npcIds.Add(npc.id);
                }
            }
        }

        public void SelectNode(StoryNodeData node)
        {
            m_inspector?.Bind(node);
        }
    }
}
