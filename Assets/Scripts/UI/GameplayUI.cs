using System.Collections.Generic;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Core;
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
        static Image s_backgroundImage;
        static TMP_Text s_narrativeText;
        static DialogueTextPresenter s_dialoguePresenter;
        static TMP_Text s_timeText;
        static TMP_Text s_logText;
        static ScrollRect s_narrativeScroll;
        static ScrollRect s_choiceScroll;
        static ScrollRect s_sidebarScroll;
        static ScrollRect s_logScroll;
        static RectTransform s_choiceArea;
        static RectTransform s_sidebarArea;
        static RectTransform s_sidebarContent;
        static RectTransform s_portraitFrame;
        static PortraitPresenter s_portraitPresenter;
        static Button s_waitNextPeriodButton;
        static Button s_waitNextDayButton;
        static Button s_saveButton;
        static bool s_waitingContinue;
        static bool s_inCombat;
        static StoryNodeData s_presentedNode;
        static List<StoryChoiceData> s_pendingChoices;
        static StoryNodeData s_noChoicesAvailableNode;

        public static void Build()
        {
            UIRoot.EnsureCanvas();
            s_waitingContinue = false;
            s_inCombat = false;
            s_presentedNode = null;
            s_pendingChoices = null;
            s_noChoicesAvailableNode = null;
            s_runner = new ScenarioRunner();
            WireEvents();

            var root = UIBuilder.Panel("Gameplay", UIRoot.Layer,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.05f, 0.05f, 0.08f, 1f));

            var backgroundGo = new GameObject("LocationBackground");
            backgroundGo.transform.SetParent(root, false);
            var backgroundRect = backgroundGo.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            s_backgroundImage = backgroundGo.AddComponent<Image>();
            s_backgroundImage.preserveAspect = false;
            s_backgroundImage.raycastTarget = false;

            UIBuilder.Panel("BackgroundShade", root,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.01f, 0.02f, 0.04f, 0.22f));

            var header = UIBuilder.Panel("Header", root,
                new Vector2(0f, 0.92f), Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.06f, 0.08f, 0.11f, 0.86f));
            s_timeText = UIBuilder.CreateText(header, "Time", "", 20, TextAlignmentOptions.MidlineLeft);
            s_timeText.rectTransform.offsetMin = new Vector2(16, 0);
            s_timeText.rectTransform.offsetMax = new Vector2(-200, 0);

            var waitLayout = new GameObject("WaitButtons").AddComponent<RectTransform>();
            waitLayout.SetParent(header, false);
            waitLayout.anchorMin = new Vector2(1f, 0f);
            waitLayout.anchorMax = new Vector2(1f, 1f);
            waitLayout.pivot = new Vector2(1f, 0.5f);
            waitLayout.sizeDelta = new Vector2(380, 0);
            waitLayout.anchoredPosition = new Vector2(-8, 0);
            var hlg = waitLayout.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;

            s_waitNextPeriodButton = CreateSmallButton(waitLayout, "\u4e0b\u4e00\u65f6\u6bb5", () => s_runner?.WaitNextPeriod());
            s_waitNextDayButton = CreateSmallButton(waitLayout, "\u4e0b\u4e00\u5929", () => s_runner?.WaitNextDay());
            s_saveButton = CreateSmallButton(waitLayout, "\u5b58\u6863", SaveGame);

            s_sidebarArea = UIBuilder.Panel("Sidebar", root,
                new Vector2(0f, 0.25f), new Vector2(0.28f, 0.92f), Vector2.zero, Vector2.zero,
                new Color(0.04f, 0.06f, 0.09f, 0.82f));
            var sidebarView = UIBuilder.ScrollableVerticalLayout(s_sidebarArea, "SidebarScroll", 6f);
            s_sidebarScroll = sidebarView.ScrollRect;
            s_sidebarContent = sidebarView.Content;
            s_sidebarContent.gameObject.name = "SidebarLayout";

            var main = UIBuilder.Panel("Main", root,
                new Vector2(0.28f, 0f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero,
                new Color(0.03f, 0.04f, 0.06f, 0.64f));

            var narrativeView = UIBuilder.ScrollableVerticalLayout(main, "NarrativeScroll", 0f, 4);
            narrativeView.Root.anchorMin = new Vector2(0.02f, 0.35f);
            narrativeView.Root.anchorMax = new Vector2(0.65f, 0.98f);
            narrativeView.Root.offsetMin = Vector2.zero;
            narrativeView.Root.offsetMax = Vector2.zero;
            s_narrativeScroll = narrativeView.ScrollRect;
            s_narrativeText = UIBuilder.CreateText(
                narrativeView.Content,
                "Narrative",
                "",
                24,
                TextAlignmentOptions.TopLeft);
            s_narrativeText.rectTransform.anchorMin = new Vector2(0f, 1f);
            s_narrativeText.rectTransform.anchorMax = new Vector2(1f, 1f);
            s_narrativeText.rectTransform.pivot = new Vector2(0.5f, 1f);
            s_narrativeText.rectTransform.sizeDelta = new Vector2(0f, 120f);
            var narrativeFitter = s_narrativeText.gameObject.AddComponent<ContentSizeFitter>();
            narrativeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            narrativeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            s_dialoguePresenter = s_narrativeText.gameObject.AddComponent<DialogueTextPresenter>();
            s_dialoguePresenter.Initialize(s_narrativeText);

            s_portraitFrame = UIBuilder.Panel("PortraitFrame", main,
                new Vector2(0.67f, 0.35f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero,
                new Color(0.02f, 0.03f, 0.04f, 0.35f));
            s_portraitFrame.gameObject.AddComponent<RectMask2D>();
            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(s_portraitFrame, false);
            var portraitRect = portraitGo.AddComponent<RectTransform>();
            portraitRect.anchorMin = Vector2.zero;
            portraitRect.anchorMax = Vector2.one;
            portraitRect.offsetMin = new Vector2(12, 12);
            portraitRect.offsetMax = new Vector2(-12, -12);
            var portraitImage = portraitGo.AddComponent<Image>();
            s_portraitPresenter = s_portraitFrame.gameObject.AddComponent<PortraitPresenter>();
            s_portraitPresenter.Initialize(portraitImage, portraitRect);

            var choiceView = UIBuilder.ScrollableVerticalLayout(main, "ChoiceScroll", 8f);
            choiceView.Root.anchorMin = new Vector2(0.05f, 0.02f);
            choiceView.Root.anchorMax = new Vector2(0.95f, 0.32f);
            choiceView.Root.offsetMin = Vector2.zero;
            choiceView.Root.offsetMax = Vector2.zero;
            s_choiceScroll = choiceView.ScrollRect;
            s_choiceArea = choiceView.Content;
            s_choiceArea.gameObject.name = "Choices";

            var logPanel = UIBuilder.Panel("Log", root,
                new Vector2(0f, 0f), new Vector2(0.28f, 0.25f), Vector2.zero, Vector2.zero,
                new Color(0.03f, 0.04f, 0.06f, 0.8f));
            var logView = UIBuilder.ScrollableVerticalLayout(logPanel, "LogScroll", 0f, 4);
            s_logScroll = logView.ScrollRect;
            s_logText = UIBuilder.CreateText(
                logView.Content,
                "LogText",
                "",
                14,
                TextAlignmentOptions.TopLeft);
            s_logText.rectTransform.anchorMin = new Vector2(0f, 1f);
            s_logText.rectTransform.anchorMax = new Vector2(1f, 1f);
            s_logText.rectTransform.pivot = new Vector2(0.5f, 1f);
            s_logText.rectTransform.sizeDelta = new Vector2(0f, 80f);
            var logFitter = s_logText.gameObject.AddComponent<ContentSizeFitter>();
            logFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            logFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateSidebarLabel(s_sidebarContent, "LocTitle", "\u8c03\u67e5\u9762\u677f", 20, 32);

            var gs = GameStateManager.Instance;
            s_runner.LoadScenario(gs.CurrentScenarioId);

            if (gs.SaveSlot == 1 && !string.IsNullOrEmpty(gs.CurrentNodeId))
                s_runner.ResumeFromSave(gs.CurrentNodeId);
            else
                s_runner.StartFrom(null);

            RefreshTime();
            RefreshBackground();
            RefreshInteractionControls();
        }

        static void WireEvents()
        {
            s_runner.OnNodePresented = node =>
            {
                if (s_inCombat) return;
                PresentNarrativeNode(node);
            };

            s_runner.OnChoicesPresented = choices =>
            {
                if (s_inCombat) return;
                s_pendingChoices = choices != null
                    ? new List<StoryChoiceData>(choices)
                    : new List<StoryChoiceData>();
                s_noChoicesAvailableNode = null;
                if (s_dialoguePresenter == null || !s_dialoguePresenter.IsRevealing)
                    RefreshNarrativeActions();
            };

            s_runner.OnNoChoicesAvailable = node =>
            {
                if (s_inCombat) return;
                s_pendingChoices = null;
                s_noChoicesAvailableNode = node;
                if (s_dialoguePresenter == null || !s_dialoguePresenter.IsRevealing)
                    RefreshNarrativeActions();
            };

            s_runner.OnLog = AppendLog;

            s_runner.OnScenarioEnded = () =>
            {
                s_portraitPresenter?.Hide(false);
                if (s_dialoguePresenter == null || !s_dialoguePresenter.IsRevealing)
                    RefreshNarrativeActions();
            };

            s_runner.OnRequestLocationUI = () =>
            {
                RefreshTime();
                RefreshBackground();
                RefreshSidebar();
            };
            s_runner.OnRequestNpcUI = RefreshSidebar;
            s_runner.OnTimeChanged = () =>
            {
                RefreshTime();
                RefreshSidebar();
            };
            s_runner.OnInventoryChanged = RefreshSidebar;
            s_runner.OnInteractionModeChanged = mode =>
            {
                s_inCombat = mode == ScenarioInteractionMode.Combat;
                RefreshInteractionControls();
            };

            s_runner.OnRequestCombatUI = ShowCombatUI;
            s_runner.Combat.OnCombatUpdated += RefreshCombatUI;
        }

        static void PresentNarrativeNode(StoryNodeData node)
        {
            s_presentedNode = node;
            s_pendingChoices = null;
            s_noChoicesAvailableNode = null;
            s_waitingContinue = false;
            ClearChoices();

            var type = StoryNodeTypeParser.Parse(node?.type);
            if (type == StoryNodeType.End)
                s_portraitPresenter?.Hide(false);
            else
                ShowPortrait(node);

            RefreshBackground();
            RefreshSidebar();

            var speaker = string.IsNullOrEmpty(node?.speaker) ? "" : $"\u3010{node.speaker}\u3011\n";
            var text = speaker + (node?.text ?? "");
            var presentationVersion = s_runner.PresentationVersion;
            if (s_dialoguePresenter != null)
            {
                s_dialoguePresenter.Present(text, () =>
                {
                    if (s_runner == null ||
                        s_runner.PresentationVersion != presentationVersion ||
                        s_presentedNode != node)
                        return;
                    RefreshNarrativeActions();
                });
            }
            else if (s_narrativeText != null)
            {
                s_narrativeText.text = text;
            }
            ScrollToTop(s_narrativeScroll);

            RefreshNarrativeActions();
        }

        static void RefreshNarrativeActions()
        {
            if (s_inCombat || s_presentedNode == null || s_choiceArea == null) return;
            ClearChoices();

            var node = s_presentedNode;
            var presentationVersion = s_runner.PresentationVersion;
            if (s_dialoguePresenter != null && s_dialoguePresenter.IsRevealing)
            {
                s_waitingContinue = true;
                UIBuilder.CreateButton(s_choiceArea, "\u7ee7\u7eed", () =>
                {
                    if (s_runner.PresentationVersion != presentationVersion ||
                        s_presentedNode != node)
                        return;
                    s_dialoguePresenter.RevealImmediately();
                });
                return;
            }

            var type = StoryNodeTypeParser.Parse(node.type);
            if (type == StoryNodeType.End)
            {
                s_waitingContinue = false;
                UIBuilder.CreateButton(s_choiceArea, "\u8fd4\u56de\u4e3b\u83dc\u5355", SceneLoader.LoadMainMenu);
                return;
            }

            if (s_pendingChoices != null && s_pendingChoices.Count > 0)
            {
                s_waitingContinue = false;
                foreach (var choice in s_pendingChoices)
                {
                    var capturedChoice = choice;
                    var availability = ConditionEvaluator.EvaluateChoice(capturedChoice);
                    var label = availability.IsAvailable
                        ? capturedChoice.text
                        : $"{capturedChoice.text}\n<color=#C9A7A7>未解锁：{availability.Reason}</color>";
                    var button = UIBuilder.CreateButton(s_choiceArea, label,
                        () => s_runner.TrySelectChoice(capturedChoice, presentationVersion));
                    button.interactable = availability.IsAvailable;
                }
                return;
            }

            if (node.choices != null && node.choices.Count > 0 &&
                s_noChoicesAvailableNode == null)
            {
                // Runner will immediately publish either available choices or the no-choice fallback.
                return;
            }

            if (s_noChoicesAvailableNode != null)
            {
                CreateNoChoicesFallback(node, presentationVersion);
                return;
            }

            s_waitingContinue = true;
            UIBuilder.CreateButton(s_choiceArea, "\u7ee7\u7eed", () =>
            {
                if (s_runner.PresentationVersion != presentationVersion ||
                    s_presentedNode != node || !s_waitingContinue)
                    return;
                if (s_dialoguePresenter != null && !s_dialoguePresenter.CanAdvance)
                    return;

                s_waitingContinue = false;
                if (!string.IsNullOrEmpty(node.nextNodeId) &&
                    !s_runner.TryAdvanceFromPresentation(node.nextNodeId, presentationVersion))
                    s_waitingContinue = true;
            });
        }

        static void CreateNoChoicesFallback(StoryNodeData node, int presentationVersion)
        {
            s_waitingContinue = false;
            if (!string.IsNullOrEmpty(node.nextNodeId))
            {
                var nextId = node.nextNodeId;
                UIBuilder.CreateButton(s_choiceArea, "\u7ee7\u7eed", () =>
                {
                    if (s_dialoguePresenter != null && !s_dialoguePresenter.CanAdvance) return;
                    s_runner.TryAdvanceFromPresentation(nextId, presentationVersion);
                });
            }
            else if (node.id != "hub_explore")
            {
                UIBuilder.CreateButton(s_choiceArea, "\u8fd4\u56de\u8c03\u67e5", () =>
                {
                    if (s_dialoguePresenter != null && !s_dialoguePresenter.CanAdvance) return;
                    s_runner.TryAdvanceFromPresentation("hub_explore", presentationVersion);
                });
            }
            else
            {
                UIBuilder.CreateButton(s_choiceArea, "\u8fd4\u56de\u4e3b\u83dc\u5355", SceneLoader.LoadMainMenu);
                AppendLog("\u5f53\u524d\u6ca1\u6709\u53ef\u7528\u7684\u8c03\u67e5\u884c\u52a8\u3002");
            }
        }

        static void ShowCombatUI()
        {
            s_inCombat = true;
            s_waitingContinue = false;
            s_presentedNode = null;
            s_pendingChoices = null;
            s_noChoicesAvailableNode = null;
            s_portraitPresenter?.Hide(false);
            var def = CombatEncounterDatabase.Get(s_runner.Combat.State?.encounterId);
            SetNarrativeTextImmediate($"\u3010\u6218\u6597\u3011{def?.displayName ?? "\u906d\u9047\u6218"}");
            RefreshCombatUI();
        }

        static void RefreshCombatUI()
        {
            if (!s_inCombat || s_runner?.Combat == null) return;

            var combat = s_runner.Combat;
            var state = combat.State;
            if (state == null) return;

            ClearChoices();

            if (state.ended)
            {
                s_inCombat = false;
                return;
            }

            var player = combat.GetPlayer();
            if (player != null)
            {
                var statusLines = new List<string>
                {
                    $"\u3010\u6218\u6597 \u00b7 \u7b2c {state.turnNumber} \u8f6e\u3011",
                    $"{player.displayName}  HP {player.HP}/{player.MaxHP}"
                };
                foreach (var enemy in combat.GetEnemies())
                    statusLines.Add($"{enemy.displayName}  HP {enemy.HP}/{enemy.MaxHP}");
                statusLines.Add(state.playerTurn ? "\u4f60\u7684\u56de\u5408" : "\u654c\u65b9\u884c\u52a8\u4e2d\u2026");
                SetNarrativeTextImmediate(string.Join("\n", statusLines));
            }

            if (!state.playerTurn) return;

            var enemies = combat.GetEnemies();
            var actionVersion = state.turnNumber;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var idx = i;
                UIBuilder.CreateButton(s_choiceArea,
                    $"\u653b\u51fb {enemy.displayName} (HP {enemy.HP}/{enemy.MaxHP})",
                    () => combat.PlayerAttack(idx, actionVersion));
            }

            UIBuilder.CreateButton(s_choiceArea, "\u95ea\u907f", () => combat.PlayerDodge(actionVersion));
            UIBuilder.CreateButton(s_choiceArea, "\u9003\u8d70", () => combat.PlayerFlee(actionVersion));
        }

        static void RefreshTime()
        {
            var gs = GameStateManager.Instance;
            if (gs == null || s_timeText == null) return;
            var loc = NPCDatabase.GetLocation(gs.CurrentLocationId);
            s_timeText.text = $"{gs.CurrentTime.DisplayString}  \u00b7  {loc?.displayName ?? gs.CurrentLocationId}";
        }

        static void SetNarrativeTextImmediate(string text)
        {
            if (s_dialoguePresenter != null)
            {
                s_dialoguePresenter.Present(text, animated: false);
                return;
            }

            if (s_narrativeText == null) return;
            s_narrativeText.text = text ?? "";
            s_narrativeText.maxVisibleCharacters = int.MaxValue;
        }

        static void RefreshSidebar()
        {
            if (s_sidebarArea == null || s_sidebarContent == null) return;
            var layout = s_sidebarContent;

            for (var i = layout.childCount - 1; i >= 1; i--)
                Object.Destroy(layout.GetChild(i).gameObject);

            var gs = GameStateManager.Instance;

            var inv = gs.Investigator;
            var status = inv == null
                ? "\u8c03\u67e5\u5458\uff1a\u672a\u521b\u5efa"
                : $"{inv.Name}\nHP {inv.HP}/{inv.MaxHP}  SAN {inv.SAN}/{inv.MaxSAN}  MP {inv.MP}/{inv.MaxMP}";
            CreateSidebarLabel(layout, "Status", status, 15, 50);

            CreateSidebarLabel(layout, "InventoryHeader", "\u2014 \u968f\u8eab\u7269\u54c1 \u2014", 16, 24);
            CreateSidebarLabel(layout, "Inventory", GetInventorySummary(gs), 14, 48);

            var explorationEnabled = s_runner != null &&
                s_runner.InteractionMode == ScenarioInteractionMode.Exploration &&
                !s_runner.IsCombatActive;
            if (gs?.Inventory != null)
            {
                var listedConsumables = new HashSet<string>();
                foreach (var itemId in gs.Inventory.Items)
                {
                    if (!listedConsumables.Add(itemId)) continue;
                    var item = ItemDatabase.Get(itemId);
                    if (item == null || !item.consumable) continue;
                    var capturedItemId = itemId;
                    var useButton = UIBuilder.CreateButton(
                        layout,
                        $"使用 {item.displayName}",
                        () => s_runner.UseItem(capturedItemId));
                    useButton.interactable = explorationEnabled;
                }
            }

            CreateSidebarLabel(layout, "LocationsHeader", "\u2014 \u5730\u70b9 \u2014", 16, 24);
            foreach (var loc in NPCDatabase.AllLocations)
            {
                var id = loc.id;
                var access = s_runner.GetLocationAccess(id);
                var label = loc.id == gs.CurrentLocationId ? $"[\u5f53\u524d] {loc.displayName}" : loc.displayName;
                if (!access.CanEnter)
                    label += $"\uff08{access.Message}\uff09";
                var button = UIBuilder.CreateButton(layout, label, () => s_runner.TravelToLocation(id));
                button.interactable = explorationEnabled && access.CanEnter;
            }

            CreateSidebarLabel(layout, "NpcsHeader", "\u2014 \u5f53\u524d NPC \u2014", 16, 24);
            var npcs = s_runner.GetAvailableNpcsAtLocation(gs.CurrentLocationId);
            foreach (var npc in npcs)
            {
                var id = npc.id;
                var button = UIBuilder.CreateButton(layout, npc.displayName, () => s_runner.TalkToNpc(id));
                button.interactable = explorationEnabled;
            }

            if (npcs.Count == 0)
                CreateSidebarLabel(layout, "NoNpc", "\uff08\u6b64\u65f6\u6b64\u5730\u65e0\u4eba\uff09", 14, 24);
        }

        static void RefreshBackground()
        {
            if (s_backgroundImage == null) return;

            var gs = GameStateManager.Instance;
            var location = gs == null ? null : NPCDatabase.GetLocation(gs.CurrentLocationId);
            var sprite = LocationArtDatabase.Get(location?.backgroundId);
            s_backgroundImage.sprite = sprite;
            s_backgroundImage.gameObject.SetActive(sprite != null);
        }

        static void RefreshInteractionControls()
        {
            if (s_runner == null) return;

            var explorationEnabled = s_runner.InteractionMode == ScenarioInteractionMode.Exploration &&
                !s_runner.IsCombatActive;
            if (s_waitNextPeriodButton != null)
                s_waitNextPeriodButton.interactable = explorationEnabled;
            if (s_waitNextDayButton != null)
                s_waitNextDayButton.interactable = explorationEnabled;
            if (s_saveButton != null)
                s_saveButton.interactable = s_runner.InteractionMode != ScenarioInteractionMode.Combat &&
                    !s_runner.IsCombatActive;

            RefreshSidebar();
        }

        static TMP_Text CreateSidebarLabel(Transform parent, string name, string text, int fontSize, float height)
        {
            var label = UIBuilder.CreateText(parent, name, text, fontSize, TextAlignmentOptions.TopLeft);
            label.rectTransform.sizeDelta = new Vector2(label.rectTransform.sizeDelta.x, height);
            var layout = label.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleHeight = 0;
            return label;
        }

        static string GetInventorySummary(GameStateManager gs)
        {
            if (gs?.Inventory == null || gs.Inventory.Items.Count == 0)
                return "\uff08\u7a7a\uff09";

            var counts = new Dictionary<string, int>();
            foreach (var itemId in gs.Inventory.Items)
            {
                if (!counts.ContainsKey(itemId)) counts[itemId] = 0;
                counts[itemId]++;
            }

            var labels = new List<string>();
            foreach (var pair in counts)
            {
                var item = ItemDatabase.Get(pair.Key);
                var count = pair.Value > 1 ? $" x{pair.Value}" : "";
                labels.Add((item?.displayName ?? pair.Key) + count);
            }
            return string.Join("\u3001", labels);
        }

        static void ShowPortrait(StoryNodeData node)
        {
            if (s_portraitPresenter == null) return;

            var portraitId = node?.portraitId;
            if (string.IsNullOrEmpty(portraitId) && !string.IsNullOrEmpty(node?.speaker))
            {
                foreach (var npc in NPCDatabase.AllNpcs)
                {
                    if (npc.displayName != node.speaker) continue;
                    portraitId = npc.portraitId;
                    break;
                }
            }

            var sprite = PortraitDatabase.Get(portraitId);
            if (sprite == null)
            {
                s_portraitPresenter.SetTalking(false);
                s_portraitPresenter.Hide();
                return;
            }

            s_portraitPresenter.Show(sprite);
            s_portraitPresenter.SetTalking(true);
        }

        static void ClearChoices()
        {
            if (s_choiceArea == null) return;
            for (var i = s_choiceArea.childCount - 1; i >= 0; i--)
                Object.Destroy(s_choiceArea.GetChild(i).gameObject);
            ScrollToTop(s_choiceScroll);
        }

        static void AppendLog(string msg)
        {
            if (string.IsNullOrEmpty(msg) || s_logText == null) return;
            s_logText.text = msg + "\n" + s_logText.text;
            if (s_logText.text.Length > 800)
                s_logText.text = s_logText.text.Substring(0, 800);
            ScrollToTop(s_logScroll);
        }

        static void ScrollToTop(ScrollRect scrollRect)
        {
            if (scrollRect == null) return;
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        static void SaveGame()
        {
            if (s_runner == null || s_runner.InteractionMode == ScenarioInteractionMode.Combat ||
                s_runner.IsCombatActive)
            {
                AppendLog("\u6218\u6597\u4e2d\u65e0\u6cd5\u5b58\u6863\u3002");
                return;
            }

            var gs = GameStateManager.Instance;
            if (SaveSystem.TrySave(1, gs.ToSaveData(), out var error))
            {
                gs.SaveSlot = 1;
                AppendLog("\u5df2\u5b58\u6863\uff08\u69fd\u4f4d 1\uff09");
            }
            else
            {
                AppendLog(error ?? "\u5b58\u6863\u5931\u8d25\u3002");
            }
        }

        static Button CreateSmallButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110, 36);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.26f, 0.32f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var text = UIBuilder.CreateText(go.transform, "Label", label, 16, TextAlignmentOptions.Center);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return btn;
        }
    }
}
