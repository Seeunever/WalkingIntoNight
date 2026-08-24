using System.Collections.Generic;
using UnityEngine;

namespace WalkingIntoNight.TRPG.NPC
{
    /// <summary>
    /// Loads the production dialogue portraits shipped with the current build.
    /// </summary>
    public static class PortraitDatabase
    {
        static readonly string[] s_basePaths =
        {
            "Art/Characters/"
        };

        static Dictionary<string, Sprite> s_cache;

        public static Sprite Get(string portraitId)
        {
            if (string.IsNullOrEmpty(portraitId)) return null;

            s_cache ??= new Dictionary<string, Sprite>();
            if (s_cache.TryGetValue(portraitId, out var cached))
                return cached;

            Sprite sprite = null;
            foreach (var basePath in s_basePaths)
            {
                sprite = Resources.Load<Sprite>(basePath + portraitId);
                if (sprite != null) break;
            }
            if (sprite != null)
                s_cache[portraitId] = sprite;
            return sprite;
        }

        public static Texture2D GetTexture(string portraitId)
        {
            var sprite = Get(portraitId);
            return sprite != null ? sprite.texture : null;
        }

        public static void ClearCache() => s_cache = null;
    }
}
