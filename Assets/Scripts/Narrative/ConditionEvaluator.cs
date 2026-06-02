using WalkingIntoNight.TRPG.Core;

namespace WalkingIntoNight.TRPG.Narrative
{
    public static class ConditionEvaluator
    {
        public static bool MeetsChoiceRequirements(StoryChoiceData choice)
        {
            if (choice == null) return false;
            var state = GameStateManager.Instance;
            if (state == null) return true;

            if (!string.IsNullOrEmpty(choice.requiredFlag) && !state.HasFlag(choice.requiredFlag))
                return false;

            if (!string.IsNullOrEmpty(choice.blockedByFlag) && state.HasFlag(choice.blockedByFlag))
                return false;

            if (!string.IsNullOrEmpty(choice.requiredItemId) && !state.Inventory.HasItem(choice.requiredItemId))
                return false;

            return true;
        }
    }
}
