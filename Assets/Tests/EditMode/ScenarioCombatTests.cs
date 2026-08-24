using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Dice;
using WalkingIntoNight.TRPG.Narrative;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class ScenarioCombatTests
    {
        GameObject m_stateObject;
        GameStateManager m_state;

        [SetUp]
        public void SetUp()
        {
            m_stateObject = new GameObject("GameStateManager_CombatTest");
            m_state = m_stateObject.AddComponent<GameStateManager>();
            m_state.ResetForNewGame();
            m_state.SetInvestigator(Investigator());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_stateObject);
        }

        [Test]
        public void InvalidEncounter_FallsBackWithoutOpeningCombatUi()
        {
            var manager = Manager();
            var runner = Runner(manager,
                CombatNode("fight", "missing", "win", "lose", "hub"),
                Node("win"),
                Node("lose", "end"),
                Node("hub", "location", allowExploration: true));
            var requestedCombatUi = false;
            var logs = new List<string>();
            runner.OnRequestCombatUI = () => requestedCombatUi = true;
            runner.OnLog = logs.Add;

            runner.StartFrom("fight");

            Assert.That(requestedCombatUi, Is.False);
            Assert.That(logs, Has.Some.Contains("未知战斗配置"));
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("hub"));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Exploration));
            AssertCombatStateCleared(manager);
        }

        [Test]
        public void Victory_SynchronizesAndClearsBeforeAdvancingToWinNode()
        {
            var manager = Manager(new Queue<bool>(new[] { true }), (minimum, maximum) => maximum - 1);
            var runner = StandardRunner(manager);

            runner.StartFrom("fight");
            Assert.That(m_state.HasPendingCombatReturn, Is.True);
            Assert.That(m_state.ActiveCombat, Is.SameAs(manager.State));

            Assert.That(manager.PlayerAttack(0, manager.State.turnNumber), Is.True);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("win"));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Narrative));
            Assert.That(m_state.Investigator.HP, Is.EqualTo(12));
            AssertCombatStateCleared(manager);
        }

        [Test]
        public void Defeat_SynchronizesZeroHpAndAdvancesToLoseNode()
        {
            m_state.SetInvestigator(Investigator(hp: 5));
            var manager = Manager(
                new Queue<bool>(new[] { false, true }),
                (minimum, maximum) => maximum - 1);
            var runner = StandardRunner(manager);

            runner.StartFrom("fight");
            Assert.That(manager.PlayerAttack(0, manager.State.turnNumber), Is.True);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("lose"));
            Assert.That(m_state.Investigator.HP, Is.EqualTo(0));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.End));
            AssertCombatStateCleared(manager);
        }

        [Test]
        public void Flee_AdvancesToFleeNodeAndClearsPendingState()
        {
            var manager = Manager(new Queue<bool>(new[] { true }));
            var runner = StandardRunner(manager);

            runner.StartFrom("fight");
            Assert.That(manager.PlayerFlee(manager.State.turnNumber), Is.True);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("flee"));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Exploration));
            AssertCombatStateCleared(manager);
        }

        [Test]
        public void MissingOutcomeTarget_LogsAndUsesSafeFleeFallback()
        {
            var manager = Manager(new Queue<bool>(new[] { true }), (minimum, maximum) => maximum - 1);
            var runner = Runner(manager,
                CombatNode("fight", "shadow_rat", "missing_win", "lose", "flee"),
                Node("lose", "end"),
                Node("flee", "location", allowExploration: true));
            var logs = new List<string>();
            runner.OnLog = logs.Add;

            runner.StartFrom("fight");
            Assert.That(manager.PlayerAttack(0, manager.State.turnNumber), Is.True);

            Assert.That(logs, Has.Some.Contains("战斗胜利出口不存在"));
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("flee"));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Exploration));
            AssertCombatStateCleared(manager);
        }

        [Test]
        public void Scenario01_CultistVictory_AdvancesToConfiguredWinNode()
        {
            var manager = Manager(
                new Queue<bool>(new[] { true, false, false, true, false, true }),
                (minimum, maximum) => maximum - 1);
            var runner = new ScenarioRunner(m_state, combatManager: manager);
            runner.LoadScenario(ScenarioRegistry.DefaultScenarioId);

            runner.AdvanceTo("combat_cultist");
            Assert.That(manager.PlayerAttack(0, manager.State.turnNumber), Is.True);
            Assert.That(manager.PlayerAttack(0, manager.State.turnNumber), Is.True);
            Assert.That(manager.PlayerAttack(0, manager.State.turnNumber), Is.True);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("after_cult_win"));
            Assert.That(m_state.Investigator.HP, Is.EqualTo(12));
            AssertCombatStateCleared(manager);
        }

        [Test]
        public void Scenario01_CultistDefeat_AdvancesToConfiguredBadEnding()
        {
            m_state.SetInvestigator(Investigator(hp: 5));
            var manager = Manager(
                new Queue<bool>(new[] { false, true }),
                (minimum, maximum) => maximum - 1);
            var runner = new ScenarioRunner(m_state, combatManager: manager);
            runner.LoadScenario(ScenarioRegistry.DefaultScenarioId);

            runner.AdvanceTo("combat_cultist");
            Assert.That(manager.PlayerAttack(0, manager.State.turnNumber), Is.True);

            Assert.That(m_state.CurrentNodeId, Is.EqualTo("end_bad"));
            Assert.That(m_state.Investigator.HP, Is.EqualTo(0));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.End));
            AssertCombatStateCleared(manager);
        }

        ScenarioRunner StandardRunner(CombatManager manager)
        {
            return Runner(manager,
                CombatNode("fight", "shadow_rat", "win", "lose", "flee"),
                Node("win"),
                Node("lose", "end"),
                Node("flee", "location", allowExploration: true));
        }

        ScenarioRunner Runner(CombatManager manager, params StoryNodeData[] nodes)
        {
            var runner = new ScenarioRunner(m_state, combatManager: manager);
            runner.LoadScenario(new ScenarioFile
            {
                scenarioId = "combat-test",
                startNodeId = nodes[0].id,
                nodes = new List<StoryNodeData>(nodes)
            });
            return runner;
        }

        void AssertCombatStateCleared(CombatManager manager)
        {
            Assert.That(manager.State, Is.Null);
            Assert.That(m_state.ActiveCombat, Is.Null);
            Assert.That(m_state.HasPendingCombatReturn, Is.False);
            Assert.That(m_state.PostCombatNodeId, Is.Null);
        }

        static CombatManager Manager(
            Queue<bool> checks = null,
            System.Func<int, int, int> randomRange = null)
        {
            checks ??= new Queue<bool>();
            return new CombatManager(
                (skill, difficulty, skillId, bonusDice, penaltyDice) => new CheckResult
                {
                    ResultType = checks.Count > 0 && checks.Dequeue()
                        ? CheckResultType.RegularSuccess
                        : CheckResultType.Failure
                },
                randomRange ?? ((minimum, maximum) => minimum));
        }

        static StoryNodeData CombatNode(
            string id,
            string combatId,
            string winNodeId,
            string loseNodeId,
            string fleeNodeId)
        {
            return new StoryNodeData
            {
                id = id,
                type = "combat",
                text = id,
                combatId = combatId,
                winNodeId = winNodeId,
                loseNodeId = loseNodeId,
                fleeNodeId = fleeNodeId
            };
        }

        static StoryNodeData Node(
            string id,
            string type = "dialogue",
            bool allowExploration = false)
        {
            return new StoryNodeData
            {
                id = id,
                type = type,
                text = id,
                allowExploration = allowExploration
            };
        }

        static Investigator Investigator(int hp = 12)
        {
            var investigator = new Investigator
            {
                Name = "场景战斗测试调查员",
                STR = 10,
                SIZ = 10,
                HP = hp,
                MaxHP = 12,
                SAN = 50,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            };
            investigator.Skills["fight"] = 60;
            investigator.Skills["dodge"] = 60;
            return investigator;
        }
    }
}
