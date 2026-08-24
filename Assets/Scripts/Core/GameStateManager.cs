using System;
using System.Collections.Generic;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.NPC;
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
        public GameTime CurrentTime { get; private set; } = GameTime.Default;

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
            CurrentTime = GameTime.Default;
            ActiveCombat = null;
            HasPendingCombatReturn = false;
            PostCombatNodeId = null;
            SaveSlot = 0;
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

        public void AdvanceTimePeriods(int count)
        {
            var time = CurrentTime;
            for (var i = 0; i < count; i++)
                time.AdvancePeriod();
            CurrentTime = time;
        }

        public void AdvanceTimeDays(int count)
        {
            var time = CurrentTime;
            for (var i = 0; i < count; i++)
                time.AdvanceDay();
            CurrentTime = time;
        }

        public void SetTime(int day, TimePeriod period)
        {
            CurrentTime = new GameTime
            {
                day = day > 0 ? day : 1,
                period = period
            };
        }

        public void WaitUntilNextDayMorning()
        {
            CurrentTime = new GameTime
            {
                day = CurrentTime.day + 1,
                period = TimePeriod.Morning
            };
        }

        public bool HasRelationshipUnlocked(string relationshipId)
        {
            if (string.IsNullOrEmpty(relationshipId)) return true;
            var rel = NPCDatabase.GetRelationship(relationshipId);
            if (rel == null) return HasFlag($"rel_{relationshipId}");
            if (!string.IsNullOrEmpty(rel.unlockFlag))
                return HasFlag(rel.unlockFlag);
            return HasFlag($"rel_{relationshipId}");
        }

        public GameSaveData ToSaveData()
        {
            return new GameSaveData
            {
                version = GameSaveData.CurrentVersion,
                scenarioId = CurrentScenarioId,
                nodeId = CurrentNodeId,
                locationId = CurrentLocationId,
                flags = new List<string>(Flags),
                investigator = Investigator?.ToData(),
                inventoryItemIds = Inventory.GetItemIds(),
                postCombatNodeId = PostCombatNodeId,
                hasPendingCombatReturn = HasPendingCombatReturn,
                currentDay = CurrentTime.day,
                currentPeriod = GameTime.PeriodToString(CurrentTime.period)
            };
        }

        public static bool TryValidateSaveData(GameSaveData data, out string error)
        {
            error = null;
            if (data == null)
            {
                error = "存档数据为空。";
                return false;
            }

            if (data.version > GameSaveData.CurrentVersion)
            {
                error = $"存档版本 {data.version} 高于当前支持版本 {GameSaveData.CurrentVersion}。";
                return false;
            }

            var registryEntry = ScenarioRegistry.Get(data.scenarioId);
            if (registryEntry == null)
            {
                error = $"存档引用了未知剧本：{data.scenarioId ?? "（空）"}。";
                return false;
            }

            ScenarioFile scenario;
            try
            {
                scenario = ScenarioLoader.Load(registryEntry.ResourcePath);
            }
            catch (Exception ex)
            {
                error = $"剧本数据读取失败：{ex.Message}";
                return false;
            }

            if (scenario?.nodes == null)
            {
                error = $"剧本数据不存在：{data.scenarioId}。";
                return false;
            }

            var nodeFound = false;
            StoryNodeData savedNode = null;
            foreach (var node in scenario.nodes)
            {
                if (node == null || node.id != data.nodeId) continue;
                nodeFound = true;
                savedNode = node;
                break;
            }

            if (!nodeFound)
            {
                error = $"存档节点不存在：{data.nodeId ?? "（空）"}。";
                return false;
            }

            var savedNodeType = StoryNodeTypeParser.Parse(savedNode.type);
            if (savedNodeType == StoryNodeType.Combat || savedNodeType == StoryNodeType.Check)
            {
                error = "该存档停在无法安全恢复的战斗或检定节点。";
                return false;
            }

            if (NPCDatabase.GetLocation(data.locationId) == null)
            {
                error = $"存档地点不存在：{data.locationId ?? "（空）"}。";
                return false;
            }

            if (data.investigator == null)
            {
                error = "存档缺少调查员数据。";
                return false;
            }

            var investigator = data.investigator;
            if (investigator.MaxHP < 1 || investigator.HP < 0 || investigator.HP > investigator.MaxHP ||
                investigator.MaxSAN < 1 || investigator.SAN < 0 || investigator.SAN > investigator.MaxSAN ||
                investigator.MaxMP < 1 || investigator.MP < 0 || investigator.MP > investigator.MaxMP)
            {
                error = "存档中的调查员状态无效。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(investigator.name))
                investigator.name = "无名调查员";
            investigator.skills ??= new List<SkillEntry>();

            data.flags ??= new List<string>();
            data.flags.RemoveAll(string.IsNullOrEmpty);
            data.inventoryItemIds ??= new List<string>();
            data.inventoryItemIds.RemoveAll(string.IsNullOrEmpty);
            data.currentDay = data.currentDay > 0 ? data.currentDay : 1;
            data.currentPeriod = GameTime.PeriodToString(GameTime.ParsePeriod(data.currentPeriod));
            data.version = GameSaveData.CurrentVersion;
            return true;
        }

        public void LoadFromSaveData(GameSaveData data)
        {
            if (data == null) return;

            CurrentScenarioId = data.scenarioId;
            CurrentNodeId = data.nodeId;
            CurrentLocationId = data.locationId;
            PostCombatNodeId = data.postCombatNodeId;
            HasPendingCombatReturn = data.hasPendingCombatReturn;
            ActiveCombat = null;

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

            CurrentTime = new GameTime
            {
                day = data.currentDay > 0 ? data.currentDay : 1,
                period = GameTime.ParsePeriod(data.currentPeriod)
            };
        }
    }
}
