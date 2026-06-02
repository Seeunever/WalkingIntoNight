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
                DisplayName = "?????????",
                Description = "????????????????????????",
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
