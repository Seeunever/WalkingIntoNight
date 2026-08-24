using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WalkingIntoNight.TRPG.Editor;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class StoryValidatorTests
    {
        [Test]
        public void Validate_WhenNodeIdsAreDuplicated_ReportsError()
        {
            var project = CreateProject(
                "start",
                Node("start"),
                Node("start"));

            var issues = StoryValidator.Validate(project);

            AssertErrorContaining(issues, "重复节点 ID: start");
        }

        [Test]
        public void Validate_WhenJumpTargetIsMissing_ReportsError()
        {
            var project = CreateProject(
                "start",
                Node("start", nextNodeId: "missing"));

            var issues = StoryValidator.Validate(project);

            AssertErrorContaining(issues, "nextNodeId 指向不存在: missing");
        }

        [Test]
        public void Validate_WhenStartNodeDoesNotExist_ReportsError()
        {
            var project = CreateProject(
                "missing",
                Node("existing"));

            var issues = StoryValidator.Validate(project);

            AssertErrorContaining(issues, "startNodeId 不存在: missing");
        }

        [Test]
        public void FormatIssues_KeepsPrefixAndMessageOnOneLine()
        {
            var formatted = StoryValidator.FormatIssues(new List<StoryValidator.ValidationIssue>
            {
                new StoryValidator.ValidationIssue { isError = true, message = "第一条" },
                new StoryValidator.ValidationIssue { isError = false, message = "第二条" }
            });

            Assert.That(formatted.Replace("\r\n", "\n"), Is.EqualTo(
                "[错误] 第一条\n[警告] 第二条\n"));
        }

        [Test]
        public void Validate_UnsupportedTypeAndMissingOrdinaryExit_ReportErrorsButEndNeedsNoExit()
        {
            var project = CreateProject(
                "bad_type",
                Node("bad_type", type: "mystery"),
                Node("blocked", type: "dialogue"),
                Node("ending", type: "end"));

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "不支持的 type: mystery");
            AssertErrorContaining(issues, "节点 blocked 缺少 nextNodeId 或选项出口");
            Assert.That(issues.Any(issue => issue.message.Contains("节点 ending 缺少")), Is.False);
        }

        [Test]
        public void Validate_LocationAndAutomaticNodesRequireExitUnlessExplorationIsAllowed()
        {
            var location = Node("location", type: "location");
            var advanceTime = Node("advance", type: "advanceTime");
            var hub = Node("hub", type: "location");
            hub.allowExploration = true;
            var project = CreateProject(
                "location",
                location,
                advanceTime,
                hub,
                Node("ending", type: "end"));

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "节点 location 缺少 nextNodeId 或选项出口");
            AssertErrorContaining(issues, "advanceTime 节点 advance 缺少 nextNodeId");
            Assert.That(issues.Any(issue => issue.message.Contains("节点 hub 缺少")), Is.False);
        }

        [Test]
        public void Validate_CheckRequiresSkillAndBothOutcomeTargets()
        {
            var project = CreateProject("check", Node("check", type: "check"));

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "check 节点 check 缺少 skillId");
            AssertErrorContaining(issues, "节点 check 缺少 successNodeId");
            AssertErrorContaining(issues, "节点 check 缺少 failureNodeId");
        }

        [Test]
        public void Validate_CombatRequiresKnownEncounterAndAllOutcomeTargets()
        {
            var combat = Node("fight", type: "combat");
            combat.combatId = "missing_encounter";
            var project = CreateProject("fight", combat);

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "引用未知战斗: missing_encounter");
            AssertErrorContaining(issues, "节点 fight 缺少 winNodeId");
            AssertErrorContaining(issues, "节点 fight 缺少 loseNodeId");
            AssertErrorContaining(issues, "节点 fight 缺少 fleeNodeId");
        }

        [Test]
        public void Validate_GiveItemAndChoiceRequireKnownItems()
        {
            var give = Node("give", "ending", "giveitem");
            give.itemId = "missing_item";
            var choiceNode = Node("choice", type: "dialogue");
            choiceNode.choices.Add(new StoryChoiceData
            {
                text = "需要物品",
                nextNodeId = "ending",
                requiredItemId = "also_missing"
            });
            var project = CreateProject(
                "give",
                give,
                choiceNode,
                Node("ending", type: "end"));

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "节点 give 引用未知物品: missing_item");
            AssertErrorContaining(issues, "选项引用未知物品: also_missing");
        }

        [Test]
        public void Validate_NodeLocationMustExist()
        {
            var start = Node("start", "ending");
            start.locationId = "missing_location";
            var project = CreateProject("start", start, Node("ending", type: "end"));

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "节点 start 引用未知地点: missing_location");
        }

        [Test]
        public void Validate_NpcDefaultLocationsAndScheduleMustExist()
        {
            var project = ValidProject();
            project.npcs.Add(new NPCDefinition
            {
                id = "bad_npc",
                defaultNodeId = "missing_node",
                locationIds = new List<string> { "missing_location" },
                schedules = new List<NpcScheduleEntry>
                {
                    new NpcScheduleEntry { locationId = "missing_schedule_location" }
                }
            });

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "defaultNodeId 不存在: missing_node");
            AssertErrorContaining(issues, "NPC bad_npc 引用未知地点: missing_location");
            AssertErrorContaining(issues, "日程引用未知地点: missing_schedule_location");
        }

        [Test]
        public void Validate_LocationNpcAndRelationshipEndpointsMustExist()
        {
            var project = ValidProject();
            project.locations[0].npcIds.Add("missing_npc");
            project.relationships.Add(new NpcRelationship
            {
                id = "bad_relationship",
                fromNpcId = "npc",
                toNpcId = "missing_npc"
            });

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "地点 location 引用未知 NPC: missing_npc");
            AssertErrorContaining(issues, "toNpcId 不存在: missing_npc");
        }

        [Test]
        public void Validate_LocationAccessConditionsRequireKnownItemAndPeriod()
        {
            var project = ValidProject();
            project.locations[0].requiredItemId = "missing_key";
            project.locations[0].requiredPeriod = "lunchtime";

            var issues = StoryValidator.Validate(project, _ => true);

            AssertErrorContaining(issues, "进入条件引用未知物品: missing_key");
            AssertErrorContaining(issues, "使用未知时段: lunchtime");
        }

        [Test]
        public void Validate_MissingPortraitIsWarningOnly()
        {
            var project = ValidProject();
            project.npcs[0].portraitId = "missing_portrait";

            var issues = StoryValidator.Validate(project, _ => false);

            Assert.That(issues.Any(issue =>
                !issue.isError && issue.message.Contains("头像资源不存在: missing_portrait")), Is.True);
            Assert.That(issues.Any(issue => issue.isError), Is.False);
        }

        [Test]
        public void Validate_CurrentScenario01_HasNoErrors()
        {
            var project = StoryJsonIO.Load("Scenario_01");

            var issues = StoryValidator.Validate(project);

            Assert.That(
                issues,
                Is.Empty,
                string.Join("\n", issues.Select(issue => issue.message)));
        }

        [Test]
        public void Scenario01_CheckFailureRoutes_AreExplicitAndPointToKnownNodes()
        {
            var scenario = StoryJsonIO.Load("Scenario_01").scenario;
            var expected = new Dictionary<string, string>
            {
                { "check_spot_main", "spot_fail" },
                { "storage_check", "hub_explore" },
                { "check_occult_wall", "san_loss_fail" },
                { "check_psych_mei", "hub_explore" },
                { "chen_library", "hub_explore" },
                { "bar_diary_search", "hub_explore" },
                { "check_persuade_woman", "combat_cultist" },
                { "midnight_san_check", "midnight_failure_choice" }
            };
            var nodes = scenario.nodes.ToDictionary(node => node.id);
            var actual = scenario.nodes
                .Where(node => string.Equals(node.type, "check"))
                .ToDictionary(node => node.id, node => node.failureNodeId);

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Values.All(nodes.ContainsKey), Is.True);
        }

        static StoryProjectData CreateProject(string startNodeId, params StoryNodeData[] nodes)
        {
            return new StoryProjectData
            {
                scenario = new ScenarioFile
                {
                    startNodeId = startNodeId,
                    nodes = new List<StoryNodeData>(nodes)
                }
            };
        }

        static StoryProjectData ValidProject()
        {
            var project = CreateProject(
                "start",
                Node("start", "ending"),
                Node("ending", type: "end"));
            project.items.Add(new ItemDefinition { id = "item" });
            project.locations.Add(new LocationDefinition
            {
                id = "location",
                npcIds = new List<string> { "npc" }
            });
            project.npcs.Add(new NPCDefinition
            {
                id = "npc",
                defaultNodeId = "ending",
                locationIds = new List<string> { "location" }
            });
            return project;
        }

        static StoryNodeData Node(
            string id,
            string nextNodeId = null,
            string type = "dialogue")
        {
            return new StoryNodeData
            {
                id = id,
                type = type,
                nextNodeId = nextNodeId
            };
        }

        static void AssertErrorContaining(
            IEnumerable<StoryValidator.ValidationIssue> issues,
            string expectedMessage)
        {
            Assert.That(
                issues.Any(issue => issue.isError && issue.message.Contains(expectedMessage)),
                Is.True,
                $"Expected an error containing '{expectedMessage}'.");
        }
    }
}
