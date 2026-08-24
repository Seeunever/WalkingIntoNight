using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Narrative;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class ScenarioResumeTests
    {
        GameObject m_stateObject;
        GameStateManager m_state;

        [SetUp]
        public void SetUp()
        {
            m_stateObject = new GameObject("GameStateManager_Test");
            m_state = m_stateObject.AddComponent<GameStateManager>();
            m_state.ResetForNewGame();
            m_state.SetInvestigator(new Investigator
            {
                Name = "测试调查员",
                HP = 10,
                MaxHP = 10,
                SAN = 40,
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
        public void ResumeFromSave_GiveItemNode_DoesNotGrantItemAgain()
        {
            m_state.Inventory.AddItem("rusty_key");
            var runner = CreateRunner(Node(
                "saved",
                "giveitem",
                "你已经取得钥匙。",
                nextNodeId: "after",
                itemId: "rusty_key"),
                Node("after", "dialogue", "之后。"));

            Assert.That(runner.ResumeFromSave("saved"), Is.True);

            Assert.That(m_state.Inventory.Items, Has.Count.EqualTo(1));
            Assert.That(m_state.Inventory.Items[0], Is.EqualTo("rusty_key"));
        }

        [Test]
        public void ResumeFromSave_ChangeSanNode_DoesNotApplyDeltaAgain()
        {
            var runner = CreateRunner(Node(
                "saved",
                "changesan",
                "理智已经下降。",
                nextNodeId: "after",
                sanDelta: -5),
                Node("after", "dialogue", "之后。"));

            Assert.That(runner.ResumeFromSave("saved"), Is.True);

            Assert.That(m_state.Investigator.SAN, Is.EqualTo(40));
        }

        [Test]
        public void ResumeFromSave_AdvanceTimeNode_DoesNotAdvanceAgain()
        {
            m_state.SetTime(3, TimePeriod.Evening);
            var runner = CreateRunner(Node(
                "saved",
                "advancetime",
                "时间已经推进。",
                nextNodeId: "after",
                advancePeriods: 2),
                Node("after", "dialogue", "之后。"));

            Assert.That(runner.ResumeFromSave("saved"), Is.True);

            Assert.That(m_state.CurrentTime.day, Is.EqualTo(3));
            Assert.That(m_state.CurrentTime.period, Is.EqualTo(TimePeriod.Evening));
        }

        [Test]
        public void ResumeFromSave_TextlessAutomaticNodes_SkipToPresentationWithoutEffects()
        {
            m_state.Inventory.AddItem("rusty_key");
            m_state.SetTime(2, TimePeriod.Night);
            StoryNodeData presented = null;
            var runner = CreateRunner(
                Node("give", "giveitem", nextNodeId: "time", itemId: "rusty_key"),
                Node("time", "advancetime", nextNodeId: "display", advancePeriods: 1),
                Node("display", "dialogue", "可展示节点。"));
            runner.OnNodePresented = node => presented = node;

            Assert.That(runner.ResumeFromSave("give"), Is.True);

            Assert.That(presented?.id, Is.EqualTo("display"));
            Assert.That(m_state.CurrentNodeId, Is.EqualTo("display"));
            Assert.That(m_state.Inventory.Items, Has.Count.EqualTo(1));
            Assert.That(m_state.CurrentTime.day, Is.EqualTo(2));
            Assert.That(m_state.CurrentTime.period, Is.EqualTo(TimePeriod.Night));
        }

        [Test]
        public void ResumeFromSave_TextlessCycle_StopsSafely()
        {
            var runner = CreateRunner(
                Node("a", "setflag", nextNodeId: "b"),
                Node("b", "advancetime", nextNodeId: "a", advancePeriods: 1));

            Assert.That(runner.ResumeFromSave("a"), Is.False);
            Assert.That(m_state.Flags, Is.Empty);
            Assert.That(m_state.CurrentTime, Is.EqualTo(GameTime.Default));
        }

        ScenarioRunner CreateRunner(params StoryNodeData[] nodes)
        {
            var runner = new ScenarioRunner(m_state);
            runner.LoadScenario(new ScenarioFile
            {
                scenarioId = "test",
                startNodeId = nodes[0].id,
                nodes = new List<StoryNodeData>(nodes)
            });
            return runner;
        }

        static StoryNodeData Node(
            string id,
            string type,
            string text = null,
            string nextNodeId = null,
            string itemId = null,
            int sanDelta = 0,
            int advancePeriods = 0)
        {
            return new StoryNodeData
            {
                id = id,
                type = type,
                text = text,
                nextNodeId = nextNodeId,
                itemId = itemId,
                itemCount = 1,
                sanDelta = sanDelta,
                advancePeriods = advancePeriods
            };
        }

    }
}
