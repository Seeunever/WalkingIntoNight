using System.Collections.Generic;

namespace AnimalCafe.TRPG.Combat
{
    public class CombatEncounterDefinition
    {
        public string id;
        public string displayName;
        public List<CombatantData> enemies = new List<CombatantData>();
    }

    public static class CombatEncounterDatabase
    {
        static readonly Dictionary<string, CombatEncounterDefinition> Encounters =
            new Dictionary<string, CombatEncounterDefinition>
            {
                {
                    "shadow_rat", new CombatEncounterDefinition
                    {
                        id = "shadow_rat",
                        displayName = "影鼠群",
                        enemies = new List<CombatantData>
                        {
                            new CombatantData
                            {
                                id = "rat1", displayName = "影鼠", isPlayer = false,
                                HP = 8, MaxHP = 8, skillFight = 40, skillDodge = 30
                            }
                        }
                    }
                },
                {
                    "cultist_acolyte", new CombatEncounterDefinition
                    {
                        id = "cultist_acolyte",
                        displayName = "狂热侍从",
                        enemies = new List<CombatantData>
                        {
                            new CombatantData
                            {
                                id = "cult1", displayName = "侍从", isPlayer = false,
                                HP = 12, MaxHP = 12, skillFight = 55, skillDodge = 25
                            },
                            new CombatantData
                            {
                                id = "cult2", displayName = "侍从", isPlayer = false,
                                HP = 10, MaxHP = 10, skillFight = 50, skillDodge = 20
                            }
                        }
                    }
                }
            };

        public static CombatEncounterDefinition Get(string id)
        {
            return id != null && Encounters.TryGetValue(id, out var def) ? def : null;
        }
    }
}
