using System;
using System.Collections.Generic;

namespace AnimalCafe.TRPG.Combat
{
    [Serializable]
    public class CombatantData
    {
        public string id;
        public string displayName;
        public bool isPlayer;
        public int HP;
        public int MaxHP;
        public int skillFight;
        public int skillDodge;
    }

    [Serializable]
    public class CombatState
    {
        public string encounterId;
        public List<CombatantData> combatants = new List<CombatantData>();
        public int activeIndex;
        public bool playerTurn = true;
        public bool ended;
        public bool playerWon;
        public bool playerFled;
    }
}
