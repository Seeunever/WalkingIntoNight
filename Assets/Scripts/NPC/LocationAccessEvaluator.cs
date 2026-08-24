using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Inventory;

namespace WalkingIntoNight.TRPG.NPC
{
    public enum LocationAccessBlockReason
    {
        None,
        UnknownLocation,
        MissingItem,
        MissingFlag,
        WrongTime,
        MissingGameState
    }

    public sealed class LocationAccessResult
    {
        public bool CanEnter { get; private set; }
        public LocationAccessBlockReason BlockReason { get; private set; }
        public string Message { get; private set; }

        public static LocationAccessResult Allowed() => new LocationAccessResult
        {
            CanEnter = true,
            BlockReason = LocationAccessBlockReason.None,
            Message = null
        };

        public static LocationAccessResult Blocked(
            LocationAccessBlockReason reason,
            string message) => new LocationAccessResult
        {
            CanEnter = false,
            BlockReason = reason,
            Message = message
        };
    }

    public static class LocationAccessEvaluator
    {
        public static LocationAccessResult Evaluate(
            LocationDefinition location,
            GameStateManager gameState)
        {
            if (location == null)
            {
                return LocationAccessResult.Blocked(
                    LocationAccessBlockReason.UnknownLocation,
                    "地点不存在");
            }

            if (gameState == null)
            {
                return LocationAccessResult.Blocked(
                    LocationAccessBlockReason.MissingGameState,
                    "游戏状态不可用");
            }

            if (!string.IsNullOrWhiteSpace(location.requiredItemId) &&
                !gameState.Inventory.HasItem(location.requiredItemId))
            {
                var item = ItemDatabase.Get(location.requiredItemId);
                return LocationAccessResult.Blocked(
                    LocationAccessBlockReason.MissingItem,
                    $"需要「{item?.displayName ?? location.requiredItemId}」");
            }

            if (!string.IsNullOrWhiteSpace(location.requiredFlag) &&
                !gameState.HasFlag(location.requiredFlag))
            {
                return LocationAccessResult.Blocked(
                    LocationAccessBlockReason.MissingFlag,
                    "尚未解锁");
            }

            if (!string.IsNullOrWhiteSpace(location.requiredPeriod) &&
                !gameState.CurrentTime.MatchesPeriod(location.requiredPeriod))
            {
                var period = GameTime.ParsePeriod(location.requiredPeriod);
                return LocationAccessResult.Blocked(
                    LocationAccessBlockReason.WrongTime,
                    $"仅在{GameTime.PeriodDisplayName(period)}可进入");
            }

            return LocationAccessResult.Allowed();
        }
    }
}
