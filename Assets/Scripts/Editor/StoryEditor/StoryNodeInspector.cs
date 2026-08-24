using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Narrative;

namespace WalkingIntoNight.TRPG.Editor
{
    public class StoryNodeInspector : VisualElement
    {
        readonly StoryEditorWindow m_window;
        ScrollView m_scroll;
        StoryNodeData m_node;

        public StoryNodeInspector(StoryEditorWindow window)
        {
            m_window = window;
            m_scroll = new ScrollView { style = { flexGrow = 1 } };
            Add(m_scroll);
        }

        public void Bind(StoryNodeData node)
        {
            m_node = node;
            m_scroll.Clear();
            if (node == null)
            {
                m_scroll.Add(new Label("未选中节点"));
                return;
            }

            AddField("ID", node.id, v => { node.id = v; RefreshGraphNode(); });
            AddEnumField("类型", node.type, v => { node.type = v; RefreshGraphNode(); },
                "dialogue", "check", "setflag", "giveitem", "changesan", "combat", "location", "npchub", "advancetime", "end");
            AddField("说话者", node.speaker, v => node.speaker = v);
            AddTextArea("文本", node.text, v => node.text = v);
            AddField("portraitId", node.portraitId, v => node.portraitId = v);
            AddField("locationId", node.locationId, v => node.locationId = v);
            AddField("nextNodeId", node.nextNodeId, v => node.nextNodeId = v);

            var type = StoryNodeTypeParser.Parse(node.type);
            if (type == StoryNodeType.Check)
            {
                m_scroll.Add(new Label("— 检定 —") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8 } });
                AddEnumField("skillId", node.skillId, v => node.skillId = v, CharacterCreator.CoreSkillIds);
                AddIntField("difficulty (0=Regular)", node.difficulty, v => node.difficulty = v);
                AddField("successNodeId", node.successNodeId, v => node.successNodeId = v);
                AddField("failureNodeId", node.failureNodeId, v => node.failureNodeId = v);
                AddIntField("bonusDice", node.bonusDice, v => node.bonusDice = v);
                AddIntField("penaltyDice", node.penaltyDice, v => node.penaltyDice = v);
            }

            if (type == StoryNodeType.SetFlag)
            {
                AddField("flag", node.flag, v => node.flag = v);
                AddToggle("flagValue", node.flagValue, v => node.flagValue = v);
                AddField("玩家提示", node.flagNotice, v => node.flagNotice = v);
            }

            if (type == StoryNodeType.GiveItem)
            {
                AddField("itemId", node.itemId, v => node.itemId = v);
                AddIntField("itemCount", node.itemCount, v => node.itemCount = v);
            }

            if (type == StoryNodeType.ChangeSan)
                AddIntField("sanDelta", node.sanDelta, v => node.sanDelta = v);

            if (type == StoryNodeType.Combat)
            {
                AddField("combatId", node.combatId, v => node.combatId = v);
                AddField("winNodeId", node.winNodeId, v => node.winNodeId = v);
                AddField("loseNodeId", node.loseNodeId, v => node.loseNodeId = v);
                AddField("fleeNodeId", node.fleeNodeId, v => node.fleeNodeId = v);
            }

            if (type == StoryNodeType.AdvanceTime)
            {
                AddIntField("advancePeriods", node.advancePeriods, v => node.advancePeriods = v);
                AddIntField("advanceDays", node.advanceDays, v => node.advanceDays = v);
            }

            if (type == StoryNodeType.End)
                AddField("endTitle", node.endTitle, v => node.endTitle = v);

