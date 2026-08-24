using System;
using System.Collections.Generic;

namespace WalkingIntoNight.TRPG.NPC
{
    [Serializable]
    public class NpcScheduleEntry
    {
        public int day;
        public string period;
        public string locationId;
    }

    [Serializable]
    public class NPCDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public string portraitId;
        public string defaultNodeId;
        public List<string> locationIds = new List<string>();
        public List<NpcScheduleEntry> schedules = new List<NpcScheduleEntry>();
    }

    [Serializable]
    public class LocationDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public List<string> npcIds = new List<string>();
        public string backgroundId;
        public string requiredItemId;
        public string requiredFlag;
        public string requiredPeriod;
    }

    [Serializable]
    public class NpcListWrapper
    {
        public List<NPCDefinition> npcs;
    }

    [Serializable]
    public class LocationListWrapper
    {
        public List<LocationDefinition> locations;
    }
}
