using System;
using System.Collections.Generic;
using WalkingIntoNight.TRPG.Character;

namespace WalkingIntoNight.TRPG.Core
{
    [Serializable]
    public class GameSaveData
    {
        public string scenarioId;
        public string nodeId;
        public string locationId;
        public List<string> flags = new List<string>();
        public InvestigatorData investigator;
        public List<string> inventoryItemIds = new List<string>();
        public string postCombatNodeId;
        public bool hasPendingCombatReturn;
        public long savedAtTicks;
    }
}
