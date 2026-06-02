using System.Collections.Generic;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.Narrative;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Core
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public Investigator Investigator { get; private set; }
        public Inventory.Inventory Inventory { get; } = new Inventory.Inventory();
        public HashSet<string> Flags { get; } = new HashSet<string>();

        public string CurrentScenarioId { get; set; } = ScenarioRegistry.DefaultScenarioId;
        public string CurrentNodeId { get; set; }
        public string CurrentLocationId { get; set; } = "cafe_main";

        public CombatState ActiveCombat { get; set; }
        public bool HasPendingCombatReturn { get; set; }
        public string PostCombatNodeId { get; set; }

        public int SaveSlot { get; set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetForNewGame()
        {
            Investigator = null;
            Inventory.Clear();
            Flags.Clear();
            CurrentScenarioId = ScenarioRegistry.DefaultScenarioId;
            CurrentNodeId = null;
            CurrentLocationId = "cafe_main";
            ActiveCombat = null;
            HasPendingCombatReturn = false;
            PostCombatNodeId = null;
        }

        public void SetInvestigator(Investigator investigator)
        {
            Investigator = investigator;
        }

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && Flags.Contains(flag);

        public void SetFlag(string flag, bool value = true)
        {
            if (string.IsNullOrEmpty(flag)) return;
            if (value) Flags.Add(flag);
            else Flags.Remove(flag);
        }

        public GameSaveData ToSaveData()
        {
            return new GameSaveData
            {
                scenarioId = CurrentScenarioId,
                nodeId = CurrentNodeId,
                locationId = CurrentLocationId,
                flags = new List<string>(Flags),
                investigator = Investigator?.ToData(),
                inventoryItemIds = Inventory.GetItemIds(),
                postCombatNodeId = PostCombatNodeId,
                hasPendingCombatReturn = HasPendingCombatReturn
            };
        }

        public void LoadFromSaveData(GameSaveData data)
        {
            if (data == null) return;

            CurrentScenarioId = data.scenarioId;
            CurrentNodeId = data.nodeId;
            CurrentLocationId = data.locationId;
            PostCombatNodeId = data.postCombatNodeId;
            HasPendingCombatReturn = data.hasPendingCombatReturn;

            Flags.Clear();
            if (data.flags != null)
            {
                foreach (var flag in data.flags)
                    Flags.Add(flag);
            }

            Investigator = data.investigator != null
                ? Investigator.FromData(data.investigator)
                : null;

            Inventory.LoadItems(data.inventoryItemIds);
        }
    }
}
