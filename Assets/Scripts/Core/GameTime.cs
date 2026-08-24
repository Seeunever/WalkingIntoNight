using System;

namespace WalkingIntoNight.TRPG.Core
{
    public enum TimePeriod
    {
        Morning,
        Afternoon,
        Evening,
        Night
    }

    [Serializable]
    public struct GameTime
    {
        public int day;
        public TimePeriod period;

        public static GameTime Default => new GameTime { day = 1, period = TimePeriod.Morning };

        public void AdvancePeriod()
        {
            switch (period)
            {
                case TimePeriod.Morning:
                    period = TimePeriod.Afternoon;
                    break;
                case TimePeriod.Afternoon:
                    period = TimePeriod.Evening;
                    break;
                case TimePeriod.Evening:
                    period = TimePeriod.Night;
                    break;
                default:
                    period = TimePeriod.Morning;
                    day = Math.Max(1, day + 1);
                    break;
            }
        }

        public void AdvanceDay()
        {
            day = Math.Max(1, day + 1);
        }

        public static TimePeriod ParsePeriod(string value)
        {
            if (string.IsNullOrEmpty(value)) return TimePeriod.Morning;
            switch (value.ToLowerInvariant())
            {
                case "afternoon": return TimePeriod.Afternoon;
                case "evening": return TimePeriod.Evening;
                case "night": return TimePeriod.Night;
                default: return TimePeriod.Morning;
            }
        }

        public static string PeriodToString(TimePeriod p)
        {
            switch (p)
            {
                case TimePeriod.Afternoon: return "afternoon";
                case TimePeriod.Evening: return "evening";
                case TimePeriod.Night: return "night";
                default: return "morning";
            }
        }

        public static string PeriodDisplayName(TimePeriod p)
        {
            switch (p)
            {
                case TimePeriod.Afternoon: return "下午";
                case TimePeriod.Evening: return "傍晚";
                case TimePeriod.Night: return "夜间";
                default: return "上午";
            }
        }

        public string DisplayString => $"第 {day} 天 · {PeriodDisplayName(period)}";

        public bool MatchesPeriod(string required)
        {
            if (string.IsNullOrEmpty(required) || required.Equals("any", StringComparison.OrdinalIgnoreCase))
                return true;
            return period == ParsePeriod(required);
        }
    }
}
