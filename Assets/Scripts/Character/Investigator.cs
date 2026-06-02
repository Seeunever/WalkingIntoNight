using System.Collections.Generic;

namespace WalkingIntoNight.TRPG.Character
{
    public class Investigator
    {
        public string Name { get; set; }
        public int STR { get; set; }
        public int CON { get; set; }
        public int POW { get; set; }
        public int DEX { get; set; }
        public int APP { get; set; }
        public int INT { get; set; }
        public int EDU { get; set; }
        public int SIZ { get; set; }

        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int SAN { get; set; }
        public int MaxSAN { get; set; }
        public int MP { get; set; }
        public int MaxMP { get; set; }

        public Dictionary<string, int> Skills { get; } = new Dictionary<string, int>();

        public int DamageBonus => (STR + SIZ) / 2 / 2 - 2; // simplified

        public int GetSkill(string skillId)
        {
            if (Skills.TryGetValue(skillId, out var value)) return value;
            return 0;
        }

        public InvestigatorData ToData()
        {
            var data = new InvestigatorData
            {
                name = Name,
                STR = STR, CON = CON, POW = POW, DEX = DEX,
                APP = APP, INT = INT, EDU = EDU, SIZ = SIZ,
                HP = HP, MaxHP = MaxHP, SAN = SAN, MaxSAN = MaxSAN,
                MP = MP, MaxMP = MaxMP,
                skills = InvestigatorData.FromDictionary(Skills)
            };
            return data;
        }

        public static Investigator FromData(InvestigatorData data)
        {
            var inv = new Investigator
            {
                Name = data.name,
                STR = data.STR, CON = data.CON, POW = data.POW, DEX = data.DEX,
                APP = data.APP, INT = data.INT, EDU = data.EDU, SIZ = data.SIZ,
                HP = data.HP, MaxHP = data.MaxHP,
                SAN = data.SAN, MaxSAN = data.MaxSAN,
                MP = data.MP, MaxMP = data.MaxMP
            };

            foreach (var kv in data.ToSkillDictionary())
                inv.Skills[kv.Key] = kv.Value;

            return inv;
        }
    }
}
