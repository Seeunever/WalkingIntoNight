using System.Collections.Generic;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Dice;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.NPC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WalkingIntoNight.TRPG.UI
{
    public static class GameplayUI
    {
        static ScenarioRunner s_runner;
        static TMP_Text s_dialogueText;
        static TMP_Text s_speakerText;
        static TMP_Text s_logText;
        static RectTransform s_choiceArea;
        static GameObject s_overlayPanel;
        static readonly List<string> s_logLines = new List<string>();

        public static void Build()
        {
            UIRoot.EnsureCanvas();
            NPCDatabase.EnsureLoaded();
            ItemDatabase.EnsureLoaded();

            var root = UIBuilder.Panel("GameplayRoot", UIRoot.Layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.05f, 0.05f, 0.08f, 1f));

            UIBuilder.CreateText(root, "Header", "WalkingIntoNight · ????", 26,
                TextAlignmentOptions.Top).rectTransform.anchorMax = new Vector2(1, 0.96f);

            var logPanel = UIBuilder.Panel("Log", root, new Vector2(0.02f, 0.58f), new Vector2(0.35f, 0.94f),
                Vector2.zero, Vector2.zero);
            s_logText = UIBuilder.CreateText(logPanel, "LogText", "", 18);

            var dialoguePanel = UIBuilder.Panel("Dialogue", root, new Vector2(0.37f, 0.35f), new Vector2(0.98f, 0.94f),
                Vector2.zero, Vector2.zero);
            s_speakerText = UIBuilder.CreateText(dialoguePanel, "Speaker", "", 28, TextAlignmentOptions.TopLeft);
            s_speakerText.rectTransform.anchorMax = new Vector2(1, 0.88f);
            s_dialogueText = UIBuilder.CreateText(dialoguePanel, "Body", "", 24);
            s_dialogueText.rectTransform.anchorMin = new Vector2(0, 0.25f);

            s_choiceArea = UIBuilder.VerticalLayout(dialoguePanel, "Choices");
            s_choiceArea.anchorMin = new Vector2(0.05f, 0.02f);
            s_choiceArea.anchorMax = new Vector2(0.95f, 0.24f);

            var menuBar = UIBuilder.VerticalLayout(root, "Menu");
            menuBar.anchorMin = new Vector2(0.02f, 0.02f);
            menuBar.anchorMax = new Vector2(0.35f, 0.55f);
            UIBuilder.CreateButton(menuBar, "???", ShowCharacterSheet);
            UIBuilder.CreateButton(menuBar, "??", ShowInventory);
            UIBuilder.CreateButton(menuBar, "?? / NPC", ShowLocationExplorer);
            UIBuilder.CreateButton(menuBar, "?? (??1)", SaveGame);
            UIBuilder.CreateButton(menuBar, "???", SceneLoader.LoadMainMenu);

            s_runner = new ScenarioRunner();
            s_runner.OnLog += AppendLog;
            s_runner.OnNodePresented += PresentNode;
            s_runner.OnChoicesPresented += PresentChoices;
            s_runner.OnCheckResolved += result => AppendLog(result.Summary);
            s_runner.OnScenarioEnded += OnEnded;
            s_runner.OnRequestCombatUI += ShowCombat;
            s_runner.OnRequestLocationUI += ShowLocationExplorer;
            s_runner.OnRequestNpcUI += ShowLocationExplorer;

            var gs = GameStateManager.Instance;
            s_runner.LoadScenario(gs.CurrentScenarioId);

            if (!string.IsNullOrEmpty(gs.CurrentNodeId))
                s_runner.StartFrom(gs.CurrentNodeId);
            else
                s_runner.StartFrom(null);
        }

        static void PresentNode(StoryNodeData node)
        {
            ClearChoices();
            s_speakerText.text = string.IsNullOrEmpty(node.speaker) ? "" : node.speaker;
            s_dialogueText.text = node.text ?? "";

            var type = StoryNodeTypeParser.Parse(node.type);
            if (type == StoryNodeType.End)
            {
                UIBuilder.CreateButton(s_choiceArea, "?????", SceneLoader.LoadMainMenu);
                return;
            }

            if ((node.choices == null || node.choices.Count == 0) && !string.IsNullOrEmpty(node.nextNodeId))
                UIBuilder.CreateButton(s_choiceArea, "??", () => s_runner.AdvanceTo(node.nextNodeId));
        }

        static void PresentChoices(List<StoryChoiceData> choices)
        {
            ClearChoices();
            if (choices == null) return;

            foreach (var choice in choices)
            {
                var c = choice;
                UIBuilder.CreateButton(s_choiceArea, c.text, () => s_runner.SelectChoice(c));
            }
        }

        static void ClearChoices()
        {
            if (s_choiceArea == null) return;
            for (var i = s_choiceArea.childCount - 1; i >= 0; i--)
                Object.Destroy(s_choiceArea.GetChild(i).gameObject);
        }

        static void AppendLog(string msg)
        {
            s_logLines.Add(msg);
            if (s_logLines.Count > 30) s_logLines.RemoveAt(0);
            s_logText.text = string.Join("\n", s_logLines);
        }

        static void OnEnded()
        {
            AppendLog("?? ???? ??");
        }

        static void ShowCharacterSheet()
        {
            CloseOverlay();
            var inv = GameStateManager.Instance.Investigator;
            if (inv == null) return;

            s_overlayPanel = CreateOverlay("???");
            UIBuilder.CreateText(s_overlayPanel.transform, "Sheet",
                $"{inv.Name}\nHP {inv.HP}/{inv.MaxHP}  SAN {inv.SAN}/{inv.MaxSAN}  MP {inv.MP}/{inv.MaxMP}\n\n" +
                $"STR {inv.STR} CON {inv.CON} POW {inv.POW} DEX {inv.DEX}\n" +
                $"APP {inv.APP} INT {inv.INT} EDU {inv.EDU} SIZ {inv.SIZ}\n\n" +
                $"?? {inv.GetSkill("spot_hidden")} ?? {inv.GetSkill("listen")} ??? {inv.GetSkill("library_use")}\n" +
                $"??? {inv.GetSkill("psychology")} ?? {inv.GetSkill("persuade")} ?? {inv.GetSkill("fight")} ?? {inv.GetSkill("firearms")}",
                24);
            AddCloseButton(s_overlayPanel);
        }

        static void ShowInventory()
        {
            CloseOverlay();
            s_overlayPanel = CreateOverlay("??");
            var layout = UIBuilder.VerticalLayout(s_overlayPanel.transform, "Items");

            foreach (var itemId in GameStateManager.Instance.Inventory.Items)
            {
                var def = ItemDatabase.Get(itemId);
                var label = def != null ? def.displayName : itemId;
                var desc = def?.description ?? "";
                UIBuilder.CreateButton(layout, $"{label} ? {desc}", () =>
                {
                    if (def != null && def.consumable)
                    {
                        UseItem(def);
                        ShowInventory();
                    }
                });
            }

            AddCloseButton(s_overlayPanel);
        }

        static void UseItem(ItemDefinition def)
        {
            var inv = GameStateManager.Instance.Investigator;
            if (inv == null) return;

            if (def.healHp > 0)
                inv.HP = Mathf.Min(inv.MaxHP, inv.HP + def.healHp);
            if (def.healSan > 0)
                inv.SAN = Mathf.Min(inv.MaxSAN, inv.SAN + def.healSan);

            GameStateManager.Instance.Inventory.RemoveItem(def.id);
            AppendLog($"??? {def.displayName}");
        }

        static void ShowLocationExplorer()
        {
            CloseOverlay();
            s_overlayPanel = CreateOverlay("??? NPC");
            var layout = UIBuilder.VerticalLayout(s_overlayPanel.transform, "Loc");

            var gs = GameStateManager.Instance;
            var current = NPCDatabase.GetLocation(gs.CurrentLocationId);

            UIBuilder.CreateText(layout, "Current", $"???{current?.displayName ?? gs.CurrentLocationId}\n{current?.description}", 22);

            foreach (var loc in NPCDatabase.AllLocations)
            {
                var locId = loc.id;
                UIBuilder.CreateButton(layout, $"?? {loc.displayName}", () => s_runner.TravelToLocation(locId));
            }

            if (current?.npcIds != null)
            {
                foreach (var npcId in current.npcIds)
                {
                    var npc = NPCDatabase.GetNpc(npcId);
                    if (npc == null) continue;
                    var id = npcId;
                    UIBuilder.CreateButton(layout, $"???{npc.displayName}", () =>
                    {
                        CloseOverlay();
                        s_runner.TalkToNpc(id);
                    });
                }
            }

            UIBuilder.CreateButton(layout, "????", () =>
            {
                CloseOverlay();
                s_runner.AdvanceTo(gs.CurrentNodeId);
            });

            AddCloseButton(s_overlayPanel);
        }

        static void ShowCombat()
        {
            CloseOverlay();
            var combat = s_runner.Combat;
            if (!combat.IsActive) return;

            s_overlayPanel = CreateOverlay("??");
            var layout = UIBuilder.VerticalLayout(s_overlayPanel.transform, "CombatUI");

            var player = combat.GetPlayer();
            UIBuilder.CreateText(layout, "Player", $"{player.displayName} HP {player.HP}/{player.MaxHP}", 22);

            var enemies = combat.GetEnemies();
            for (var i = 0; i < enemies.Count; i++)
            {
                var idx = i;
                var e = enemies[i];
                UIBuilder.CreateButton(layout, $"?? {e.displayName} (HP {e.HP})", () => combat.PlayerAttack(idx));
            }

            UIBuilder.CreateButton(layout, "??", combat.PlayerDodge);
            UIBuilder.CreateButton(layout, "??", combat.PlayerFlee);
        }

        static GameObject CreateOverlay(string title)
        {
            var panel = UIBuilder.Panel("Overlay", UIRoot.Layer, new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f),
                Vector2.zero, Vector2.zero, new Color(0.1f, 0.12f, 0.18f, 0.98f));
            UIBuilder.CreateText(panel, "Title", title, 32, TextAlignmentOptions.Top);
            return panel.gameObject;
        }

        static void AddCloseButton(GameObject panel)
        {
            var btn = UIBuilder.CreateButton(panel.transform, "??", CloseOverlay);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.3f, 0.02f);
            rt.anchorMax = new Vector2(0.7f, 0.08f);
            rt.anchoredPosition = Vector2.zero;
        }

        static void CloseOverlay()
        {
            if (s_overlayPanel != null)
                Object.Destroy(s_overlayPanel);
            s_overlayPanel = null;
        }

        static void SaveGame()
        {
            var data = GameStateManager.Instance.ToSaveData();
            SaveSystem.Save(0, data);
            AppendLog("?????? 1");
        }
    }
}
