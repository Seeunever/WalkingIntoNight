using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace WalkingIntoNight.TRPG.Core
{
    public sealed class DevelopmentBuildSmokeTest : MonoBehaviour
    {
        const string SuccessMarker = "WALKING_INTO_NIGHT_BUILD_SMOKE_PASS";
        const string IdentityMigrationSuccessMarker =
            "WALKING_INTO_NIGHT_IDENTITY_MIGRATION_SMOKE_PASS";

        static string s_identityPreparationError;
        static string s_identityPhase;
        static string s_expectedCurrentJson;
        static string s_expectedMarkerContents;

        bool m_runIdentityMigrationSmoke;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetIdentityMigrationSmokeState()
        {
            s_identityPreparationError = null;
            s_identityPhase = null;
            s_expectedCurrentJson = null;
            s_expectedMarkerContents = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void PrepareIdentityMigrationSmoke()
        {
            if (!IdentityMigrationSmokeEnvironment.IsRequested()) return;

            try
            {
                s_identityPhase = IdentityMigrationSmokeEnvironment.GetPhaseOrThrow();
                var currentRoot =
                    IdentityMigrationSmokeEnvironment.GetCurrentRootOrThrow();
                var legacyRoot =
                    IdentityMigrationSmokeEnvironment.GetLegacyRootOrThrow();
                var currentPath = Path.Combine(currentRoot, "trpg_save_1.json");
                var legacyPath = Path.Combine(legacyRoot, "trpg_save_1.json");
                var markerPath = Path.Combine(
                    currentRoot,
                    ProductIdentity.IdentityMigrationMarkerName);

                if (s_identityPhase == "first")
                {
                    Require(!File.Exists(currentPath),
                        "first 阶段开始前新路径存档必须不存在。");
                    Require(!File.Exists(markerPath),
                        "first 阶段开始前迁移标记必须不存在。");
                    Require(!File.Exists(legacyPath),
                        "first 阶段必须使用全新的烟测目录。");

                    Directory.CreateDirectory(legacyRoot);
                    s_expectedCurrentJson = JsonUtility.ToJson(
                        CreateIdentitySmokeSave("SP1D_FIRST", 100),
                        true);
                    File.WriteAllText(legacyPath, s_expectedCurrentJson);
                }
                else
                {
                    Require(File.Exists(currentPath),
                        "second 阶段缺少 first 阶段迁移出的新路径存档。");
                    Require(File.Exists(markerPath),
                        "second 阶段缺少 first 阶段迁移标记。");
                    Require(File.Exists(legacyPath),
                        "second 阶段缺少旧路径存档。");

                    s_expectedCurrentJson = File.ReadAllText(currentPath);
                    s_expectedMarkerContents = File.ReadAllText(markerPath);
                    File.WriteAllText(
                        legacyPath,
                        JsonUtility.ToJson(
                            CreateIdentitySmokeSave("SP1D_SECOND", 999),
                            true));
                }
            }
            catch (Exception exception)
            {
                s_identityPreparationError = exception.ToString();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            if (!Debug.isDebugBuild) return;

            var runGameplaySmoke = DevelopmentSmokeEnvironment.IsRequested();
            var runIdentityMigrationSmoke =
                IdentityMigrationSmokeEnvironment.IsRequested();
            if (!runGameplaySmoke && !runIdentityMigrationSmoke) return;

            var go = new GameObject(nameof(DevelopmentBuildSmokeTest));
            DontDestroyOnLoad(go);
            go.AddComponent<DevelopmentBuildSmokeTest>()
                .m_runIdentityMigrationSmoke = runIdentityMigrationSmoke;
        }

        IEnumerator Start()
        {
            yield return null;
            var exitCode = 0;
            try
            {
                if (m_runIdentityMigrationSmoke)
                {
                    RunIdentityMigrationSmokeTest();
                    Debug.Log(IdentityMigrationSuccessMarker + ":" +
                        s_identityPhase);
                }
                else
                {
                    RunSmokeTest();
                    Debug.Log(SuccessMarker);
                }
            }
            catch (Exception exception)
            {
                var failureMarker = m_runIdentityMigrationSmoke
                    ? "WALKING_INTO_NIGHT_IDENTITY_MIGRATION_SMOKE_FAIL"
                    : "WALKING_INTO_NIGHT_BUILD_SMOKE_FAIL";
                Debug.LogError($"{failureMarker}: {exception}");
                exitCode = 1;
            }
            yield return null;
            Application.Quit(exitCode);
        }

        static void RunSmokeTest()
        {
            RequireProductIdentity();

            var state = GameStateManager.Instance;
            Require(state != null, "GameStateManager 未初始化。");
            state.ResetForNewGame();
            Require(state.Investigator == null && state.Inventory.Items.Count == 0,
                "新游戏重置失败。");

            state.SetInvestigator(new Investigator
            {
                Name = "Windows 构建烟测调查员",
                HP = 12,
                MaxHP = 12,
                SAN = 50,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            });
            state.Inventory.AddItem("notebook");
            state.CurrentScenarioId = ScenarioRegistry.DefaultScenarioId;

            var runner = new ScenarioRunner(state);
            runner.LoadScenario(state.CurrentScenarioId);
            runner.AdvanceTo("spot_success");
            runner.AdvanceTo("set_found_coin");
            Require(state.Inventory.HasItem("silver_coin") && state.HasFlag("found_coin"),
                "调查物品或旗标没有正确写入状态。");
            runner.AdvanceTo("hub_explore");

            Require(SaveSystem.TrySave(1, state.ToSaveData(), out var saveError),
                saveError ?? "保存失败。");
            state.ResetForNewGame();
            Require(SaveSystem.TryLoad(1, out var loaded, out var loadError),
                loadError ?? "读取失败。");
            Require(GameStateManager.TryValidateSaveData(loaded, out var validationError),
                validationError ?? "存档校验失败。");
            state.LoadFromSaveData(loaded);
            Require(state.CurrentNodeId == "hub_explore" &&
                state.Inventory.HasItem("silver_coin") && state.HasFlag("found_coin"),
                "继续游戏没有恢复节点、物品和旗标。");

            var ended = false;
            runner = new ScenarioRunner(state);
            runner.LoadScenario(state.CurrentScenarioId);
            runner.OnScenarioEnded = () => ended = true;
            runner.AdvanceTo("end_neutral");
            Require(ended && runner.InteractionMode == ScenarioInteractionMode.End,
                "构建内结局流程没有结束。");
            Require(SaveSystem.Delete(1), "烟测存档清理失败。");
        }

        static void RunIdentityMigrationSmokeTest()
        {
            Require(string.IsNullOrWhiteSpace(s_identityPreparationError),
                "身份迁移烟测准备失败：" + s_identityPreparationError);
            RequireProductIdentity();

            var currentRoot =
                IdentityMigrationSmokeEnvironment.GetCurrentRootOrThrow();
            var legacyRoot =
                IdentityMigrationSmokeEnvironment.GetLegacyRootOrThrow();
            var legacyPath = Path.Combine(legacyRoot, "trpg_save_1.json");
            var currentPath = Path.Combine(currentRoot, "trpg_save_1.json");
            var markerPath = Path.Combine(
                currentRoot,
                ProductIdentity.IdentityMigrationMarkerName);

            Require(File.Exists(legacyPath), "旧路径烟测存档不存在。");
            Require(File.Exists(currentPath), "首次启动未生成新路径存档。");
            Require(File.Exists(markerPath), "首次启动未生成身份迁移完成标记。");
            var currentJson = File.ReadAllText(currentPath);
            var legacyJson = File.ReadAllText(legacyPath);
            var markerContents = File.ReadAllText(markerPath);

            Require(currentJson == s_expectedCurrentJson,
                $"{s_identityPhase} 阶段的新路径存档发生意外变化。");
            if (s_identityPhase == "first")
            {
                Require(legacyJson == s_expectedCurrentJson,
                    "first 阶段修改了旧路径源文件。");
            }
            else
            {
                Require(markerContents == s_expectedMarkerContents,
                    "second 阶段重复改写了迁移完成标记。");
                var secondLegacy = JsonUtility.FromJson<GameSaveData>(legacyJson);
                Require(secondLegacy?.investigator?.name == "SP1D_SECOND",
                    "second 阶段没有正确模拟旧构建写入更新存档。");
            }

            Require(SaveSystem.TryLoad(1, out var loaded, out var loadError),
                loadError ?? "迁移后的存档无法读取。");
            Require(GameStateManager.TryValidateSaveData(
                    loaded,
                    out var validationError),
                validationError ?? "迁移后的存档无法通过语义校验。");
            Require(
                loaded.investigator?.name == "SP1D_FIRST",
                $"迁移后调查员不符：{loaded.investigator?.name ?? "（空）"}。" +
                "期待：SP1D_FIRST");

            Button continueButton = null;
            foreach (var button in FindObjectsOfType<Button>())
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null || label.text != "继续") continue;
                continueButton = button;
                break;
            }

            Require(continueButton != null, "主菜单未生成继续按钮。");
            Require(continueButton.interactable,
                "迁移完成后主菜单继续按钮仍不可用。");
        }

        static GameSaveData CreateIdentitySmokeSave(
            string investigatorName,
            long savedAtTicks)
        {
            return new GameSaveData
            {
                version = GameSaveData.CurrentVersion,
                scenarioId = ScenarioRegistry.DefaultScenarioId,
                nodeId = "hub_explore",
                locationId = "cafe_main",
                flags = new List<string> { "found_coin" },
                investigator = new InvestigatorData
                {
                    name = investigatorName,
                    STR = 50,
                    CON = 50,
                    POW = 50,
                    DEX = 50,
                    APP = 50,
                    INT = 60,
                    EDU = 60,
                    SIZ = 50,
                    HP = 10,
                    MaxHP = 10,
                    SAN = 50,
                    MaxSAN = 50,
                    MP = 10,
                    MaxMP = 10,
                    skills = new List<SkillEntry>
                    {
                        new SkillEntry { id = "spot_hidden", value = 65 }
                    }
                },
                inventoryItemIds = new List<string> { "notebook" },
                savedAtTicks = savedAtTicks,
                currentDay = 1,
                currentPeriod = "night"
            };
        }

        static void RequireProductIdentity()
        {
            Require(Application.companyName == ProductIdentity.CompanyName,
                $"运行时 companyName 不符：{Application.companyName}。");
            Require(Application.productName == ProductIdentity.ProductName,
                $"运行时 productName 不符：{Application.productName}。");
            Require(Application.version == ProductIdentity.ProductVersion,
                $"运行时 version 不符：{Application.version}。");
            // Unity/Tuanjie only defines Application.identifier at runtime for
            // Apple, Android and UWP. Standalone Win64 returns an empty string,
            // so its identifier is enforced by ProjectIdentityValidator before build.
            if (!string.IsNullOrEmpty(Application.identifier))
            {
                Require(Application.identifier == ProductIdentity.ApplicationIdentifier,
                    $"运行时 identifier 不符：{Application.identifier}。");
            }

            var expectedSuffix = Path.Combine(
                ProductIdentity.CompanyName,
                ProductIdentity.ProductName);
            var normalizedPersistentPath = Path.GetFullPath(
                Application.persistentDataPath);
            Require(normalizedPersistentPath.EndsWith(
                    expectedSuffix,
                    StringComparison.OrdinalIgnoreCase),
                $"persistentDataPath 未使用正式身份：{normalizedPersistentPath}。");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
