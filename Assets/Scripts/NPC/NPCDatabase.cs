using System.Collections.Generic;
using UnityEngine;

namespace AnimalCafe.TRPG.NPC
{
    public static class NPCDatabase
    {
        static Dictionary<string, NPCDefinition> s_npcs;
        static Dictionary<string, LocationDefinition> s_locations;

        public static void EnsureLoaded()
        {
            if (s_npcs != null) return;
            s_npcs = new Dictionary<string, NPCDefinition>();
            s_locations = new Dictionary<string, LocationDefinition>();

            var npcAsset = Resources.Load<TextAsset>("Data/NPCs/npcs");
            if (npcAsset != null)
            {
                var wrapper = JsonUtility.FromJson<NpcListWrapper>(npcAsset.text);
                if (wrapper?.npcs != null)
                    foreach (var n in wrapper.npcs)
                        if (!string.IsNullOrEmpty(n.id)) s_npcs[n.id] = n;
            }

            var locAsset = Resources.Load<TextAsset>("Data/NPCs/locations");
            if (locAsset != null)
            {
                var wrapper = JsonUtility.FromJson<LocationListWrapper>(locAsset.text);
                if (wrapper?.locations != null)
                    foreach (var l in wrapper.locations)
                        if (!string.IsNullOrEmpty(l.id)) s_locations[l.id] = l;
            }

            if (s_npcs.Count == 0) RegisterDefaultNpcs();
            if (s_locations.Count == 0) RegisterDefaultLocations();
        }

        static void RegisterDefaultNpcs()
        {
            AddNpc(new NPCDefinition
            {
                id = "barista_mei", displayName = "店员小梅", portraitId = "rabbit",
                description = "夜班店员，神情疲惫。", defaultNodeId = "npc_mei_talk",
                locationIds = new List<string> { "cafe_main" }
            });
            AddNpc(new NPCDefinition
            {
                id = "regular_chen", displayName = "常客老陈", portraitId = "bear",
                description = "总在角落读报的老人。", defaultNodeId = "npc_chen_talk",
                locationIds = new List<string> { "cafe_main" }
            });
            AddNpc(new NPCDefinition
            {
                id = "stray_cat", displayName = "店猫", portraitId = "fox",
                description = "蹲在柜台上的猫。", defaultNodeId = "npc_cat_talk",
                locationIds = new List<string> { "cafe_main", "cafe_storage" }
            });
            AddNpc(new NPCDefinition
            {
                id = "silent_woman", displayName = "黑衣女人", portraitId = "swan",
                description = "只在关店后出现。", defaultNodeId = "npc_woman_talk",
                locationIds = new List<string> { "cafe_basement" }
            });
            AddNpc(new NPCDefinition
            {
                id = "owner_ghost", displayName = "老板的影子", portraitId = "bear",
                description = "不稳定的存在。", defaultNodeId = "npc_owner_talk",
                locationIds = new List<string> { "cafe_basement" }
            });
        }

        static void RegisterDefaultLocations()
        {
            AddLoc(new LocationDefinition
            {
                id = "cafe_main", displayName = "咖啡馆大厅", description = "关店后的空荡大厅。",
                npcIds = new List<string> { "barista_mei", "regular_chen", "stray_cat" }
            });
            AddLoc(new LocationDefinition
            {
                id = "cafe_storage", displayName = "储藏室", description = "堆满原料与杂物。",
                npcIds = new List<string> { "stray_cat" }
            });
            AddLoc(new LocationDefinition
            {
                id = "cafe_basement", displayName = "地下室", description = "潮湿阴暗，有不祥的气息。",
                npcIds = new List<string> { "silent_woman", "owner_ghost" }
            });
        }

        static void AddNpc(NPCDefinition n) => s_npcs[n.id] = n;
        static void AddLoc(LocationDefinition l) => s_locations[l.id] = l;

        public static NPCDefinition GetNpc(string id)
        {
            EnsureLoaded();
            return id != null && s_npcs.TryGetValue(id, out var n) ? n : null;
        }

        public static LocationDefinition GetLocation(string id)
        {
            EnsureLoaded();
            return id != null && s_locations.TryGetValue(id, out var l) ? l : null;
        }

        public static IEnumerable<LocationDefinition> AllLocations
        {
            get
            {
                EnsureLoaded();
                return s_locations.Values;
            }
        }
    }
}
