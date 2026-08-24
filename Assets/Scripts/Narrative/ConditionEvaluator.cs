using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Narrative
{
    public enum ChoiceBlockReason
    {
        None,
        InvalidChoice,
        MissingFlag,
        BlockedByFlag,
        MissingItem,
        WrongDay,
        TooEarly,
        TooLate,
        WrongPeriod,
        MissingRelationship
    }

    public readonly struct ChoiceAvailability
    {
        public bool IsAvailable { get; }
        public ChoiceBlockReason BlockReason { get; }
        public string Reason { get; }

        ChoiceAvailability(bool isAvailable, ChoiceBlockReason blockReason, string reason)
        {
            IsAvailable = isAvailable;
            BlockReason = blockReason;
            Reason = reason;
        }

        public static ChoiceAvailability Available() =>
            new ChoiceAvailability(true, ChoiceBlockReason.None, null);

        public static ChoiceAvailability Blocked(ChoiceBlockReason reason, string message) =>
            new ChoiceAvailability(false, reason, message);
    }

    public static class ConditionEvaluator
    {
        public static ChoiceAvailability EvaluateChoice(
            StoryChoiceData choice,
            GameStateManager gameState = null)
        {
            if (choice == null)
                return ChoiceAvailability.Blocked(
                    ChoiceBlockReason.InvalidChoice,
                    "这个行动暂时不可用。");

            var state = gameState != null ? gameState : GameStateManager.Instance;
            if (state == null) return ChoiceAvailability.Available();

            if (!string.IsNullOrEmpty(choice.requiredFlag) && !state.HasFlag(choice.requiredFlag))
                return Blocked(choice, ChoiceBlockReason.MissingFlag, "还缺少必要的剧情线索。");

            if (!string.IsNullOrEmpty(choice.blockedByFlag) && state.HasFlag(choice.blockedByFlag))
                return Blocked(choice, ChoiceBlockReason.BlockedByFlag, "先前的选择已经关闭了这条路线。");

            if (!string.IsNullOrEmpty(choice.requiredItemId) && !state.Inventory.HasItem(choice.requiredItemId))
            {
                var item = ItemDatabase.Get(choice.requiredItemId);
                return Blocked(
                    choice,
                    ChoiceBlockReason.MissingItem,
                    $"需要先取得「{item?.displayName ?? choice.requiredItemId}」。");
            }

            var time = state.CurrentTime;
            if (choice.requiredDay > 0 && time.day != choice.requiredDay)
                return Blocked(choice, ChoiceBlockReason.WrongDay, $"只能在第 {choice.requiredDay} 天行动。");

            if (choice.requiredMinDay > 0 && time.day < choice.requiredMinDay)
                return Blocked(choice, ChoiceBlockReason.TooEarly, $"需要等到第 {choice.requiredMinDay} 天。");

            if (choice.requiredMaxDay > 0 && time.day > choice.requiredMaxDay)
                return Blocked(choice, ChoiceBlockReason.TooLate, $"这条线索已在第 {choice.requiredMaxDay} 天后失效。");

            if (!string.IsNullOrEmpty(choice.requiredPeriod) && !time.MatchesPeriod(choice.requiredPeriod))
            {
                var period = GameTime.PeriodDisplayName(GameTime.ParsePeriod(choice.requiredPeriod));
                return Blocked(choice, ChoiceBlockReason.WrongPeriod, $"只能在{period}行动。");
            }

            if (!string.IsNullOrEmpty(choice.requiredRelationship) &&
                !state.HasRelationshipUnlocked(choice.requiredRelationship))
            {
                var relationship = NPCDatabase.GetRelationship(choice.requiredRelationship);
                var label = relationship?.label ?? choice.requiredRelationship;
                return Blocked(
                    choice,
                    ChoiceBlockReason.MissingRelationship,
                    $"需要先了解关系线索「{label}」。");
            }

            return ChoiceAvailability.Available();
        }

        public static bool MeetsChoiceRequirements(
            StoryChoiceData choice,
            GameStateManager gameState = null) =>
            EvaluateChoice(choice, gameState).IsAvailable;

        static ChoiceAvailability Blocked(
            StoryChoiceData choice,
            ChoiceBlockReason reason,
            string fallback)
        {
            var message = string.IsNullOrWhiteSpace(choice.unavailableReason)
                ? fallback
                : choice.unavailableReason;
            return ChoiceAvailability.Blocked(reason, message);
        }
    }
}