            DrawChoices(node);
            DrawDeleteButton(node);
        }

        void DrawChoices(StoryNodeData node)
        {
            node.choices ??= new List<StoryChoiceData>();
            m_scroll.Add(new Label("— 选项 —") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 12 } });

            for (var i = 0; i < node.choices.Count; i++)
            {
                var choice = node.choices[i];
                var box = new VisualElement
                {
                    style =
                    {
                        borderTopWidth = 1, borderBottomWidth = 1,
                        borderLeftWidth = 1, borderRightWidth = 1,
                        borderTopColor = new Color(0.3f, 0.3f, 0.3f),
                        borderBottomColor = new Color(0.3f, 0.3f, 0.3f),
                        borderLeftColor = new Color(0.3f, 0.3f, 0.3f),
                        borderRightColor = new Color(0.3f, 0.3f, 0.3f),
                        paddingTop = 4, paddingBottom = 4, paddingLeft = 4, paddingRight = 4,
                        marginBottom = 6
                    }
                };
                var idx = i;
                box.Add(MakeField("text", choice.text, v => choice.text = v));
                box.Add(MakeField("nextNodeId", choice.nextNodeId, v => choice.nextNodeId = v));
                box.Add(MakeField("requiredFlag", choice.requiredFlag, v => choice.requiredFlag = v));
                box.Add(MakeField("blockedByFlag", choice.blockedByFlag, v => choice.blockedByFlag = v));
                box.Add(MakeField("requiredItemId", choice.requiredItemId, v => choice.requiredItemId = v));
                box.Add(MakeIntField("requiredDay", choice.requiredDay, v => choice.requiredDay = v));
                box.Add(MakeField("requiredPeriod", choice.requiredPeriod, v => choice.requiredPeriod = v));
                box.Add(MakeIntField("requiredMinDay", choice.requiredMinDay, v => choice.requiredMinDay = v));
                box.Add(MakeIntField("requiredMaxDay", choice.requiredMaxDay, v => choice.requiredMaxDay = v));
                box.Add(MakeField("requiredRelationship", choice.requiredRelationship, v => choice.requiredRelationship = v));
                box.Add(MakeField("未解锁原因", choice.unavailableReason, v => choice.unavailableReason = v));

                var removeBtn = new Button(() => { node.choices.RemoveAt(idx); Bind(node); }) { text = "删除选项" };
                box.Add(removeBtn);
                m_scroll.Add(box);
            }

            var addBtn = new Button(() =>
            {
                node.choices.Add(new StoryChoiceData { text = "新选项" });
                Bind(node);
            }) { text = "+ 添加选项" };
            m_scroll.Add(addBtn);
        }

        void DrawDeleteButton(StoryNodeData node)
        {
            var btn = new Button(() =>
            {
                if (!EditorUtility.DisplayDialog("删除节点", $"确定删除 {node.id}？", "删除", "取消")) return;
                m_window.GraphView.DeleteNode(node);
                Bind(null);
            })
            { text = "删除节点", style = { marginTop = 16, color = new Color(1f, 0.4f, 0.4f) } };
            m_scroll.Add(btn);
        }

        void AddField(string label, string value, Action<string> setter)
        {
            m_scroll.Add(MakeField(label, value, setter));
        }

        void AddIntField(string label, int value, Action<int> setter)
        {
            m_scroll.Add(MakeIntField(label, value, setter));
        }

        void AddToggle(string label, bool value, Action<bool> setter)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(new Label(label) { style = { width = 120 } });
            var toggle = new Toggle { value = value };
            toggle.RegisterValueChangedCallback(evt => setter(evt.newValue));
            row.Add(toggle);
            m_scroll.Add(row);
        }

        void AddTextArea(string label, string value, Action<string> setter)
        {
            m_scroll.Add(new Label(label));
            var field = new TextField { multiline = true, value = value ?? "" };
            field.style.minHeight = 80;
            field.RegisterValueChangedCallback(evt => setter(evt.newValue));
            m_scroll.Add(field);
        }

        void AddEnumField(string label, string value, Action<string> setter, params string[] options)
        {
            var choices = options.ToList();
            var idx = choices.IndexOf(value);
            if (idx < 0) idx = 0;

            var popup = new PopupField<string>(choices, idx);
            popup.label = label;
            popup.style.marginBottom = 4;
            popup.RegisterValueChangedCallback(evt => setter(evt.newValue));
            m_scroll.Add(popup);
        }

        VisualElement MakeField(string label, string value, Action<string> setter)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
            row.Add(new Label(label) { style = { width = 120, fontSize = 11 } });
            var field = new TextField { value = value ?? "" };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => setter(evt.newValue));
            row.Add(field);
            return row;
        }

        VisualElement MakeIntField(string label, int value, Action<int> setter)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
            row.Add(new Label(label) { style = { width = 120, fontSize = 11 } });
            var field = new IntegerField { value = value };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => setter(evt.newValue));
            row.Add(field);
            return row;
        }

        void RefreshGraphNode()
        {
            if (m_node != null)
                m_window.GraphView.RefreshNode(m_node);
        }
    }
}
