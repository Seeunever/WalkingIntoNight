using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class LocationAccessTests
    {
        GameObject m_stateObject;
        GameStateManager m_state;

        [SetUp]
        public void SetUp()
        {
            m_stateObject = new GameObject("GameStateManager_LocationAccessTest");
            m_state = m_stateObject.AddComponent<GameStateManager>();
            m_state.ResetForNewGame();
            m_state.SetInvestigator(new Investigator
            {
                Name = "地点测试调查员",
                HP = 10,
                MaxHP = 10,
                SAN = 50,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            });
            NPCDatabase.Reload();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_stateObject);
        }

        [Test]
        public void BasementTravel_IsBlockedWithoutKeyAndAllowedAfterKey()
        {
            var runner = HubRunner();
            var logs = new List<string>();
            runner.OnLog = logs.Add;
            runner.StartFrom("hub_explore");
            var originalLocation = m_state.CurrentLocationId;

            var blocked = runner.GetLocationAccess("cafe_basement");
            Assert.That(blocked.CanEnter, Is.False);
            Assert.That(blocked.BlockReason, Is.EqualTo(LocationAccessBlockReason.MissingItem));
            Assert.That(blocked.Message, Does.Contain("生锈的钥匙"));
            Assert.That(runner.TravelToLocation("cafe_basement"), Is.False);
            Assert.That(m_state.CurrentLocationId, Is.EqualTo(originalLocation));
            Assert.That(logs, Has.Some.Contains("无法前往地点"));

            m_state.Inventory.AddItem("rusty_key");

            Assert.That(runner.GetLocationAccess("cafe_basement").CanEnter, Is.True);
            Assert.That(runner.TravelToLocation("cafe_basement"), Is.True);
            Assert.That(m_state.CurrentLocationId, Is.EqualTo("cafe_basement"));
        }

        [Test]
        public void TimeGatedLocation_ReportsWrongTimeSeparatelyFromMissingItem()
        {
            var location = new LocationDefinition
            {
                id = "night_room",
                requiredPeriod = "night"
            };

            var blocked = LocationAccessEvaluator.Evaluate(location, m_state);

            Assert.That(blocked.CanEnter, Is.False);
            Assert.That(blocked.BlockReason, Is.EqualTo(LocationAccessBlockReason.WrongTime));
            Assert.That(blocked.Message, Is.EqualTo("仅在夜间可进入"));

            m_state.SetTime(1, TimePeriod.Night);
            Assert.That(LocationAccessEvaluator.Evaluate(location, m_state).CanEnter, Is.True);
        }

        [Test]
        public void Waiting_EveningToNightThenNextDayMorning_RefreshesScheduledNpcs()
        {
            var runner = HubRunner();
            var timeUpdates = 0;
            runner.OnTimeChanged = () => timeUpdates++;
            runner.StartFrom("hub_explore");
            m_state.SetTime(1, TimePeriod.Evening);

            Assert.That(AvailableNpcIds(runner), Does.Not.Contain("silent_woman"));
            Assert.That(runner.WaitNextPeriod(), Is.True);
            Assert.That(m_state.CurrentTime.day, Is.EqualTo(1));
            Assert.That(m_state.CurrentTime.period, Is.EqualTo(TimePeriod.Night));
            Assert.That(AvailableNpcIds(runner), Does.Contain("silent_woman"));

            Assert.That(runner.WaitNextDay(), Is.True);
            Assert.That(m_state.CurrentTime.day, Is.EqualTo(2));
            Assert.That(m_state.CurrentTime.period, Is.EqualTo(TimePeriod.Morning));
            Assert.That(AvailableNpcIds(runner), Does.Not.Contain("silent_woman"));
            Assert.That(timeUpdates, Is.EqualTo(2));
        }

        [Test]
        public void Waiting_DoesNotRepeatCurrentNodeSideEffect()
        {
            var effectNode = new StoryNodeData
            {
                id = "effect",
                type = "giveitem",
                text = "获得物品",
                itemId = "silver_coin",
                nextNodeId = "hub_explore",
                allowExploration = true
            };
            var runner = Runner(effectNode, HubNode());
            runner.StartFrom("effect");

            Assert.That(m_state.Inventory.Items.Count(item => item == "silver_coin"), Is.EqualTo(1));
            Assert.That(runner.WaitNextPeriod(), Is.True);
            Assert.That(m_state.Inventory.Items.Count(item => item == "silver_coin"), Is.EqualTo(1));
        }

        string[] AvailableNpcIds(ScenarioRunner runner)
        {
            return runner.GetAvailableNpcsAtLocation("cafe_basement")
                .Select(npc => npc.id)
                .ToArray();
        }

        ScenarioRunner HubRunner() => Runner(HubNode());

        ScenarioRunner Runner(params StoryNodeData[] nodes)
        {
            var runner = new ScenarioRunner(m_state);
            runner.LoadScenario(new ScenarioFile
            {
                scenarioId = "location-access-test",
                startNodeId = nodes[0].id,
                nodes = new List<StoryNodeData>(nodes)
            });
            return runner;
        }

        static StoryNodeData HubNode() => new StoryNodeData
        {
            id = "hub_explore",
            type = "location",
            text = "自由调查",
            allowExploration = true
        };
    }
}
