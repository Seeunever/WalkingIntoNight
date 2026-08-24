using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Narrative;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class ScenarioInteractionModeTests
    {
        GameObject m_stateObject;
        GameStateManager m_state;
        float m_now;

        [SetUp]
        public void SetUp()
        {
            m_stateObject = new GameObject("GameStateManager_InteractionModeTest");
            m_now = 1f;
            m_state = m_stateObject.AddComponent<GameStateManager>();
            m_state.ResetForNewGame();
            m_state.SetInvestigator(new Investigator
            {
                Name = "门控测试调查员",
                HP = 10,
                MaxHP = 10,
                SAN = 50,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_stateObject);
        }

        [Test]
        public void NarrativeMode_BlocksExplorationCommandsWithoutChangingState()
        {
            var runner = CreateRunner(
                Node("intro", "dialogue", nextNodeId: "hub"),
                Node("hub", "location", allowExploration: true));
            runner.StartFrom("intro");
            var originalTime = m_state.CurrentTime;
            var originalLocation = m_state.CurrentLocationId;

            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Narrative));
            Assert.That(runner.WaitNextPeriod(), Is.False);
            Assert.That(runner.WaitNextDay(), Is.False);
            Assert.That(runner.TravelToLocation("cafe_storage"), Is.False);
            Assert.That(runner.TalkToNpc("npc_cat"), Is.False);
            Assert.That(m_state.CurrentTime, Is.EqualTo(originalTime));
            Assert.That(m_state.CurrentLocationId, Is.EqualTo(originalLocation));
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("intro"));
        }

        [Test]
        public void ExplorationNode_EnablesWaitAndPublishesModeChange()
        {
            var runner = CreateRunner(
                Node("intro", "dialogue", nextNodeId: "hub"),
                Node("hub", "location", allowExploration: true));
            var modes = new List<ScenarioInteractionMode>();
            runner.OnInteractionModeChanged = modes.Add;
            runner.StartFrom("intro");

            Assert.That(
                runner.TryAdvanceFromPresentation("hub", runner.PresentationVersion),
                Is.True);
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Exploration));
            Assert.That(modes, Is.EqualTo(new[] { ScenarioInteractionMode.Exploration }));

            var originalTime = m_state.CurrentTime;
            Assert.That(runner.WaitNextPeriod(), Is.True);
            Assert.That(m_state.CurrentTime, Is.Not.EqualTo(originalTime));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Exploration));
        }

        [Test]
        public void EndNode_DisablesExplorationCommands()
        {
            var runner = CreateRunner(
                Node("hub", "location", allowExploration: true),
                Node("ending", "end"));
            runner.StartFrom("hub");
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Exploration));

            runner.AdvanceTo("ending");

            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.End));
            Assert.That(runner.WaitNextPeriod(), Is.False);
        }

        [Test]
        public void RapidContinue_CannotAdvanceASecondNode()
        {
            var runner = CreateRunner(
                Node("first", "dialogue", nextNodeId: "second"),
                Node("second", "dialogue", nextNodeId: "third"),
                Node("third", "dialogue"));
            runner.StartFrom("first");
            var oldVersion = runner.PresentationVersion;

            Assert.That(runner.TryAdvanceFromPresentation("second", oldVersion), Is.True);
            Assert.That(runner.TryAdvanceFromPresentation("third", oldVersion), Is.False);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("second"));

            var newVersion = runner.PresentationVersion;
            Assert.That(runner.TryAdvanceFromPresentation("third", newVersion), Is.False);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("second"));

            m_now += 0.75f;
            Assert.That(runner.TryAdvanceFromPresentation("third", newVersion), Is.False);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("second"));

            m_now += 0.01f;
            Assert.That(runner.TryAdvanceFromPresentation("third", newVersion), Is.True);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("third"));
        }

        [Test]
        public void OldChoiceButton_CannotSelectAgainAfterNodeChanges()
        {
            var firstChoice = new StoryChoiceData { text = "第一项", nextNodeId = "first_result" };
            var staleChoice = new StoryChoiceData { text = "旧按钮", nextNodeId = "wrong_result" };
            var start = Node("start", "dialogue");
            start.choices.Add(firstChoice);
            start.choices.Add(staleChoice);
            var runner = CreateRunner(
                start,
                Node("first_result", "dialogue"),
                Node("wrong_result", "dialogue"));
            runner.StartFrom("start");
            var oldVersion = runner.PresentationVersion;

            Assert.That(runner.TrySelectChoice(firstChoice, oldVersion), Is.True);
            Assert.That(runner.TrySelectChoice(staleChoice, oldVersion), Is.False);
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("first_result"));
        }

        ScenarioRunner CreateRunner(params StoryNodeData[] nodes)
        {
            var runner = new ScenarioRunner(m_state, () => m_now);
            runner.LoadScenario(new ScenarioFile
            {
                scenarioId = "interaction-test",
                startNodeId = nodes[0].id,
                nodes = new List<StoryNodeData>(nodes)
            });
            return runner;
        }

        static StoryNodeData Node(
            string id,
            string type,
            string nextNodeId = null,
            bool allowExploration = false)
        {
            return new StoryNodeData
            {
                id = id,
                type = type,
                text = id,
                nextNodeId = nextNodeId,
                allowExploration = allowExploration
            };
        }
    }
}
