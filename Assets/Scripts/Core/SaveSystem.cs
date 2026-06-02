using System.IO;
using UnityEngine;

namespace AnimalCafe.TRPG.Core
{
    public static class SaveSystem
    {
        const int SlotCount = 3;

        public static bool HasSave(int slot)
        {
            return File.Exists(GetPath(slot));
        }

        public static void Save(int slot, GameSaveData data)
        {
            data.savedAtTicks = System.DateTime.UtcNow.Ticks;
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetPath(slot), json);
        }

        public static GameSaveData Load(int slot)
        {
            var path = GetPath(slot);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
        }

        public static void Delete(int slot)
        {
            var path = GetPath(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        public static int SlotCountMax => SlotCount;

        static string GetPath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"trpg_save_{slot}.json");
        }
    }
}
