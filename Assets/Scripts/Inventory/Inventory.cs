using System.Collections.Generic;

namespace WalkingIntoNight.TRPG.Inventory
{
    public class Inventory
    {
        readonly List<string> m_itemIds = new List<string>();

        public IReadOnlyList<string> Items => m_itemIds;

        public void Clear() => m_itemIds.Clear();

        public bool HasItem(string itemId) => !string.IsNullOrEmpty(itemId) && m_itemIds.Contains(itemId);

        public void AddItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            for (var i = 0; i < count; i++)
                m_itemIds.Add(itemId);
        }

        public bool RemoveItem(string itemId)
        {
            var idx = m_itemIds.IndexOf(itemId);
            if (idx < 0) return false;
            m_itemIds.RemoveAt(idx);
            return true;
        }

        public List<string> GetItemIds() => new List<string>(m_itemIds);

        public void LoadItems(List<string> ids)
        {
            m_itemIds.Clear();
            if (ids == null) return;
            m_itemIds.AddRange(ids);
        }
    }
}
