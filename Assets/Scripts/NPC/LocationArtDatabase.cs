using System.Collections.Generic;
using UnityEngine;

namespace WalkingIntoNight.TRPG.NPC
{
    public static class LocationArtDatabase
    {
        const string BasePath = "Art/Backgrounds/";

        static Dictionary<string, Sprite> s_cache;

        public static Sprite Get(string backgroundId)
        {
            if (string.IsNullOrEmpty(backgroundId)) return null;

            s_cache ??= new Dictionary<string, Sprite>();
            if (s_cache.TryGetValue(backgroundId, out var cached))
                return cached;

            var sprite = Resources.Load<Sprite>(BasePath + backgroundId);
            if (sprite != null)
                s_cache[backgroundId] = sprite;
            return sprite;
        }

        public static void ClearCache() => s_cache = null;
    }
}
