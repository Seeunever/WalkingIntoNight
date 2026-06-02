using System.Collections.Generic;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Inventory
{
    public static class ItemDatabase
    {
        static Dictionary<string, ItemDefinition> s_items;

        public static void EnsureLoaded()
        {
            if (s_items != null) return;
            s_items = new Dictionary<string, ItemDefinition>();

            var asset = Resources.Load<TextAsset>("Data/Items/items");
            if (asset == null)
            {
                RegisterDefaults();
                return;
            }

            var wrapper = JsonUtility.FromJson<ItemListWrapper>(asset.text);
            if (wrapper?.items != null)
            {
                foreach (var item in wrapper.items)
                {
                    if (!string.IsNullOrEmpty(item.id))
                        s_items[item.id] = item;
                }
            }
        }

        static void RegisterDefaults()
        {
            Add(new ItemDefinition { id = "flashlight", displayName = "手电筒", description = "照亮黑暗角落。", consumable = false });
            Add(new ItemDefinition { id = "notebook", displayName = "调查笔记", description = "记录线索的笔记本。", consumable = false });
            Add(new ItemDefinition { id = "rusty_key", displayName = "生锈的钥匙", description = "也许能打开储藏室。", consumable = false });
            Add(new ItemDefinition { id = "first_aid_kit", displayName = "急救包", description = "恢复少量生命值。", consumable = true, healHp = 3 });
            Add(new ItemDefinition { id = "calming_tea", displayName = "安神茶", description = "恢复少量理智。", consumable = true, healSan = 5 });
            Add(new ItemDefinition { id = "strange_symbol", displayName = "奇怪符号拓片", description = "从地下室墙上拓下的符号。", consumable = false });
            Add(new ItemDefinition { id = "silver_coin", displayName = "银币", description = "年代不明的银币。", consumable = false });
            Add(new ItemDefinition { id = "owner_diary", displayName = "老板的日记", description = "咖啡店老板的秘密日记。", consumable = false });
            Add(new ItemDefinition { id = "cold_touch", displayName = "寒意残留", description = "剧情标记物品。", consumable = false });
        }

        static void Add(ItemDefinition def) => s_items[def.id] = def;

        public static ItemDefinition Get(string id)
        {
            EnsureLoaded();
            return id != null && s_items.TryGetValue(id, out var def) ? def : null;
        }

        public static IEnumerable<ItemDefinition> All
        {
            get
            {
                EnsureLoaded();
                return s_items.Values;
            }
        }

        [System.Serializable]
        class ItemListWrapper
        {
            public List<ItemDefinition> items;
        }
    }
}
