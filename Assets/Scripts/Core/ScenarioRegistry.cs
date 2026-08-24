using System.Collections.Generic;

namespace WalkingIntoNight.TRPG.Core
{
    public class ScenarioRegistryEntry
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string ResourcePath;
        public bool Unlocked = true;
    }

    public static class ScenarioRegistry
    {
        public const string DefaultScenarioId = "Scenario_01";

        static readonly List<ScenarioRegistryEntry> Entries = new List<ScenarioRegistryEntry>
        {
            new ScenarioRegistryEntry
            {
                Id = DefaultScenarioId,
                DisplayName = "\u5496\u5561\u9986\u5173\u5e97\u540e\u7684\u5931\u8e2a",
                Description = "\u7b2c\u4e00\u4e2a\u5267\u672c\uff1a\u96e8\u591c\u5496\u5561\u9986\u8c03\u67e5\u3002",
                ResourcePath = "Data/Scenarios/Scenario_01/nodes"
            }
        };

        public static IReadOnlyList<ScenarioRegistryEntry> All => Entries;

        public static ScenarioRegistryEntry Get(string id)
        {
            foreach (var entry in Entries)
            {
                if (entry.Id == id) return entry;
            }

            return null;
        }
    }
}
