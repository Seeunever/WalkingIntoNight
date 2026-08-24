using System.Collections.Generic;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Dice;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.NPC;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Narrative
{
    public enum ScenarioInteractionMode
    {
        Narrative,
        Exploration,
        Combat,
        End
    }

    public class ScenarioRunner
    {
        const int MaxResumeTraversal = 128;
        const float MinPresentationInputInterval = 0.75f;

        public ScenarioFile Scenario { get; private set; }
        Dictionary<string, StoryNodeData> m_nodes;
        bool m_combatEventsWired;

        readonly CombatManager m_combat;
        readonly GameStateManager m_gameState;
        readonly System.Func<float> m_realtimeProvider;
        readonly System.Func<int, CheckDifficulty, string, int, int, CheckResult> m_skillCheck;
        ScenarioInteractionMode m_interactionMode = ScenarioInteractionMode.Narrative;
        int m_presentationVersion;
        int m_consumedPresentationVersion = -1;
        float m_nextPresentationInputTime = float.NegativeInfinity;
        public CombatManager Combat => m_combat;
        public ScenarioInteractionMode InteractionMode => m_interactionMode;
        public int PresentationVersion => m_presentationVersion;

        GameStateManager GameState => m_gameState != null
            ? m_gameState
            : GameStateManager.Instance;

        public ScenarioRunner() : this(null)
        {
        }

        public ScenarioRunner(
            GameStateManager gameState,
            System.Func<float> realtimeProvider = null,
            CombatManager combatManager = null,
            System.Func<int, CheckDifficulty, string, int, int, CheckResult> skillCheck = null)
        {
            m_gameState = gameState;
            m_realtimeProvider = realtimeProvider;
            m_combat = combatManager ?? new CombatManager();
            m_skillCheck = skillCheck ?? ((skill, difficulty, skillId, bonusDice, penaltyDice) =>
                DiceRoller.SkillCheck(skill, difficulty, skillId, bonusDice, penaltyDice));
        }

        public System.Action<StoryNodeData> OnNodePresented;
        public System.Action<List<StoryChoiceData>> OnChoicesPresented;
        public System.Action<StoryNodeData> OnNoChoicesAvailable;
        public System.Action<CheckResult> OnCheckResolved;
        public System.Action<string> OnLog;
        public System.Action OnScenarioEnded;
        public System.Action OnRequestCombatUI;
        public System.Action OnRequestLocationUI;
        public System.Action OnRequestNpcUI;
        public System.Action OnTimeChanged;
        public System.Action OnInventoryChanged;
        public System.Action<ScenarioInteractionMode> OnInteractionModeChanged;

        public bool IsCombatActive => m_combat.IsActive;

        public void LoadScenario(string scenarioId)
        {
            var entry = ScenarioRegistry.Get(scenarioId);
            var path = entry?.ResourcePath ?? "Data/Scenarios/Scenario_01/nodes";
            LoadScenario(ScenarioLoader.Load(path));
        }

        public void LoadScenario(ScenarioFile scenario)
        {
            Scenario = scenario;
            m_nodes = ScenarioLoader.BuildLookup(Scenario);

            if (m_combatEventsWired) return;
            m_combatEventsWired = true;
            m_combat.OnCombatLog += msg => OnLog?.Invoke(msg);
            m_combat.OnCombatUpdated += () =>
            {
                if (m_combat.State != null && m_combat.State.ended)
                    ResolveCombatEnd();
            };
        }

        public void StartFrom(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                nodeId = Scenario?.startNodeId;

            GameState.CurrentNodeId = nodeId;
            AdvanceTo(nodeId);
        }

        public bool ResumeFromSave(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                SetInteractionMode(ScenarioInteractionMode.End);
                OnLog?.Invoke("存档缺少当前节点。");
                OnScenarioEnded?.Invoke();
                return false;
            }

            return ResumeWithoutReapplyingEffects(
                nodeId,
                new HashSet<string>(),
                0);
        }

        public void Continue()
        {
            AdvanceTo(GameState.CurrentNodeId);
        }

        public void SelectChoice(StoryChoiceData choice)
        {
            TrySelectChoice(choice, m_presentationVersion);
        }

        public bool TrySelectChoice(StoryChoiceData choice, int presentationVersion)
        {
            if (choice == null || !ConditionEvaluator.MeetsChoiceRequirements(choice, GameState)) return false;
            if (!TryConsumePresentation(presentationVersion)) return false;
            AdvanceTo(choice.nextNodeId);
            return true;
        }

        public bool TryAdvanceFromPresentation(string nodeId, int presentationVersion)
        {
            if (!TryConsumePresentation(presentationVersion)) return false;
            AdvanceTo(nodeId);
            return true;
        }

        public void AdvanceTo(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || m_nodes == null)
            {
                SetInteractionMode(ScenarioInteractionMode.End);
                OnScenarioEnded?.Invoke();
                return;
            }

            if (!m_nodes.TryGetValue(nodeId, out var node))
            {
                SetInteractionMode(ScenarioInteractionMode.End);
                OnLog?.Invoke($"缺失节点: {nodeId}");
                OnScenarioEnded?.Invoke();
                return;
            }

            GameState.CurrentNodeId = nodeId;
            SetInteractionMode(GetInteractionMode(node));
            ProcessNode(node);
        }

        void ProcessNode(StoryNodeData node)
        {
            var type = StoryNodeTypeParser.Parse(node.type);

            switch (type)
            {
                case StoryNodeType.SetFlag:
                {
                    var wasSet = GameState.HasFlag(node.flag);
                    GameState.SetFlag(node.flag, node.flagValue);
                    if (!string.IsNullOrWhiteSpace(node.flagNotice))
                        OnLog?.Invoke($"【新线索】{node.flagNotice}");
                    if (node.flagValue && !wasSet)
                        AnnounceRelationshipsUnlockedByFlag(node.flag);
                    if (!string.IsNullOrEmpty(node.text))
                        PresentNode(node);
                    else
                        AdvanceTo(node.nextNodeId);
                    break;
                }

                case StoryNodeType.GiveItem:
                {
                    GameState.Inventory.AddItem(node.itemId, node.itemCount);
                    var itemName = ItemDatabase.Get(node.itemId)?.displayName ?? node.itemId;
                    var countLabel = node.itemCount > 1 ? $" ×{node.itemCount}" : "";
                    OnLog?.Invoke($"【获得物品】{itemName}{countLabel}");
                    OnInventoryChanged?.Invoke();
                    if (!string.IsNullOrEmpty(node.text))
                        PresentNode(node);
                    else
                        AdvanceTo(node.nextNodeId);
                    break;
                }

                case StoryNodeType.ChangeSan:
                    ApplySanDelta(node.sanDelta);
                    if (!string.IsNullOrEmpty(node.text))
                        PresentNode(node);
                    else
                        AdvanceTo(node.nextNodeId);
                    break;

                case StoryNodeType.Check:
                    ResolveCheck(node);
                    break;

                case StoryNodeType.Combat:
                    StartCombat(node);
                    break;

                case StoryNodeType.Location:
                    if (!string.IsNullOrEmpty(node.locationId))
                        GameState.CurrentLocationId = node.locationId;
                    PresentNodeWithChoices(node);
                    OnRequestLocationUI?.Invoke();
                    break;

                case StoryNodeType.NpcHub:
                    PresentNode(node);
                    OnRequestNpcUI?.Invoke();
                    break;

                case StoryNodeType.AdvanceTime:
                    ApplyAdvanceTime(node);
                    if (!string.IsNullOrEmpty(node.text))
                        PresentNode(node);
                    else
                        AdvanceTo(node.nextNodeId);
                    break;

                case StoryNodeType.End:
                    PresentNode(node);
                    OnScenarioEnded?.Invoke();
                    break;

                default:
                    if (!string.IsNullOrEmpty(node.locationId))
                        GameState.CurrentLocationId = node.locationId;
                    PresentNodeWithChoices(node);
                    break;
            }
        }

        bool ResumeWithoutReapplyingEffects(
            string nodeId,
            HashSet<string> visited,
            int depth)
        {
            if (depth >= MaxResumeTraversal || !visited.Add(nodeId))
            {
                SetInteractionMode(ScenarioInteractionMode.End);
                OnLog?.Invoke("存档恢复遇到循环节点，已停止推进。");
                OnScenarioEnded?.Invoke();
                return false;
            }

            if (m_nodes == null || !m_nodes.TryGetValue(nodeId, out var node))
            {
                SetInteractionMode(ScenarioInteractionMode.End);
                OnLog?.Invoke($"存档节点不存在: {nodeId}");
                OnScenarioEnded?.Invoke();
                return false;
            }

            GameState.CurrentNodeId = nodeId;
            SetInteractionMode(GetInteractionMode(node));
            var type = StoryNodeTypeParser.Parse(node.type);
            switch (type)
            {
                case StoryNodeType.SetFlag:
                case StoryNodeType.GiveItem:
                case StoryNodeType.ChangeSan:
                case StoryNodeType.AdvanceTime:
                    if (!string.IsNullOrEmpty(node.text))
                    {
                        PresentNode(node);
                        return true;
                    }

                    if (string.IsNullOrEmpty(node.nextNodeId))
                    {
                        SetInteractionMode(ScenarioInteractionMode.End);
                        OnLog?.Invoke($"自动节点 {node.id} 缺少后继节点。");
                        OnScenarioEnded?.Invoke();
                        return false;
                    }

                    return ResumeWithoutReapplyingEffects(
                        node.nextNodeId,
                        visited,
                        depth + 1);

                case StoryNodeType.Location:
                    PresentNodeWithChoices(node);
                    OnRequestLocationUI?.Invoke();
                    return true;

                case StoryNodeType.NpcHub:
                    PresentNode(node);
                    OnRequestNpcUI?.Invoke();
                    return true;

                case StoryNodeType.End:
                    PresentNode(node);
                    OnScenarioEnded?.Invoke();
                    return true;

                case StoryNodeType.Combat:
                case StoryNodeType.Check:
                    SetInteractionMode(ScenarioInteractionMode.End);
                    OnLog?.Invoke("该存档停在无法安全恢复的战斗或检定节点。");
                    OnScenarioEnded?.Invoke();
                    return false;

                default:
                    PresentNodeWithChoices(node);
                    return true;
            }
        }

        void PresentNodeWithChoices(StoryNodeData node)
        {
            if (node.choices != null && node.choices.Count > 0)
            {
                PresentNode(node);
                var choices = new List<StoryChoiceData>();
                foreach (var c in node.choices)
                {
                    if (c != null)
                        choices.Add(c);
                }
                if (choices.Count > 0)
                    OnChoicesPresented?.Invoke(choices);
                else
                    OnNoChoicesAvailable?.Invoke(node);
            }
            else
                PresentNode(node);
        }

        void ResolveCheck(StoryNodeData node)
        {
            var inv = GameState.Investigator;
            var skill = inv != null ? inv.GetSkill(node.skillId) : 0;
            var diff = (CheckDifficulty)Mathf.Clamp(node.difficulty, 0, 2);
            var result = m_skillCheck(
                skill,
                diff,
                node.skillId,
                node.bonusDice,
                node.penaltyDice);
            OnCheckResolved?.Invoke(result);
            OnLog?.Invoke(result.Summary);

            if (!string.IsNullOrEmpty(node.text))
                PresentNode(node);

            AdvanceTo(result.IsSuccess ? node.successNodeId : node.failureNodeId);
        }

        void ApplyAdvanceTime(StoryNodeData node)
        {
            var gs = GameState;
            if (node.advancePeriods > 0)
                gs.AdvanceTimePeriods(node.advancePeriods);
            if (node.advanceDays > 0)
                gs.AdvanceTimeDays(node.advanceDays);
            OnLog?.Invoke($"时间推进：{gs.CurrentTime.DisplayString}");
            OnTimeChanged?.Invoke();
        }

        void ApplySanDelta(int delta)
        {
            var inv = GameState.Investigator;
            if (inv == null) return;
            inv.SAN = Mathf.Clamp(inv.SAN + delta, 0, inv.MaxSAN);
            OnLog?.Invoke(delta < 0 ? $"理智下降 {Mathf.Abs(delta)}，当前 {inv.SAN}/{inv.MaxSAN}" : $"理智恢复 {delta}，当前 {inv.SAN}/{inv.MaxSAN}");
        }

        void StartCombat(StoryNodeData node)
        {
            var gs = GameState;
            ClearPendingCombatState(gs);

            if (!m_combat.TryStartEncounter(node.combatId, gs.Investigator, out var error))
            {
                m_combat.ClearEncounter();
                OnLog?.Invoke(error ?? $"战斗无法开始：{node.combatId ?? "（空）"}。");
                ContinueFromCombatFallback(node);
                return;
            }

            gs.PostCombatNodeId = node.winNodeId;
            gs.HasPendingCombatReturn = true;
            gs.ActiveCombat = m_combat.State;
            OnRequestCombatUI?.Invoke();
        }

        void ResolveCombatEnd()
        {
            var combat = m_combat.State;
            if (combat == null) return;

            m_combat.SyncPlayerToInvestigator();
            var gs = GameState;
            var combatNode = m_nodes != null && m_nodes.TryGetValue(gs.CurrentNodeId, out var node)
                ? node
                : null;
            string next;
            string outcome;

            if (combat.playerFled)
            {
                next = combatNode?.fleeNodeId;
                outcome = "逃跑";
            }
            else if (combat.playerWon)
            {
                next = gs.PostCombatNodeId;
                outcome = "胜利";
            }
            else
            {
                next = combatNode?.loseNodeId;
                outcome = "失败";
            }

            if (!IsKnownNode(next))
            {
                OnLog?.Invoke($"战斗{outcome}出口不存在：{next ?? "（空）"}。");
                next = FindSafeCombatFallback(combatNode);
            }

            ClearPendingCombatState(gs);
            m_combat.ClearEncounter();
            SetInteractionMode(ScenarioInteractionMode.Narrative);

            if (IsKnownNode(next))
                AdvanceTo(next);
            else
            {
                SetInteractionMode(ScenarioInteractionMode.End);
                OnScenarioEnded?.Invoke();
            }
        }

        void ContinueFromCombatFallback(StoryNodeData combatNode)
        {
            var fallback = FindSafeCombatFallback(combatNode);
            SetInteractionMode(ScenarioInteractionMode.Narrative);
            if (IsKnownNode(fallback))
            {
                AdvanceTo(fallback);
                return;
            }

            SetInteractionMode(ScenarioInteractionMode.End);
            OnScenarioEnded?.Invoke();
        }

        string FindSafeCombatFallback(StoryNodeData combatNode)
        {
            if (IsKnownNode(combatNode?.fleeNodeId))
                return combatNode.fleeNodeId;
            if (IsKnownNode("hub_explore"))
                return "hub_explore";
            return null;
        }

        bool IsKnownNode(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && m_nodes != null && m_nodes.ContainsKey(nodeId);
        }

        static void ClearPendingCombatState(GameStateManager gameState)
        {
            if (gameState == null) return;
            gameState.HasPendingCombatReturn = false;
            gameState.PostCombatNodeId = null;
            gameState.ActiveCombat = null;
        }

        public bool TalkToNpc(string npcId)
        {
            if (!RequireExplorationMode()) return false;
            var npc = NPCDatabase.GetNpc(npcId);
            if (npc == null) return false;
            AdvanceTo(npc.defaultNodeId);
            return true;
        }

        public bool TravelToLocation(string locationId)
        {
            if (!RequireExplorationMode()) return false;
            var access = GetLocationAccess(locationId);
            if (!access.CanEnter)
            {
                OnLog?.Invoke($"无法前往地点：{access.Message}");
                return false;
            }

            GameState.CurrentLocationId = locationId;
            var loc = NPCDatabase.GetLocation(locationId);
            OnLog?.Invoke($"前往：{loc?.displayName ?? locationId}");
            AdvanceTo("hub_explore");
            return true;
        }

        public LocationAccessResult GetLocationAccess(string locationId)
        {
            return LocationAccessEvaluator.Evaluate(
                NPCDatabase.GetLocation(locationId),
                GameState);
        }

        public bool WaitNextPeriod()
        {
            if (!RequireExplorationMode()) return false;
            var gs = GameState;
            gs.AdvanceTimePeriods(1);
            OnLog?.Invoke($"等待至：{gs.CurrentTime.DisplayString}");
            OnTimeChanged?.Invoke();
            RefreshHub();
            return true;
        }

        public bool WaitNextDay()
        {
            if (!RequireExplorationMode()) return false;
            var gs = GameState;
            gs.WaitUntilNextDayMorning();
            OnLog?.Invoke($"等待至：{gs.CurrentTime.DisplayString}");
            OnTimeChanged?.Invoke();
            RefreshHub();
            return true;
        }

        public bool UseItem(string itemId)
        {
            if (!RequireExplorationMode()) return false;

            var item = ItemDatabase.Get(itemId);
            if (item == null || !GameState.Inventory.HasItem(itemId))
            {
                OnLog?.Invoke("背包里没有这个物品。");
                return false;
            }

            if (!item.consumable)
            {
                OnLog?.Invoke($"「{item.displayName}」不是可消耗物品。");
                return false;
            }

            var investigator = GameState.Investigator;
            if (investigator == null) return false;

            var oldHp = investigator.HP;
            var oldSan = investigator.SAN;
            investigator.HP = Mathf.Clamp(investigator.HP + item.healHp, 0, investigator.MaxHP);
            investigator.SAN = Mathf.Clamp(investigator.SAN + item.healSan, 0, investigator.MaxSAN);
            var healedHp = investigator.HP - oldHp;
            var healedSan = investigator.SAN - oldSan;
            var hasStoryUse = !string.IsNullOrEmpty(item.useNodeId) && IsKnownNode(item.useNodeId);

            if (healedHp <= 0 && healedSan <= 0 && !hasStoryUse)
            {
                OnLog?.Invoke($"目前不需要使用「{item.displayName}」。");
                return false;
            }

            GameState.Inventory.RemoveItem(itemId);
            var effects = new List<string>();
            if (healedHp > 0) effects.Add($"HP +{healedHp}");
            if (healedSan > 0) effects.Add($"SAN +{healedSan}");
            var effectLabel = effects.Count > 0 ? $"：{string.Join("，", effects)}" : "";
            OnLog?.Invoke($"【使用物品】{item.displayName}{effectLabel}");
            OnInventoryChanged?.Invoke();

            if (hasStoryUse)
                AdvanceTo(item.useNodeId);
            return true;
        }

        public List<NPCDefinition> GetAvailableNpcsAtLocation(string locationId)
        {
            var gs = GameState;
            return NPCDatabase.GetAvailableNpcsAtLocation(
                locationId ?? gs.CurrentLocationId,
                gs.CurrentTime);
        }

        void RefreshHub()
        {
            var nodeId = GameState.CurrentNodeId;
            if (string.IsNullOrEmpty(nodeId) || m_nodes == null) return;
            if (!m_nodes.TryGetValue(nodeId, out var node)) return;

            var type = StoryNodeTypeParser.Parse(node.type);
            if (type == StoryNodeType.Location ||
                (type == StoryNodeType.Dialogue && node.choices != null && node.choices.Count > 0))
            {
                ProcessNode(node);
            }
            else
            {
                OnRequestLocationUI?.Invoke();
            }
        }

        void PresentNode(StoryNodeData node)
        {
            m_presentationVersion++;
            m_consumedPresentationVersion = -1;
            OnNodePresented?.Invoke(node);
        }

        void AnnounceRelationshipsUnlockedByFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return;
            foreach (var relationship in NPCDatabase.AllRelationships)
            {
                if (relationship == null || relationship.unlockFlag != flag) continue;
                var fromName = NPCDatabase.GetNpc(relationship.fromNpcId)?.displayName ?? relationship.fromNpcId;
                var toName = NPCDatabase.GetNpc(relationship.toNpcId)?.displayName ?? relationship.toNpcId;
                OnLog?.Invoke($"【关系线索】{fromName}与{toName}：{relationship.label}");
            }
        }

        bool TryConsumePresentation(int presentationVersion)
        {
            if (presentationVersion != m_presentationVersion ||
                m_consumedPresentationVersion == presentationVersion)
                return false;

            var currentTime = m_realtimeProvider != null
                ? m_realtimeProvider()
                : Time.realtimeSinceStartup;
            if (currentTime <= m_nextPresentationInputTime)
                return false;

            m_consumedPresentationVersion = presentationVersion;
            m_nextPresentationInputTime = currentTime + MinPresentationInputInterval;
            return true;
        }

        bool RequireExplorationMode()
        {
            if (m_interactionMode == ScenarioInteractionMode.Exploration && !m_combat.IsActive)
                return true;

            OnLog?.Invoke("当前剧情尚未回到自由调查状态。");
            return false;
        }

        ScenarioInteractionMode GetInteractionMode(StoryNodeData node)
        {
            var type = StoryNodeTypeParser.Parse(node?.type);
            if (type == StoryNodeType.Combat) return ScenarioInteractionMode.Combat;
            if (type == StoryNodeType.End) return ScenarioInteractionMode.End;
            return node != null && node.allowExploration
                ? ScenarioInteractionMode.Exploration
                : ScenarioInteractionMode.Narrative;
        }

        void SetInteractionMode(ScenarioInteractionMode mode)
        {
            if (m_interactionMode == mode) return;
            m_interactionMode = mode;
            OnInteractionModeChanged?.Invoke(mode);
        }
    }
}
