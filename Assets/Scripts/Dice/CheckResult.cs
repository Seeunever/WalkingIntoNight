namespace WalkingIntoNight.TRPG.Dice
{
    public class CheckResult
    {
        public int RolledValue;
        public int TargetValue;
        public int SecondRoll;
        public bool UsedPenaltyDice;
        public bool UsedBonusDice;
        public CheckDifficulty Difficulty;
        public CheckResultType ResultType;
        public string SkillId;
        public string Summary;

        public bool IsSuccess =>
            ResultType == CheckResultType.CriticalSuccess ||
            ResultType == CheckResultType.ExtremeSuccess ||
            ResultType == CheckResultType.HardSuccess ||
            ResultType == CheckResultType.RegularSuccess;
    }
}
