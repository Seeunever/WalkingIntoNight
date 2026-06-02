using System;
using System.Collections.Generic;

namespace WalkingIntoNight.TRPG.Character
{
    [Serializable]
    public class SkillEntry
    {
        public string id;
        public int value;
    }

    [Serializable]
    public class InvestigatorData
    {
        public string name;
        public int STR, CON, POW, DEX, APP, INT, EDU, SIZ;
        public int HP, MaxHP, SAN, MaxSAN, MP, MaxMP;
        public List<SkillEntry> skills = new List<SkillEntry>();

        public Dictionary<string, int> ToSkillDictionary()
        {
            var dict = new Dictionary<string, int>();
            if (skills == null) return dict;
            foreach (var s in skills)
            {
                if (!string.IsNullOrEmpty(s.id))
                    dict[s.id] = s.value;
            }
            return dict;
        }

        public static List<SkillEntry> FromDictionary(Dictionary<string, int> dict)
        {
            var list = new List<SkillEntry>();
            if (dict == null) return list;
            foreach (var kv in dict)
                list.Add(new SkillEntry { id = kv.Key, value = kv.Value });
            return list;
        }
    }
}
