using System;

namespace WalkingIntoNight.TRPG.Inventory
{
    [Serializable]
    public class ItemDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public bool consumable;
        public int healHp;
        public int healSan;
        public string useNodeId;
    }
}
