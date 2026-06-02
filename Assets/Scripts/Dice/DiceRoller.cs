using System.Text;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Dice
{
    public static class DiceRoller
    {
        public static int RollD100()
        {
            return Random.Range(1, 101);
        }

        public static int RollWithBonusPenalty(int bonusDice, int penaltyDice)
        {
            var first = RollD100();
            if (bonusDice <= 0 && penaltyDice <= 0) return first;

            var rolls = new System.Collections.Generic.List<int> { first };
            for (var i = 0; i < bonusDice; i++) rolls.Add(RollD100());
            for (var i = 0; i < penaltyDice; i++) rolls.Add(RollD100());

            if (bonusDice > penaltyDice)
            {
                rolls.Sort();
                return rolls[0];
            }

            rolls.Sort();
            return rolls[rolls.Count - 1];
        }

        public static CheckResult SkillCheck(int skillValue, CheckDifficulty difficulty, string skillId = "",
            int bonusDice = 0, int penaltyDice = 0)
        {
            var target = skillValue;
            var rolled = RollWithBonusPenalty(bonusDice, penaltyDice);
            var second = bonusDice > 0 || penaltyDice > 0 ? RollD100() : 0;

            var hardTarget = target / 2;
            var extremeTarget = target / 5;

            CheckResultType resultType;
            if (rolled == 1)
                resultType = CheckResultType.CriticalSuccess;
            else if (rolled >= 96 && target < 50)
                resultType = CheckResultType.CriticalFailure;
            else if (rolled >= 96)
                resultType = CheckResultType.Failure;
            else if (difficulty == CheckDifficulty.Extreme && rolled <= extremeTarget)
                resultType = CheckResultType.ExtremeSuccess;
            else if (difficulty == CheckDifficulty.Hard && rolled <= hardTarget)
                resultType = CheckResultType.HardSuccess;
            else if (difficulty == CheckDifficulty.Regular && rolled <= target)
                resultType = CheckResultType.RegularSuccess;
            else if (rolled <= extremeTarget)
                resultType = CheckResultType.ExtremeSuccess;
            else if (rolled <= hardTarget)
                resultType = CheckResultType.HardSuccess;
            else if (rolled <= target)
                resultType = CheckResultType.RegularSuccess;
            else
                resultType = CheckResultType.Failure;

            var sb = new StringBuilder();
            sb.Append($"检定 [{skillId}] 目标{target} 掷出{rolled}");
            if (bonusDice > 0) sb.Append($" (奖励骰×{bonusDice})");
            if (penaltyDice > 0) sb.Append($" (惩罚骰×{penaltyDice})");
            sb.Append($" → {GetResultLabel(resultType)}");

            return new CheckResult
            {
                RolledValue = rolled,
                TargetValue = target,
                SecondRoll = second,
                UsedBonusDice = bonusDice > 0,
                UsedPenaltyDice = penaltyDice > 0,
                Difficulty = difficulty,
                ResultType = resultType,
                SkillId = skillId,
                Summary = sb.ToString()
            };
        }

        public static string GetResultLabel(CheckResultType type)
        {
            switch (type)
            {
                case CheckResultType.CriticalSuccess: return "大成功";
                case CheckResultType.ExtremeSuccess: return "极难成功";
                case CheckResultType.HardSuccess: return "困难成功";
                case CheckResultType.RegularSuccess: return "成功";
                case CheckResultType.CriticalFailure: return "大失败";
                default: return "失败";
            }
        }
    }
}
