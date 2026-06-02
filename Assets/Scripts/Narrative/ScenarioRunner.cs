using System.Collections.Generic;
using AnimalCafe.TRPG.Character;
using AnimalCafe.TRPG.Combat;
using AnimalCafe.TRPG.Core;
using AnimalCafe.TRPG.Dice;
using AnimalCafe.TRPG.Inventory;
using AnimalCafe.TRPG.NPC;
using UnityEngine;

namespace AnimalCafe.TRPG.Narrative
{
    public class ScenarioRunner
    {
        public ScenarioFile Scenario { get; private set; }
        Dictionary<string, StoryNodeData> m_nodes;

        readonly CombatManager m_combat = new CombatManager();
        public CombatManager Combat => m_combat;

        public System.Action<StoryNodeData> OnNodePresented;
        public System.Action<List<StoryChoiceData>> OnChoicesPresented;
        public System.Action<CheckResult> OnCheckResolved;
        public System.Action<string> OnLog;
        public System.Action OnScenarioEnded;
        public System.Action OnRequestCombatUI;
        public System.Action OnRequestLocationUI;
        public System.Action OnRequestNpcUI;

        public bool IsCombatActive => m_combat.IsActive;

        public void LoadScenario(string scenarioId)
        {
            var entry = ScenarioRegistry.Get(scenarioId);
            var path = entry?.ResourcePath ?? "Data/Scenarios/Scenario_01/nodes";
            Scenario = ScenarioLoader.Load(path);
            m_nodes = ScenarioLoader.BuildLookup(Scenario);

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

            GameStateManager.Instance.CurrentNodeId = nodeId;
            AdvanceTo(nodeId);
        }

        public void Continue()
        {
            AdvanceTo(GameStateManager.Instance.CurrentNodeId);
        }

        public void SelectChoice(StoryChoiceData choice)
        {
            if (choice == null || !ConditionEvaluator.MeetsChoiceRequirements(choice)) return;
            AdvanceTo(choice.nextNodeId);
        }

        public void AdvanceTo(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || m_nodes == null)
            {
                OnScenarioEnded?.Invoke();
                return;
            }

            if (!m_nodes.TryGetValue(nodeId, out var node))
            {
                OnLog?.Invoke($"缺失节点: {nodeId}");
                OnScenarioEnded?.Invoke();
                return;
            }

            GameStateManager.Instance.CurrentNodeId = nodeId;
            ProcessNode(node);
        }

        void ProcessNode(StoryNodeData node)
        {
            var type = StoryNodeTypeParser.Parse(node.type);

            switch (type)
            {
                case StoryNodeType.SetFlag:
                    GameStateManager.Instance.SetFlag(node.flag, node.flagValue);
                    OnLog?.Invoke($"标记更新: {node.flag}");
                    if (!string.IsNullOrEmpty(node.text))
                        OnNodePresented?.Invoke(node);
                    else
                        AdvanceTo(node.nextNodeId);
                    break;

                case StoryNodeType.GiveItem:
                    GameStateManager.Instance.Inventory.AddItem(node.itemId, node.itemCount);
                    OnLog?.Invoke($"获得物品: {ItemDatabase.Get(node.itemId)?.displayName ?? node.itemId}");
                    if (!string.IsNullOrEmpty(node.text))
                        OnNodePresented?.Invoke(node);
                    else
                        AdvanceTo(node.nextNodeId);
                    break;

                case StoryNodeType.ChangeSan:
                    ApplySanDelta(node.sanDelta);
                    if (!string.IsNullOrEmpty(node.text))
                        OnNodePresented?.Invoke(node);
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
                        GameStateManager.Instance.CurrentLocationId = node.locationId;
                    OnNodePresented?.Invoke(node);
                    OnRequestLocationUI?.Invoke();
                    break;

                case StoryNodeType.NpcHub:
                    OnNodePresented?.Invoke(node);
                    OnRequestNpcUI?.Invoke();
                    break;

                case StoryNodeType.End:
                    OnNodePresented?.Invoke(node);
                    OnScenarioEnded?.Invoke();
                    break;

                default:
                    if (!string.IsNullOrEmpty(node.locationId))
                        GameStateManager.Instance.CurrentLocationId = node.locationId;

                    if (node.choices != null && node.choices.Count > 0)
                    {
                        OnNodePresented?.Invoke(node);
                        var valid = new List<StoryChoiceData>();
                        foreach (var c in node.choices)
                        {
                            if (ConditionEvaluator.MeetsChoiceRequirements(c))
                                valid.Add(c);
                        }
                        OnChoicesPresented?.Invoke(valid);
                    }
                    else
                        OnNodePresented?.Invoke(node);
                    break;
            }
        }

        void ResolveCheck(StoryNodeData node)
        {
            var inv = GameStateManager.Instance.Investigator;
            var skill = inv != null ? inv.GetSkill(node.skillId) : 0;
            var diff = (CheckDifficulty)Mathf.Clamp(node.difficulty, 0, 2);
            var result = DiceRoller.SkillCheck(skill, diff, node.skillId, node.bonusDice, node.penaltyDice);
            OnCheckResolved?.Invoke(result);
            OnLog?.Invoke(result.Summary);

            if (!string.IsNullOrEmpty(node.text))
                OnNodePresented?.Invoke(node);

            AdvanceTo(result.IsSuccess ? node.successNodeId : node.failureNodeId);
        }

        void ApplySanDelta(int delta)
        {
            var inv = GameStateManager.Instance.Investigator;
            if (inv == null) return;
            inv.SAN = Mathf.Clamp(inv.SAN + delta, 0, inv.MaxSAN);
            OnLog?.Invoke(delta < 0 ? $"理智下降 {Mathf.Abs(delta)}，当前 {inv.SAN}/{inv.MaxSAN}" : $"理智恢复 {delta}，当前 {inv.SAN}/{inv.MaxSAN}");
        }

        void StartCombat(StoryNodeData node)
        {
            GameStateManager.Instance.PostCombatNodeId = node.winNodeId;
            GameStateManager.Instance.HasPendingCombatReturn = true;
            m_combat.StartEncounter(node.combatId, GameStateManager.Instance.Investigator);
            OnRequestCombatUI?.Invoke();
        }

        void ResolveCombatEnd()
        {
            var combat = m_combat.State;
            if (combat == null) return;

            m_combat.SyncPlayerToInvestigator();
            var gs = GameStateManager.Instance;
            string next;

            if (combat.playerFled)
                next = m_nodes.TryGetValue(gs.CurrentNodeId, out var n) ? n.fleeNodeId : gs.PostCombatNodeId;
            else if (combat.playerWon)
                next = gs.PostCombatNodeId;
            else
                next = m_nodes.TryGetValue(gs.CurrentNodeId, out var n2) ? n2.loseNodeId : "end_bad";

            gs.HasPendingCombatReturn = false;
            m_combat.ClearEncounter();
            AdvanceTo(next);
        }

        public void TalkToNpc(string npcId)
        {
            var npc = NPCDatabase.GetNpc(npcId);
            if (npc == null) return;
            AdvanceTo(npc.defaultNodeId);
        }

        public void TravelToLocation(string locationId)
        {
            GameStateManager.Instance.CurrentLocationId = locationId;
            var loc = NPCDatabase.GetLocation(locationId);
            OnLog?.Invoke($"前往：{loc?.displayName ?? locationId}");
            AdvanceTo("hub_explore");
        }
    }
}
