using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Inventory;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Editor
{
    public static class StoryValidator
    {
        static readonly HashSet<string> SupportedNodeTypes = new HashSet<string>
        {
            "dialogue",
            "check",
            "setflag",
            "giveitem",
            "changesan",
            "combat",
            "location",
            "npchub",
            "advancetime",
            "end"
        };

        static readonly HashSet<string> SupportedPeriods = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "any",
            "morning",
            "afternoon",
            "evening",
            "night"
        };

        public class ValidationIssue
        {
            public bool isError;
            public string message;
        }

        public static List<ValidationIssue> Validate(
            StoryProjectData project,
            Func<string, bool> portraitExists = null)
        {
            var issues = new List<ValidationIssue>();
            if (project?.scenario?.nodes == null)
            {
                issues.Add(new ValidationIssue { isError = true, message = "剧本无节点数据。" });
                return issues;
            }

            var nodes = project.scenario.nodes;
            var npcs = project.npcs ?? new List<NPCDefinition>();
            var locations = project.locations ?? new List<LocationDefinition>();
            var items = project.items ?? new List<ItemDefinition>();
            var relationships = project.relationships ?? new List<NpcRelationship>();
            var nodeIds = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id))
                {
                    issues.Add(new ValidationIssue { isError = true, message = "存在空节点 ID。" });
                    continue;
                }
                if (!nodeIds.Add(node.id))
                    issues.Add(new ValidationIssue { isError = true, message = $"重复节点 ID: {node.id}" });
            }

            if (string.IsNullOrEmpty(project.scenario.startNodeId))
                issues.Add(new ValidationIssue { isError = true, message = "未设置 startNodeId。" });
            else if (!nodeIds.Contains(project.scenario.startNodeId))
                issues.Add(new ValidationIssue { isError = true, message = $"startNodeId 不存在: {project.scenario.startNodeId}" });

            var npcIds = new HashSet<string>(npcs
                .Where(n => n != null && !string.IsNullOrWhiteSpace(n.id))
                .Select(n => n.id));
            var locIds = new HashSet<string>(locations
                .Where(l => l != null && !string.IsNullOrWhiteSpace(l.id))
                .Select(l => l.id));
            var itemIds = new HashSet<string>(items
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i.id))
                .Select(i => i.id));

            foreach (var node in nodes)
            {
                if (node == null) continue;
                ValidateNodeShape(issues, nodeIds, itemIds, locIds, node);

                CheckRef(issues, nodeIds, node.id, node.nextNodeId, "nextNodeId");
                CheckRef(issues, nodeIds, node.id, node.successNodeId, "successNodeId");
                CheckRef(issues, nodeIds, node.id, node.failureNodeId, "failureNodeId");
                CheckRef(issues, nodeIds, node.id, node.winNodeId, "winNodeId");
                CheckRef(issues, nodeIds, node.id, node.loseNodeId, "loseNodeId");
                CheckRef(issues, nodeIds, node.id, node.fleeNodeId, "fleeNodeId");

                if (node.choices != null)
                {
                    foreach (var choice in node.choices)
                    {
                        if (choice == null) continue;
                        if (string.IsNullOrWhiteSpace(choice.nextNodeId))
                            AddError(issues, $"节点 {node.id} 存在缺少 nextNodeId 的选项。");
                        else
                            CheckRef(issues, nodeIds, node.id, choice.nextNodeId, "choice.nextNodeId");

                        if (!string.IsNullOrWhiteSpace(choice.requiredItemId) &&
                            !itemIds.Contains(choice.requiredItemId))
                        {
                            AddError(issues,
                                $"节点 {node.id} 的选项引用未知物品: {choice.requiredItemId}");
                        }
                    }
                }
            }

            foreach (var npc in npcs)
            {
                if (npc == null || string.IsNullOrEmpty(npc.id)) continue;
                if (string.IsNullOrWhiteSpace(npc.defaultNodeId))
                    AddError(issues, $"NPC {npc.id} 缺少 defaultNodeId。");
                else if (!nodeIds.Contains(npc.defaultNodeId))
                    AddError(issues, $"NPC {npc.id} 的 defaultNodeId 不存在: {npc.defaultNodeId}");

                if (npc.locationIds != null)
                {
                    foreach (var locId in npc.locationIds)
                    {
                        if (!string.IsNullOrEmpty(locId) && !locIds.Contains(locId))
                            AddError(issues, $"NPC {npc.id} 引用未知地点: {locId}");
                    }
                }

                if (npc.schedules != null)
                {
                    foreach (var schedule in npc.schedules)
                    {
                        if (schedule == null || string.IsNullOrWhiteSpace(schedule.locationId))
                            AddError(issues, $"NPC {npc.id} 的日程缺少 locationId。");
                        else if (!locIds.Contains(schedule.locationId))
                            AddError(issues,
                                $"NPC {npc.id} 的日程引用未知地点: {schedule.locationId}");
                    }
                }
            }

            foreach (var location in locations)
            {
                if (!string.IsNullOrWhiteSpace(location?.requiredItemId) &&
                    !itemIds.Contains(location.requiredItemId))
                {
                    AddError(issues,
                        $"地点 {location.id} 的进入条件引用未知物品: {location.requiredItemId}");
                }

                if (!string.IsNullOrWhiteSpace(location?.requiredPeriod) &&
                    !SupportedPeriods.Contains(location.requiredPeriod))
                {
                    AddError(issues,
                        $"地点 {location.id} 使用未知时段: {location.requiredPeriod}");
                }

                if (location?.npcIds == null) continue;
                foreach (var npcId in location.npcIds)
                {
                    if (!string.IsNullOrWhiteSpace(npcId) && !npcIds.Contains(npcId))
                        AddError(issues, $"地点 {location.id} 引用未知 NPC: {npcId}");
                }
            }

            foreach (var rel in relationships)
            {
                if (rel == null) continue;
                if (!npcIds.Contains(rel.fromNpcId))
                    AddError(issues, $"关系 {rel.id} fromNpcId 不存在: {rel.fromNpcId}");
                if (!npcIds.Contains(rel.toNpcId))
                    AddError(issues, $"关系 {rel.id} toNpcId 不存在: {rel.toNpcId}");
            }

            ValidatePortraits(issues, nodes, npcs, portraitExists);

            var referenced = CollectReferencedIds(nodes);
            foreach (var npc in npcs)
                AddRef(referenced, npc?.defaultNodeId);
            foreach (var item in items)
                AddRef(referenced, item?.useNodeId);
            foreach (var id in nodeIds)
            {
                if (id != project.scenario.startNodeId && !referenced.Contains(id))
                    AddWarning(issues, $"孤立节点（无入边）: {id}");
            }

            return issues;
        }

        static void ValidateNodeShape(
            List<ValidationIssue> issues,
            HashSet<string> nodeIds,
            HashSet<string> itemIds,
            HashSet<string> locIds,
            StoryNodeData node)
        {
            var type = string.IsNullOrWhiteSpace(node.type)
                ? "dialogue"
                : node.type.Trim().ToLowerInvariant();
            if (!SupportedNodeTypes.Contains(type))
            {
                AddError(issues, $"节点 {node.id} 使用不支持的 type: {node.type}");
                return;
            }

            switch (type)
            {
                case "dialogue":
                case "npchub":
                    RequireOrdinaryExit(issues, node);
                    break;

                case "location":
                    if (!node.allowExploration)
                        RequireOrdinaryExit(issues, node);
                    break;

                case "setflag":
                case "changesan":
                case "advancetime":
                    RequireNext(issues, node);
                    break;

                case "giveitem":
                    RequireNext(issues, node);
                    if (string.IsNullOrWhiteSpace(node.itemId))
                        AddError(issues, $"giveitem 节点 {node.id} 缺少 itemId。");
                    else if (!itemIds.Contains(node.itemId))
                        AddError(issues, $"节点 {node.id} 引用未知物品: {node.itemId}");
                    break;

                case "check":
                    if (string.IsNullOrWhiteSpace(node.skillId))
                        AddError(issues, $"check 节点 {node.id} 缺少 skillId。");
                    RequireNodeTarget(issues, node.id, node.successNodeId, "successNodeId");
                    RequireNodeTarget(issues, node.id, node.failureNodeId, "failureNodeId");
                    break;

                case "combat":
                    if (string.IsNullOrWhiteSpace(node.combatId))
                        AddError(issues, $"combat 节点 {node.id} 缺少 combatId。");
                    else if (CombatEncounterDatabase.Get(node.combatId) == null)
                        AddError(issues, $"combat 节点 {node.id} 引用未知战斗: {node.combatId}");
                    RequireNodeTarget(issues, node.id, node.winNodeId, "winNodeId");
                    RequireNodeTarget(issues, node.id, node.loseNodeId, "loseNodeId");
                    RequireNodeTarget(issues, node.id, node.fleeNodeId, "fleeNodeId");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(node.locationId) && !locIds.Contains(node.locationId))
                AddError(issues, $"节点 {node.id} 引用未知地点: {node.locationId}");
        }

        static void RequireOrdinaryExit(List<ValidationIssue> issues, StoryNodeData node)
        {
            if (!string.IsNullOrWhiteSpace(node.nextNodeId) ||
                node.choices != null && node.choices.Any(c => c != null))
                return;
            AddError(issues, $"节点 {node.id} 缺少 nextNodeId 或选项出口。");
        }

        static void RequireNext(List<ValidationIssue> issues, StoryNodeData node)
        {
            if (string.IsNullOrWhiteSpace(node.nextNodeId))
                AddError(issues, $"{node.type ?? "dialogue"} 节点 {node.id} 缺少 nextNodeId。");
        }

        static void RequireNodeTarget(
            List<ValidationIssue> issues,
            string fromId,
            string targetId,
            string field)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                AddError(issues, $"节点 {fromId} 缺少 {field}。");
            }
        }

        static void ValidatePortraits(
            List<ValidationIssue> issues,
            IEnumerable<StoryNodeData> nodes,
            IEnumerable<NPCDefinition> npcs,
            Func<string, bool> portraitExists)
        {
            portraitExists ??= id => PortraitDatabase.Get(id) != null;
            foreach (var npc in npcs)
            {
                if (npc == null || string.IsNullOrWhiteSpace(npc.portraitId)) continue;
                if (!portraitExists(npc.portraitId))
                    AddWarning(issues, $"NPC {npc.id} 的头像资源不存在: {npc.portraitId}");
            }

            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.portraitId)) continue;
                if (!portraitExists(node.portraitId))
                    AddWarning(issues, $"节点 {node.id} 的头像资源不存在: {node.portraitId}");
            }
        }

        static void CheckRef(List<ValidationIssue> issues, HashSet<string> nodeIds, string fromId, string targetId, string field)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            if (!nodeIds.Contains(targetId))
                AddError(issues, $"节点 {fromId} 的 {field} 指向不存在: {targetId}");
        }

        static void AddError(List<ValidationIssue> issues, string message) =>
            issues.Add(new ValidationIssue { isError = true, message = message });

        static void AddWarning(List<ValidationIssue> issues, string message) =>
            issues.Add(new ValidationIssue { isError = false, message = message });

        static HashSet<string> CollectReferencedIds(List<StoryNodeData> nodes)
        {
            var refs = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (node == null) continue;
                AddRef(refs, node.nextNodeId);
                AddRef(refs, node.successNodeId);
                AddRef(refs, node.failureNodeId);
                AddRef(refs, node.winNodeId);
                AddRef(refs, node.loseNodeId);
                AddRef(refs, node.fleeNodeId);
                if (node.choices == null) continue;
                foreach (var c in node.choices)
                    AddRef(refs, c?.nextNodeId);
            }
            return refs;
        }

        static void AddRef(HashSet<string> set, string id)
        {
            if (!string.IsNullOrEmpty(id)) set.Add(id);
        }

        public static string FormatIssues(List<ValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0) return "验证通过，无问题。";
            var sb = new StringBuilder();
            foreach (var issue in issues)
                sb.Append(issue.isError ? "[错误] " : "[警告] ").AppendLine(issue.message);
            return sb.ToString();
        }
    }
}
