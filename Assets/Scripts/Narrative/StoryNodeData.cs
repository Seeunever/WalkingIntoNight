using System;
using System.Collections.Generic;
using WalkingIntoNight.TRPG.Dice;

namespace WalkingIntoNight.TRPG.Narrative
{
    [Serializable]
    public class StoryChoiceData
    {
        public string text;
        public string nextNodeId;
        public string requiredFlag;
        public string blockedByFlag;
        public string requiredItemId;
    }

    [Serializable]
    public class StoryNodeData
    {
        public string id;
        public string type;
        public string speaker;
        public string text;
        public string portraitId;
        public string locationId;
        public List<StoryChoiceData> choices = new List<StoryChoiceData>();
        public string nextNodeId;

        public string skillId;
        public int difficulty;
        public string successNodeId;
        public string failureNodeId;
        public int bonusDice;
        public int penaltyDice;

        public string flag;
        public bool flagValue = true;
        public string itemId;
        public int itemCount = 1;
        public int sanDelta;

        public string combatId;
        public string winNodeId;
        public string loseNodeId;
        public string fleeNodeId;

        public string npcId;
        public string endTitle;
    }

    [Serializable]
    public class ScenarioFile
    {
        public string scenarioId;
        public string title;
        public string startNodeId;
        public List<StoryNodeData> nodes = new List<StoryNodeData>();
    }

    public enum StoryNodeType
    {
        Dialogue,
        Check,
        SetFlag,
        GiveItem,
        ChangeSan,
        Combat,
        Location,
        NpcHub,
        End
    }

    public static class StoryNodeTypeParser
    {
        public static StoryNodeType Parse(string type)
        {
            if (string.IsNullOrEmpty(type)) return StoryNodeType.Dialogue;
            switch (type.ToLowerInvariant())
            {
                case "check": return StoryNodeType.Check;
                case "setflag": return StoryNodeType.SetFlag;
                case "giveitem": return StoryNodeType.GiveItem;
                case "changesan": return StoryNodeType.ChangeSan;
                case "combat": return StoryNodeType.Combat;
                case "location": return StoryNodeType.Location;
                case "npchub": return StoryNodeType.NpcHub;
                case "end": return StoryNodeType.End;
                default: return StoryNodeType.Dialogue;
            }
        }
    }
}
