using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Dice;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class InvestigationLoopTests
    {
        GameObject m_stateObject;
        GameStateManager m_state;
        float m_now;

        [SetUp]
        public void SetUp()
        {
            m_stateObject = new GameObject("GameStateManager_InvestigationLoopTest");
            m_state = m_stateObject.AddComponent<GameStateManager>();
            m_state.ResetForNewGame();
            m_state.SetInvestigator(new Investigator
            {
                Name = "调查闭环测试员",
                HP = 6,
                MaxHP = 10,
                SAN = 40,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            });
            m_now = 1f;
            NPCDatabase.Reload();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_stateObject);
        }

        [Test]
        public void Hub_PresentsLockedChoicesWithSpecificReasons_AndUnlocksWithoutReloading()
        {
            var runner = ScenarioRunner();
            List<StoryChoiceData> presented = null;
            runner.OnChoicesPresented = choices => presented = choices;

            runner.StartFrom("hub_explore");

            Assert.That(presented, Has.Count.EqualTo(4));
            var basement = presented.Single(choice => choice.nextNodeId == "goto_basement_check");
            var result = ConditionEvaluator.EvaluateChoice(basement, m_state);
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.BlockReason, Is.EqualTo(ChoiceBlockReason.MissingItem));
            Assert.That(result.Reason, Is.EqualTo("需要先在储藏室找到生锈的钥匙。"));
            Assert.That(runner.TrySelectChoice(basement, runner.PresentationVersion), Is.False);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("hub_explore"));

            m_state.Inventory.AddItem("rusty_key");
            m_now += 1f;

            Assert.That(ConditionEvaluator.EvaluateChoice(basement, m_state).IsAvailable, Is.True);
            Assert.That(runner.TrySelectChoice(basement, runner.PresentationVersion), Is.True);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("goto_basement_check"));
        }

        [Test]
        public void FinalChoice_ExplainsMissingRitualKnowledge()
        {
            var scenario = ScenarioLoader.Load("Data/Scenarios/Scenario_01/nodes");
            var final = scenario.nodes.Single(node => node.id == "final_choice");
            var ritual = final.choices.Single(choice => choice.nextNodeId == "end_good");

            var blocked = ConditionEvaluator.EvaluateChoice(ritual, m_state);

            Assert.That(blocked.IsAvailable, Is.False);
            Assert.That(blocked.BlockReason, Is.EqualTo(ChoiceBlockReason.MissingFlag));
            Assert.That(blocked.Reason, Is.EqualTo("需要先读懂地下室墙上的仪式符号。"));
            m_state.SetFlag("know_ritual");
            Assert.That(ConditionEvaluator.EvaluateChoice(ritual, m_state).IsAvailable, Is.True);
        }

        [Test]
        public void Consumable_UsesExactConfiguredEffectAndRemovesOneCopy()
        {
            var runner = ScenarioRunner();
            var logs = new List<string>();
            var inventoryUpdates = 0;
            runner.OnLog = logs.Add;
            runner.OnInventoryChanged = () => inventoryUpdates++;
            m_state.Inventory.AddItem("first_aid_kit", 2);
            runner.StartFrom("hub_explore");

            Assert.That(runner.UseItem("first_aid_kit"), Is.True);

            Assert.That(m_state.Investigator.HP, Is.EqualTo(9));
            Assert.That(m_state.Inventory.Items.Count(id => id == "first_aid_kit"), Is.EqualTo(1));
            Assert.That(inventoryUpdates, Is.EqualTo(1));
            Assert.That(logs, Has.Some.EqualTo("【使用物品】急救包：HP +3"));
        }

        [Test]
        public void Consumable_AtFullStatIsNotWasted()
        {
            var runner = ScenarioRunner();
            var logs = new List<string>();
            runner.OnLog = logs.Add;
            m_state.Investigator.HP = m_state.Investigator.MaxHP;
            m_state.Inventory.AddItem("first_aid_kit");
            runner.StartFrom("hub_explore");

            Assert.That(runner.UseItem("first_aid_kit"), Is.False);

            Assert.That(m_state.Inventory.HasItem("first_aid_kit"), Is.True);
            Assert.That(logs, Has.Some.Contains("目前不需要使用"));
        }

        [Test]
        public void MeiTrust_UnlocksRelationshipAndPublishesFriendlyLog()
        {
            var runner = ScenarioRunner();
            var logs = new List<string>();
            runner.OnLog = logs.Add;

            runner.StartFrom("mei_reveal");

            Assert.That(m_state.HasRelationshipUnlocked("mei_chen"), Is.True);
            Assert.That(logs, Has.Some.EqualTo("【关系线索】店员小梅与常客老陈：互相照应"));
        }

        [TestCase("set_motive_duty", "motive_duty")]
        [TestCase("set_motive_memory", "motive_memory")]
        [TestCase("set_motive_kindness", "motive_kindness")]
        public void OpeningMotive_SetsChosenFlagAndContinuesIntoCafe(
            string nodeId,
            string expectedFlag)
        {
            var runner = ScenarioRunner();
            List<StoryChoiceData> choices = null;
            runner.OnChoicesPresented = value => choices = value;

            runner.StartFrom("intro_motive");
            Select(runner, choices.Single(choice => choice.nextNodeId == nodeId));

            Assert.That(m_state.HasFlag(expectedFlag), Is.True);
            Assert.That(
                new[] { "motive_duty", "motive_memory", "motive_kindness" }
                    .Count(m_state.HasFlag),
                Is.EqualTo(1));
            Assert.That(m_state.CurrentNodeId, Is.EqualTo(nodeId));
            AdvanceCurrent(runner);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("intro_02"));
        }

        [Test]
        public void OpeningClue_ConvergesOnThreeWitnessesAndCatRouteReturnsToHub()
        {
            var runner = ScenarioRunner();
            List<StoryChoiceData> choices = null;
            runner.OnChoicesPresented = value => choices = value;
            Assert.That(
                runner.Scenario.nodes.Single(node => node.id == "intro_01").text,
                Does.Contain("午夜"));
            Assert.That(
                runner.Scenario.nodes.Single(node => node.id == "intro_02").text,
                Does.Contain("三只杯子"));

            runner.StartFrom("intro_cat_mirror");
            AdvanceCurrent(runner);

            Assert.That(m_state.HasFlag("cat_mirror"), Is.True);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("intro_03"));
            Select(runner, choices.Single(choice =>
                choice.nextNodeId == "intro_email_shown"));
            AdvanceCurrent(runner);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("intro_hall_threads"));

            runner.StartFrom("intro_03");
            Select(runner, choices.Single(choice =>
                choice.nextNodeId == "intro_third_cup"));
            AdvanceCurrent(runner);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("intro_hall_threads"));
            Assert.That(
                choices.Select(choice => choice.nextNodeId),
                Is.EquivalentTo(new[]
                {
                    "npc_mei_talk",
                    "npc_chen_talk",
                    "npc_cat_talk",
                    "hub_explore"
                }));

            Select(runner, choices.Single(choice =>
                choice.nextNodeId == "npc_cat_talk"));
            AdvanceCurrent(runner);

            Assert.That(m_state.HasFlag("cat_guided"), Is.True);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("hub_explore"));
            Assert.That(runner.InteractionMode,
                Is.EqualTo(ScenarioInteractionMode.Exploration));
        }

        [Test]
        public void SilverCoinRoute_ClosesInvestigationLoopAtNeutralEnding()
        {
            var runner = ScenarioRunner();
            List<StoryChoiceData> choices = null;
            runner.OnChoicesPresented = value => choices = value;

            runner.StartFrom("check_spot_main");
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("spot_success"));
            AdvanceCurrent(runner);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("intro_hall_threads"));
            Assert.That(m_state.Inventory.HasItem("silver_coin"), Is.True);
            Assert.That(m_state.HasFlag("found_coin"), Is.True);
            Select(runner, choices.Single(choice => choice.nextNodeId == "hub_explore"));
            Select(runner, choices.Single(choice => choice.nextNodeId == "midnight_event"));
            AdvanceCurrent(runner);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("final_choice"));
            Select(runner, choices.Single(choice => choice.nextNodeId == "end_neutral"));

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("end_neutral"));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.End));
        }

        [Test]
        public void FailedRoll_ReturnsToHubAndLeavesRetryRouteAvailable()
        {
            var runner = new ScenarioRunner(
                m_state,
                () => m_now,
                skillCheck: (skill, difficulty, skillId, bonusDice, penaltyDice) =>
                    new CheckResult { ResultType = CheckResultType.Failure });
            runner.LoadScenario(ScenarioRegistry.DefaultScenarioId);
            List<StoryChoiceData> choices = null;
            runner.OnChoicesPresented = value => choices = value;

            runner.StartFrom("check_spot_main");
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("spot_fail"));
            AdvanceCurrent(runner);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("intro_hall_threads"));
            Select(runner, choices.Single(choice => choice.nextNodeId == "hub_explore"));
            var storage = choices.Single(choice => choice.nextNodeId == "goto_storage");
            Select(runner, storage);
            AdvanceCurrent(runner);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("hub_explore"));
            Assert.That(choices, Has.Some.Matches<StoryChoiceData>(choice =>
                choice.nextNodeId == "goto_storage"));
        }

        [Test]
        public void Scenario01_AllNonEndNodesHaveKnownExit_AndFourEndingsRemainReachable()
        {
            var scenario = ScenarioLoader.Load("Data/Scenarios/Scenario_01/nodes");
            var nodeIds = new HashSet<string>(scenario.nodes.Select(node => node.id));
            var endings = scenario.nodes.Where(node =>
                StoryNodeTypeParser.Parse(node.type) == StoryNodeType.End).ToList();

            Assert.That(endings.Select(node => node.id), Is.EquivalentTo(new[]
            {
                "end_good",
                "end_neutral",
                "end_madness",
                "end_bad"
            }));

            foreach (var node in scenario.nodes.Except(endings))
            {
                var exits = ExitIds(node).Where(id => !string.IsNullOrEmpty(id)).ToList();
                Assert.That(exits, Is.Not.Empty, $"非结局节点 {node.id} 没有出口。");
                Assert.That(exits.All(nodeIds.Contains), Is.True, $"节点 {node.id} 存在未知出口。");
            }

            var reachable = new HashSet<string>();
            var pending = new Queue<string>();
            pending.Enqueue(scenario.startNodeId);
            while (pending.Count > 0)
            {
                var nodeId = pending.Dequeue();
                if (!reachable.Add(nodeId)) continue;
                var node = scenario.nodes.Single(candidate => candidate.id == nodeId);
                foreach (var exit in ExitIds(node))
                    if (!string.IsNullOrEmpty(exit) && nodeIds.Contains(exit))
                        pending.Enqueue(exit);
            }

            Assert.That(
                endings.Select(node => node.id).Where(id => !reachable.Contains(id)),
                Is.Empty,
                "四个结局都必须能从开场沿剧情出口抵达。");

            var midnightCheck = scenario.nodes.Single(node => node.id == "midnight_san_check");
            var recovery = scenario.nodes.Single(node => node.id == midnightCheck.failureNodeId);
            Assert.That(recovery.choices, Has.Some.Matches<StoryChoiceData>(choice =>
                choice.nextNodeId == "midnight_failure_cost"));
            Assert.That(scenario.nodes.Single(node => node.id == "midnight_failure_cost").nextNodeId,
                Is.EqualTo("final_choice"));
        }

        ScenarioRunner ScenarioRunner()
        {
            var runner = new ScenarioRunner(
                m_state,
                () => m_now,
                skillCheck: (skill, difficulty, skillId, bonusDice, penaltyDice) =>
                    new CheckResult { ResultType = CheckResultType.RegularSuccess });
            runner.LoadScenario(ScenarioRegistry.DefaultScenarioId);
            return runner;
        }

        void AdvanceCurrent(ScenarioRunner runner)
        {
            var node = runner.Scenario.nodes.Single(candidate => candidate.id == m_state.CurrentNodeId);
            m_now += 1f;
            Assert.That(
                runner.TryAdvanceFromPresentation(node.nextNodeId, runner.PresentationVersion),
                Is.True,
                $"无法从 {node.id} 前进到 {node.nextNodeId}。");
        }

        void Select(ScenarioRunner runner, StoryChoiceData choice)
        {
            m_now += 1f;
            Assert.That(runner.TrySelectChoice(choice, runner.PresentationVersion), Is.True);
        }

        static IEnumerable<string> ExitIds(StoryNodeData node)
        {
            if (!string.IsNullOrEmpty(node.nextNodeId)) yield return node.nextNodeId;
            if (!string.IsNullOrEmpty(node.successNodeId)) yield return node.successNodeId;
            if (!string.IsNullOrEmpty(node.failureNodeId)) yield return node.failureNodeId;
            if (!string.IsNullOrEmpty(node.winNodeId)) yield return node.winNodeId;
            if (!string.IsNullOrEmpty(node.loseNodeId)) yield return node.loseNodeId;
            if (!string.IsNullOrEmpty(node.fleeNodeId)) yield return node.fleeNodeId;
            if (node.choices == null) yield break;
            foreach (var choice in node.choices)
                if (choice != null) yield return choice.nextNodeId;
        }
    }
}
