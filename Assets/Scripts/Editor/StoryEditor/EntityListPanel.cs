using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Editor
{
    public class EntityListPanel : VisualElement
    {
        readonly StoryEditorWindow m_window;
        VisualElement m_listContainer;
        string m_activeTab = "npc";

        public EntityListPanel(StoryEditorWindow window)
        {
            m_window = window;
            style.minWidth = 220;

            var tabs = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            AddTabButton(tabs, "NPC", "npc");
            AddTabButton(tabs, "地点", "loc");
            AddTabButton(tabs, "物品", "item");
            AddTabButton(tabs, "关系", "rel");
            AddTabButton(tabs, "日程", "sched");
            Add(tabs);

            m_listContainer = new ScrollView { style = { flexGrow = 1 } };
            Add(m_listContainer);

            Refresh();
        }

        void AddTabButton(VisualElement parent, string label, string tabId)
        {
            var btn = new Button(() => { m_activeTab = tabId; Refresh(); }) { text = label };
            btn.style.flexGrow = 1;
            parent.Add(btn);
        }

        public void Refresh()
        {
            m_listContainer.Clear();
            switch (m_activeTab)
            {
                case "npc": DrawNpcs(); break;
                case "loc": DrawLocations(); break;
                case "item": DrawItems(); break;
                case "rel": DrawRelationships(); break;
                case "sched": DrawSchedules(); break;
            }
        }

        void DrawNpcs()
        {
            m_listContainer.Add(MakeHeader("NPC 列表"));
            foreach (var npc in m_window.Project.npcs.ToList())
                m_listContainer.Add(DrawNpcEditor(npc));
            m_listContainer.Add(MakeAddButton("添加 NPC", () =>
            {
                var id = "npc_" + (m_window.Project.npcs.Count + 1);
                m_window.Project.npcs.Add(new NPCDefinition
                {
                    id = id,
                    displayName = "新 NPC",
                    defaultNodeId = "",
                    locationIds = new List<string>(),
                    schedules = new List<NpcScheduleEntry>()
                });
                Refresh();
            }));
        }

        VisualElement DrawNpcEditor(NPCDefinition npc)
        {
            var box = MakeBox();
            box.Add(MakeField("id", npc.id, v => npc.id = v));
            box.Add(MakeField("displayName", npc.displayName, v => npc.displayName = v));
            box.Add(MakeTextArea("description", npc.description, v => npc.description = v));
            box.Add(MakeField("portraitId", npc.portraitId, v => npc.portraitId = v));
            box.Add(MakeField("defaultNodeId", npc.defaultNodeId, v => npc.defaultNodeId = v));
            box.Add(MakeField("locationIds (逗号分隔)", string.Join(",", npc.locationIds ?? new List<string>()),
                v => npc.locationIds = SplitList(v)));
            box.Add(MakeRemoveButton(() => { m_window.Project.npcs.Remove(npc); Refresh(); }));
            return box;
        }

        void DrawLocations()
        {
            m_listContainer.Add(MakeHeader("地点列表"));
            foreach (var loc in m_window.Project.locations.ToList())
                m_listContainer.Add(DrawLocationEditor(loc));
            m_listContainer.Add(MakeAddButton("添加地点", () =>
            {
                m_window.Project.locations.Add(new LocationDefinition
                {
                    id = "loc_" + (m_window.Project.locations.Count + 1),
                    displayName = "新地点",
                    npcIds = new List<string>()
                });
                Refresh();
            }));
        }

        VisualElement DrawLocationEditor(LocationDefinition loc)
        {
            var box = MakeBox();
            box.Add(MakeField("id", loc.id, v => loc.id = v));
            box.Add(MakeField("displayName", loc.displayName, v => loc.displayName = v));
            box.Add(MakeTextArea("description", loc.description, v => loc.description = v));
            box.Add(MakeField("npcIds (逗号分隔)", string.Join(",", loc.npcIds ?? new List<string>()),
                v => loc.npcIds = SplitList(v)));
            box.Add(MakeField("backgroundId", loc.backgroundId, v => loc.backgroundId = v));
            box.Add(MakeField("requiredItemId", loc.requiredItemId, v => loc.requiredItemId = v));
            box.Add(MakeField("requiredFlag", loc.requiredFlag, v => loc.requiredFlag = v));
            box.Add(MakeField("requiredPeriod", loc.requiredPeriod, v => loc.requiredPeriod = v));
            box.Add(MakeRemoveButton(() => { m_window.Project.locations.Remove(loc); Refresh(); }));
            return box;
        }

        void DrawItems()
        {
            m_listContainer.Add(MakeHeader("物品列表"));
            foreach (var item in m_window.Project.items.ToList())
                m_listContainer.Add(DrawItemEditor(item));
            m_listContainer.Add(MakeAddButton("添加物品", () =>
            {
                m_window.Project.items.Add(new ItemDefinition
                {
                    id = "item_" + (m_window.Project.items.Count + 1),
                    displayName = "新物品"
                });
                Refresh();
            }));
        }

        VisualElement DrawItemEditor(ItemDefinition item)
        {
            var box = MakeBox();
            box.Add(MakeField("id", item.id, v => item.id = v));
            box.Add(MakeField("displayName", item.displayName, v => item.displayName = v));
            box.Add(MakeTextArea("description", item.description, v => item.description = v));
            box.Add(MakeToggle("consumable", item.consumable, v => item.consumable = v));
            box.Add(MakeIntField("healHp", item.healHp, v => item.healHp = v));
            box.Add(MakeIntField("healSan", item.healSan, v => item.healSan = v));
            box.Add(MakeField("useNodeId", item.useNodeId, v => item.useNodeId = v));
            box.Add(MakeRemoveButton(() => { m_window.Project.items.Remove(item); Refresh(); }));
            return box;
        }

        void DrawRelationships()
        {
            m_listContainer.Add(MakeHeader("NPC 关系"));
            foreach (var rel in m_window.Project.relationships.ToList())
                m_listContainer.Add(DrawRelationshipEditor(rel));
            m_listContainer.Add(MakeAddButton("添加关系", () =>
            {
                m_window.Project.relationships.Add(new NpcRelationship
                {
                    id = "rel_" + (m_window.Project.relationships.Count + 1),
                    type = "knows",
                    label = "相识"
                });
                Refresh();
            }));
        }

        VisualElement DrawRelationshipEditor(NpcRelationship rel)
        {
            var box = MakeBox();
            box.Add(MakeField("id", rel.id, v => rel.id = v));
            box.Add(MakeField("fromNpcId", rel.fromNpcId, v => rel.fromNpcId = v));
            box.Add(MakeField("toNpcId", rel.toNpcId, v => rel.toNpcId = v));
            box.Add(MakeField("type", rel.type, v => rel.type = v));
            box.Add(MakeField("label", rel.label, v => rel.label = v));
            box.Add(MakeTextArea("description", rel.description, v => rel.description = v));
            box.Add(MakeField("unlockFlag", rel.unlockFlag, v =>
            {
                rel.unlockFlag = v;
                if (string.IsNullOrEmpty(v) && !string.IsNullOrEmpty(rel.id))
                    rel.unlockFlag = "rel_" + rel.id;
            }));
            if (string.IsNullOrEmpty(rel.unlockFlag) && !string.IsNullOrEmpty(rel.id))
                rel.unlockFlag = "rel_" + rel.id;
            box.Add(new Label($"建议旗标: rel_{rel.id}") { style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f) } });
            box.Add(MakeRemoveButton(() => { m_window.Project.relationships.Remove(rel); Refresh(); }));
            return box;
        }

        void DrawSchedules()
        {
            m_listContainer.Add(MakeHeader("NPC 日程"));
            m_listContainer.Add(new Label("day=0 表示每天；period 可为 morning/afternoon/evening/night/any")
                { style = { fontSize = 10, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });

            foreach (var npc in m_window.Project.npcs)
            {
                npc.schedules ??= new List<NpcScheduleEntry>();
                var npcBox = MakeBox();
                npcBox.Add(MakeHeader(npc.displayName + " (" + npc.id + ")"));

                for (var i = 0; i < npc.schedules.Count; i++)
                {
                    var entry = npc.schedules[i];
                    var idx = i;
                    var row = MakeBox();
                    row.Add(MakeIntField("day", entry.day, v => entry.day = v));
                    row.Add(MakeField("period", entry.period, v => entry.period = v));
                    row.Add(MakeField("locationId", entry.locationId, v => entry.locationId = v));
                    row.Add(MakeRemoveButton(() => { npc.schedules.RemoveAt(idx); Refresh(); }));
                    npcBox.Add(row);
                }

                npcBox.Add(MakeAddButton("添加日程", () =>
                {
                    npc.schedules.Add(new NpcScheduleEntry { day = 1, period = "morning", locationId = "" });
                    Refresh();
                }));
                m_listContainer.Add(npcBox);
            }
        }

        static List<string> SplitList(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new List<string>();
            return value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        static Label MakeHeader(string text) =>
            new Label(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6, marginBottom = 4 } };

        static VisualElement MakeBox() => new VisualElement
        {
            style =
            {
                borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                borderTopColor = new Color(0.25f, 0.25f, 0.28f),
                borderBottomColor = new Color(0.25f, 0.25f, 0.28f),
                borderLeftColor = new Color(0.25f, 0.25f, 0.28f),
                borderRightColor = new Color(0.25f, 0.25f, 0.28f),
                paddingTop = 6, paddingBottom = 6, paddingLeft = 6, paddingRight = 6,
                marginBottom = 8
            }
        };

        static Button MakeAddButton(string text, Action onClick) =>
            new Button(onClick) { text = "+ " + text, style = { marginBottom = 8 } };

        static Button MakeRemoveButton(Action onClick) =>
            new Button(onClick) { text = "删除", style = { color = new Color(1f, 0.45f, 0.45f) } };

        static VisualElement MakeField(string label, string value, Action<string> setter)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
            row.Add(new Label(label) { style = { width = 100, fontSize = 10 } });
            var field = new TextField { value = value ?? "" };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => setter(evt.newValue));
            row.Add(field);
            return row;
        }

        static VisualElement MakeIntField(string label, int value, Action<int> setter)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
            row.Add(new Label(label) { style = { width = 100, fontSize = 10 } });
            var field = new IntegerField { value = value };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => setter(evt.newValue));
            row.Add(field);
            return row;
        }

        static VisualElement MakeTextArea(string label, string value, Action<string> setter)
        {
            var col = new VisualElement { style = { marginBottom = 4 } };
            col.Add(new Label(label) { style = { fontSize = 10 } });
            var field = new TextField { multiline = true, value = value ?? "" };
            field.style.minHeight = 40;
            field.RegisterValueChangedCallback(evt => setter(evt.newValue));
            col.Add(field);
            return col;
        }

        static VisualElement MakeToggle(string label, bool value, Action<bool> setter)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(new Label(label) { style = { width = 100, fontSize = 10 } });
            var toggle = new Toggle { value = value };
            toggle.RegisterValueChangedCallback(evt => setter(evt.newValue));
            row.Add(toggle);
            return row;
        }
    }
}
