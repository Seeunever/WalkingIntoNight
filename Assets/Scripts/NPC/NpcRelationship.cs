using System;
using System.Collections.Generic;

namespace WalkingIntoNight.TRPG.NPC
{
    [Serializable]
    public class NpcRelationship
    {
        public string id;
        public string fromNpcId;
        public string toNpcId;
        public string type;
        public string label;
        public string description;
        public string unlockFlag;
    }

    [Serializable]
    public class RelationshipListWrapper
    {
        public List<NpcRelationship> relationships = new List<NpcRelationship>();
    }
}
