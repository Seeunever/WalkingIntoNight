using System.Collections.Generic;
using UnityEngine;

namespace AnimalCafe.TRPG.Narrative
{
    public static class ScenarioLoader
    {
        public static ScenarioFile Load(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"Scenario not found at Resources/{resourcePath}");
                return null;
            }

            return JsonUtility.FromJson<ScenarioFile>(asset.text);
        }

        public static Dictionary<string, StoryNodeData> BuildLookup(ScenarioFile file)
        {
            var dict = new Dictionary<string, StoryNodeData>();
            if (file?.nodes == null) return dict;
            foreach (var node in file.nodes)
            {
                if (!string.IsNullOrEmpty(node.id))
                    dict[node.id] = node;
            }
            return dict;
        }
    }
}
