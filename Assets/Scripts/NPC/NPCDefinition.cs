using System;
using System.Collections.Generic;

namespace AnimalCafe.TRPG.NPC
{
    [Serializable]
    public class NPCDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public string portraitId;
        public string defaultNodeId;
        public List<string> locationIds = new List<string>();
    }

    [Serializable]
    public class LocationDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public List<string> npcIds = new List<string>();
        public string backgroundId;
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
