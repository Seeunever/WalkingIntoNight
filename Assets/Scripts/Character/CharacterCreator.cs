using System.Collections.Generic;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Character
{
    public static class CharacterCreator
    {
        public static readonly string[] CoreSkillIds =
        {
            "spot_hidden", "listen", "library_use", "psychology", "persuade",
            "fast_talk", "stealth", "fight", "dodge", "firearms",
            "first_aid", "occult", "credit_rating", "locksmith", "track"
        };

        public static Investigator RollRandom(string name)
        {
            var inv = new Investigator { Name = string.IsNullOrWhiteSpace(name) ? "无名调查员" : name.Trim() };

            inv.STR = Roll3d6x5();
            inv.CON = Roll3d6x5();
            inv.POW = Roll3d6x5();
            inv.DEX = Roll3d6x5();
            inv.APP = Roll3d6x5();
            inv.INT = Roll3d6x5();
            inv.EDU = Roll3d6x5() + 6;
            inv.SIZ = Roll2d6x5() + 6;

            ApplyDerivedStats(inv);
            AssignDefaultSkills(inv);
            return inv;
        }

        public static Investigator CreateWithBuyPoints(string name, int[] attributes, int skillPoints)
        {
            var inv = new Investigator { Name = string.IsNullOrWhiteSpace(name) ? "无名调查员" : name.Trim() };
            if (attributes != null && attributes.Length >= 8)
            {
                inv.STR = attributes[0];
                inv.CON = attributes[1];
                inv.POW = attributes[2];
                inv.DEX = attributes[3];
                inv.APP = attributes[4];
                inv.INT = attributes[5];
                inv.EDU = attributes[6];
                inv.SIZ = attributes[7];
            }
            else
            {
                return RollRandom(name);
            }

            ApplyDerivedStats(inv);
            AssignDefaultSkills(inv);

            if (skillPoints > 0)
                DistributeSkillPoints(inv, skillPoints);

            return inv;
        }

        static void ApplyDerivedStats(Investigator inv)
        {
            inv.MaxHP = (inv.CON + inv.SIZ) / 10;
            if (inv.MaxHP < 1) inv.MaxHP = 1;
            inv.HP = inv.MaxHP;

            inv.MaxSAN = inv.POW;
            inv.SAN = inv.MaxSAN;

            inv.MaxMP = inv.POW / 5;
            if (inv.MaxMP < 1) inv.MaxMP = 1;
            inv.MP = inv.MaxMP;
        }

        static void AssignDefaultSkills(Investigator inv)
        {
            foreach (var id in CoreSkillIds)
                inv.Skills[id] = 0;

            inv.Skills["credit_rating"] = Mathf.Clamp(inv.EDU, 0, 99);
            inv.Skills["dodge"] = inv.DEX / 2;
            inv.Skills["library_use"] = inv.EDU;
            inv.Skills["occult"] = inv.EDU / 2;
            inv.Skills["spot_hidden"] = 25;
            inv.Skills["listen"] = 20;
            inv.Skills["psychology"] = 10;
            inv.Skills["persuade"] = 10;
            inv.Skills["fast_talk"] = 5;
            inv.Skills["stealth"] = 20;
            inv.Skills["fight"] = 25;
            inv.Skills["firearms"] = 20;
            inv.Skills["first_aid"] = 30;
            inv.Skills["locksmith"] = 1;
            inv.Skills["track"] = 10;
        }

        static void DistributeSkillPoints(Investigator inv, int points)
        {
            var preferred = new[] { "spot_hidden", "listen", "psychology", "library_use", "persuade" };
            var remaining = points;
            foreach (var id in preferred)
            {
                if (remaining <= 0) break;
                var add = Mathf.Min(remaining, 15);
                inv.Skills[id] = Mathf.Clamp(inv.Skills[id] + add, 0, 99);
                remaining -= add;
            }
        }

        static int Roll3d6x5()
        {
            var sum = 0;
            for (var i = 0; i < 3; i++) sum += Random.Range(1, 7);
            return sum * 5;
        }

        static int Roll2d6x5()
        {
            var sum = 0;
            for (var i = 0; i < 2; i++) sum += Random.Range(1, 7);
            return sum * 5;
        }
    }
}
