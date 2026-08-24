using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Editor
{
    [Serializable]
    public class NodePositionEntry
    {
        public string id;
        public float x;
        public float y;
    }

    [Serializable]
    public class ScenarioEditorMeta
    {
        public List<NodePositionEntry> nodePositions = new List<NodePositionEntry>();
        public float viewportX;
        public float viewportY;
        public float viewportZoom = 1f;
    }

    public class StoryProjectData
    {
        public string scenarioFolder;
        public ScenarioFile scenario = new ScenarioFile();
        public List<NPCDefinition> npcs = new List<NPCDefinition>();
        public List<LocationDefinition> locations = new List<LocationDefinition>();
        public List<ItemDefinition> items = new List<ItemDefinition>();
        public List<NpcRelationship> relationships = new List<NpcRelationship>();
        public ScenarioEditorMeta editorMeta = new ScenarioEditorMeta();

        public Dictionary<string, Vector2> GetPositionMap()
        {
            var map = new Dictionary<string, Vector2>();
            if (editorMeta?.nodePositions == null) return map;
            foreach (var entry in editorMeta.nodePositions)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.id))
                    map[entry.id] = new Vector2(entry.x, entry.y);
            }
            return map;
        }

        public void SetPosition(string nodeId, Vector2 pos)
        {
            editorMeta.nodePositions ??= new List<NodePositionEntry>();
            for (var i = 0; i < editorMeta.nodePositions.Count; i++)
            {
                if (editorMeta.nodePositions[i].id == nodeId)
                {
                    editorMeta.nodePositions[i].x = pos.x;
                    editorMeta.nodePositions[i].y = pos.y;
                    return;
                }
            }
            editorMeta.nodePositions.Add(new NodePositionEntry { id = nodeId, x = pos.x, y = pos.y });
        }
    }

    public static class StoryJsonIO
    {
        const string DataRoot = "Assets/Resources/Data";

        public static string GetScenarioFolder(string scenarioId)
        {
            return $"{DataRoot}/Scenarios/{scenarioId}";
        }

        public static StoryProjectData Load(string scenarioId)
        {
            var folder = GetScenarioFolder(scenarioId);
            var project = new StoryProjectData { scenarioFolder = folder };

            var nodesPath = $"{folder}/nodes.json";
            if (File.Exists(nodesPath))
            {
                var json = File.ReadAllText(nodesPath);
                project.scenario = JsonUtility.FromJson<ScenarioFile>(json) ?? new ScenarioFile();
            }
            project.scenario.nodes ??= new List<StoryNodeData>();
            project.scenario.scenarioId = scenarioId;

            project.npcs = LoadList<NpcListWrapper, NPCDefinition>(
                $"{DataRoot}/NPCs/npcs.json", w => w.npcs);
            project.locations = LoadList<LocationListWrapper, LocationDefinition>(
                $"{DataRoot}/NPCs/locations.json", w => w.locations);
            project.items = LoadList<ItemListWrapper, ItemDefinition>(
                $"{DataRoot}/Items/items.json", w => w.items);
            project.relationships = LoadList<RelationshipListWrapper, NpcRelationship>(
                $"{DataRoot}/NPCs/relationships.json", w => w.relationships);

            var metaPath = $"{folder}/nodes.editor.json";
            if (File.Exists(metaPath))
            {
                project.editorMeta = JsonUtility.FromJson<ScenarioEditorMeta>(File.ReadAllText(metaPath))
                                   ?? new ScenarioEditorMeta();
            }

            return project;
        }

        static List<TItem> LoadList<TWrapper, TItem>(string path, Func<TWrapper, List<TItem>> selector)
            where TWrapper : new()
        {
            if (!File.Exists(path)) return new List<TItem>();
            var wrapper = JsonUtility.FromJson<TWrapper>(File.ReadAllText(path));
            return selector(wrapper) ?? new List<TItem>();
        }

        public static void Save(StoryProjectData project)
        {
            if (project == null) return;
            Directory.CreateDirectory(project.scenarioFolder);

            project.scenario.nodes ??= new List<StoryNodeData>();
            File.WriteAllText(
                $"{project.scenarioFolder}/nodes.json",
                JsonUtility.ToJson(project.scenario, true));

            WriteWrapper($"{DataRoot}/NPCs/npcs.json",
                new NpcListWrapper { npcs = project.npcs ?? new List<NPCDefinition>() });
            WriteWrapper($"{DataRoot}/NPCs/locations.json",
                new LocationListWrapper { locations = project.locations ?? new List<LocationDefinition>() });
            WriteWrapper($"{DataRoot}/Items/items.json",
                new ItemListWrapper { items = project.items ?? new List<ItemDefinition>() });
            WriteWrapper($"{DataRoot}/NPCs/relationships.json",
                new RelationshipListWrapper { relationships = project.relationships ?? new List<NpcRelationship>() });

            project.editorMeta ??= new ScenarioEditorMeta();
            File.WriteAllText(
                $"{project.scenarioFolder}/nodes.editor.json",
                JsonUtility.ToJson(project.editorMeta, true));
        }

        static void WriteWrapper<T>(string path, T wrapper)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? DataRoot);
            File.WriteAllText(path, JsonUtility.ToJson(wrapper, true));
        }
    }
}
