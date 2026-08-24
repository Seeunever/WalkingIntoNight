using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class SaveSystemTests
    {
        string m_testRoot;
        string m_storageRoot;
        string m_legacyStorageRoot;

        [SetUp]
        public void SetUp()
        {
            m_testRoot = Path.Combine(
                Path.GetTempPath(),
                "WalkingIntoNight_SaveTests_" + Guid.NewGuid().ToString("N"));
            m_storageRoot = Path.Combine(m_testRoot, "Current");
            m_legacyStorageRoot = Path.Combine(m_testRoot, "Legacy");
            Directory.CreateDirectory(m_storageRoot);
            Directory.CreateDirectory(m_legacyStorageRoot);
            SaveSystem.SetStorageRootOverrideForTests(m_storageRoot);
            SaveSystem.SetLegacyStorageRootOverrideForTests(m_legacyStorageRoot);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.SetStorageRootOverrideForTests(null);
            SaveSystem.SetLegacyStorageRootOverrideForTests(null);
            if (Directory.Exists(m_testRoot))
                Directory.Delete(m_testRoot, true);
        }

        [Test]
        public void TrySaveAndTryLoad_RoundTripPreservesCompleteGameState()
        {
            var original = CreateValidSave();

            Assert.That(SaveSystem.TrySave(1, original, out var saveError), Is.True, saveError);
            Assert.That(SaveSystem.TryLoad(1, out var loaded, out var loadError), Is.True, loadError);

            Assert.That(loaded.version, Is.EqualTo(GameSaveData.CurrentVersion));
            Assert.That(loaded.scenarioId, Is.EqualTo(original.scenarioId));
            Assert.That(loaded.nodeId, Is.EqualTo(original.nodeId));
            Assert.That(loaded.locationId, Is.EqualTo(original.locationId));
            Assert.That(loaded.currentDay, Is.EqualTo(original.currentDay));
            Assert.That(loaded.currentPeriod, Is.EqualTo(original.currentPeriod));
            Assert.That(loaded.flags, Is.EquivalentTo(original.flags));
            Assert.That(loaded.inventoryItemIds, Is.EqualTo(original.inventoryItemIds));
            Assert.That(loaded.investigator.name, Is.EqualTo(original.investigator.name));
            Assert.That(loaded.investigator.HP, Is.EqualTo(original.investigator.HP));
            Assert.That(loaded.investigator.SAN, Is.EqualTo(original.investigator.SAN));
            Assert.That(loaded.investigator.MP, Is.EqualTo(original.investigator.MP));
            Assert.That(loaded.investigator.skills.Count, Is.EqualTo(1));
            Assert.That(loaded.savedAtTicks, Is.GreaterThan(0));
        }

        [Test]
        public void TryLoad_LegacySaveWithMissingLists_GetsSafeDefaults()
        {
            const string legacyJson =
                "{\"scenarioId\":\"Scenario_01\",\"nodeId\":\"hub_explore\"," +
                "\"locationId\":\"cafe_main\",\"investigator\":{" +
                "\"name\":\"旧调查员\",\"HP\":7,\"MaxHP\":7," +
                "\"SAN\":40,\"MaxSAN\":45,\"MP\":9,\"MaxMP\":9}}";
            File.WriteAllText(GetSlotPath(1), legacyJson);

            Assert.That(SaveSystem.TryLoad(1, out var loaded, out var error), Is.True, error);

            Assert.That(loaded.version, Is.EqualTo(GameSaveData.CurrentVersion));
            Assert.That(loaded.flags, Is.Not.Null.And.Empty);
            Assert.That(loaded.inventoryItemIds, Is.Not.Null.And.Empty);
            Assert.That(loaded.investigator.skills, Is.Not.Null.And.Empty);
            Assert.That(loaded.currentDay, Is.EqualTo(1));
            Assert.That(loaded.currentPeriod, Is.EqualTo("morning"));
        }

        [Test]
        public void TryLoad_BadJson_ReturnsFailureWithoutThrowing()
        {
            File.WriteAllText(GetSlotPath(1), "{ definitely not valid json");

            var succeeded = SaveSystem.TryLoad(1, out var loaded, out var error);

            Assert.That(succeeded, Is.False);
            Assert.That(loaded, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void TryLoad_FutureVersion_ReturnsReadableFailure()
        {
            File.WriteAllText(GetSlotPath(1), "{\"version\":999}");

            var succeeded = SaveSystem.TryLoad(1, out var loaded, out var error);

            Assert.That(succeeded, Is.False);
            Assert.That(loaded, Is.Null);
            Assert.That(error, Does.Contain("999"));
        }

        [Test]
        public void TryMigrateLegacySaves_LegacyOnlyCopiesValidSaveAndKeepsSource()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 100;
            legacy.investigator.name = "旧路径调查员";
            WriteSave(m_legacyStorageRoot, 1, legacy);
            var originalLegacyJson = File.ReadAllText(GetLegacySlotPath(1));

            var succeeded = SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error);

            Assert.That(succeeded, Is.True, error);
            Assert.That(report.LegacySlotsFound, Is.EqualTo(1));
            Assert.That(report.MigratedSlots, Is.EqualTo(1));
            Assert.That(report.BackupsCreated, Is.Zero);
            Assert.That(File.Exists(GetLegacySlotPath(1)), Is.True);
            Assert.That(
                File.ReadAllText(GetLegacySlotPath(1)),
                Is.EqualTo(originalLegacyJson));
            Assert.That(File.Exists(GetMigrationMarkerPath()), Is.True);
            Assert.That(SaveSystem.TryLoad(1, out var loaded, out var loadError),
                Is.True, loadError);
            Assert.That(loaded.investigator.name, Is.EqualTo("旧路径调查员"));
        }

        [Test]
        public void TryMigrateLegacySaves_NewerValidCurrentSaveIsNeverOverwritten()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 100;
            legacy.investigator.name = "旧路径较旧";
            WriteSave(m_legacyStorageRoot, 1, legacy);

            var current = CreateValidSave();
            current.savedAtTicks = 200;
            current.investigator.name = "新路径较新";
            WriteSave(m_storageRoot, 1, current);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.MigratedSlots, Is.Zero);
            Assert.That(report.PreservedCurrentSlots, Is.EqualTo(1));
            Assert.That(report.BackupsCreated, Is.Zero);
            Assert.That(SaveSystem.TryLoad(1, out var loaded, out var loadError),
                Is.True, loadError);
            Assert.That(loaded.investigator.name, Is.EqualTo("新路径较新"));
        }

        [Test]
        public void TryMigrateLegacySaves_NewerLegacyReplacesCurrentWithRollbackBackup()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 200;
            legacy.investigator.name = "旧路径较新";
            WriteSave(m_legacyStorageRoot, 1, legacy);

            var current = CreateValidSave();
            current.savedAtTicks = 100;
            current.investigator.name = "新路径较旧";
            WriteSave(m_storageRoot, 1, current);
            var originalCurrentJson = File.ReadAllText(GetSlotPath(1));

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var firstReport,
                out var firstError), Is.True, firstError);

            var backupPath = GetSlotPath(1) + ".before_identity_migration.bak";
            Assert.That(firstReport.MigratedSlots, Is.EqualTo(1));
            Assert.That(firstReport.BackupsCreated, Is.EqualTo(1));
            Assert.That(File.ReadAllText(backupPath), Is.EqualTo(originalCurrentJson));
            Assert.That(SaveSystem.TryLoad(1, out var loaded, out var loadError),
                Is.True, loadError);
            Assert.That(loaded.investigator.name, Is.EqualTo("旧路径较新"));

            var migratedJson = File.ReadAllText(GetSlotPath(1));
            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var secondReport,
                out var secondError), Is.True, secondError);
            Assert.That(secondReport.MigratedSlots, Is.Zero);
            Assert.That(secondReport.AlreadyCompleted, Is.True);
            Assert.That(secondReport.BackupsCreated, Is.Zero);
            Assert.That(File.ReadAllText(GetSlotPath(1)), Is.EqualTo(migratedJson));
            Assert.That(File.ReadAllText(backupPath), Is.EqualTo(originalCurrentJson));
        }

        [Test]
        public void TryMigrateLegacySaves_InvalidLegacyIsSkippedAndPreserved()
        {
            const string invalidJson = "{ definitely not valid json";
            File.WriteAllText(GetLegacySlotPath(1), invalidJson);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.LegacySlotsFound, Is.EqualTo(1));
            Assert.That(report.InvalidLegacySlots, Is.EqualTo(1));
            Assert.That(report.MigratedSlots, Is.Zero);
            Assert.That(File.Exists(GetSlotPath(1)), Is.False);
            Assert.That(File.ReadAllText(GetLegacySlotPath(1)), Is.EqualTo(invalidJson));
            Assert.That(File.Exists(GetMigrationMarkerPath()), Is.False);
        }

        [Test]
        public void TryMigrateLegacySaves_ValidLegacyReplacesInvalidCurrentAndBacksItUp()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 100;
            WriteSave(m_legacyStorageRoot, 1, legacy);
            const string invalidCurrent = "broken current save";
            File.WriteAllText(GetSlotPath(1), invalidCurrent);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.MigratedSlots, Is.EqualTo(1));
            Assert.That(report.BackupsCreated, Is.EqualTo(1));
            Assert.That(
                File.ReadAllText(GetSlotPath(1) + ".before_identity_migration.bak"),
                Is.EqualTo(invalidCurrent));
            Assert.That(SaveSystem.TryLoad(1, out _, out var loadError),
                Is.True, loadError);
        }

        [Test]
        public void TryMigrateLegacySaves_SemanticallyInvalidLegacyCanBeRetried()
        {
            var invalidLegacy = CreateValidSave();
            invalidLegacy.scenarioId = "missing_scenario";
            WriteSave(m_legacyStorageRoot, 1, invalidLegacy);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var firstReport,
                out var firstError), Is.True, firstError);
            Assert.That(firstReport.InvalidLegacySlots, Is.EqualTo(1));
            Assert.That(File.Exists(GetMigrationMarkerPath()), Is.False);

            var repairedLegacy = CreateValidSave();
            repairedLegacy.investigator.name = "修复后的旧档";
            WriteSave(m_legacyStorageRoot, 1, repairedLegacy);
            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var secondReport,
                out var secondError), Is.True, secondError);
            Assert.That(secondReport.MigratedSlots, Is.EqualTo(1));
            Assert.That(File.Exists(GetMigrationMarkerPath()), Is.True);
        }

        [Test]
        public void TryMigrateLegacySaves_InvalidLegacyWithValidCurrentLocksCurrentChoice()
        {
            var invalidLegacy = CreateValidSave();
            invalidLegacy.scenarioId = "missing_scenario";
            WriteSave(m_legacyStorageRoot, 1, invalidLegacy);
            var current = CreateValidSave();
            current.savedAtTicks = 100;
            current.investigator.name = "已经在玩的新档";
            WriteSave(m_storageRoot, 1, current);
            var currentJson = File.ReadAllText(GetSlotPath(1));

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var firstReport,
                out var firstError), Is.True, firstError);
            Assert.That(firstReport.InvalidLegacySlots, Is.EqualTo(1));
            Assert.That(firstReport.PreservedCurrentSlots, Is.EqualTo(1));
            Assert.That(File.Exists(GetMigrationMarkerPath()), Is.True);

            var repairedLegacy = CreateValidSave();
            repairedLegacy.savedAtTicks = 999;
            repairedLegacy.investigator.name = "后来修好的旧档";
            WriteSave(m_legacyStorageRoot, 1, repairedLegacy);
            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var secondReport,
                out var secondError), Is.True, secondError);
            Assert.That(secondReport.AlreadyCompleted, Is.True);
            Assert.That(File.ReadAllText(GetSlotPath(1)), Is.EqualTo(currentJson));
        }

        [Test]
        public void TryMigrateLegacySaves_FutureVersionCurrentSaveIsPreservedExactly()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 500;
            WriteSave(m_legacyStorageRoot, 1, legacy);
            const string futureJson = "{\"version\":999,\"futureField\":\"keep me\"}";
            File.WriteAllText(GetSlotPath(1), futureJson);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.PreservedCurrentSlots, Is.EqualTo(1));
            Assert.That(report.MigratedSlots, Is.Zero);
            Assert.That(File.ReadAllText(GetSlotPath(1)), Is.EqualTo(futureJson));
            Assert.That(File.Exists(GetMigrationMarkerPath()), Is.True);
        }

        [Test]
        public void TryMigrateLegacySaves_ZeroTimestampsPreferCurrentDestination()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 0;
            legacy.investigator.name = "旧路径";
            WriteSave(m_legacyStorageRoot, 1, legacy);
            var current = CreateValidSave();
            current.savedAtTicks = 0;
            current.investigator.name = "新路径";
            WriteSave(m_storageRoot, 1, current);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.PreservedCurrentSlots, Is.EqualTo(1));
            Assert.That(SaveSystem.TryLoad(1, out var loaded, out var loadError),
                Is.True, loadError);
            Assert.That(loaded.investigator.name, Is.EqualTo("新路径"));
        }

        [Test]
        public void TryMigrateLegacySaves_MissingCurrentTimestampStillPrefersCurrent()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 999;
            legacy.investigator.name = "有时间戳的旧路径";
            WriteSave(m_legacyStorageRoot, 1, legacy);
            var current = CreateValidSave();
            current.savedAtTicks = 0;
            current.investigator.name = "无时间戳的新路径";
            WriteSave(m_storageRoot, 1, current);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.PreservedCurrentSlots, Is.EqualTo(1));
            Assert.That(SaveSystem.TryLoad(1, out var loaded, out var loadError),
                Is.True, loadError);
            Assert.That(loaded.investigator.name, Is.EqualTo("无时间戳的新路径"));
        }

        [Test]
        public void TryMigrateLegacySaves_ExistingBackupGetsAUniqueAdditionalBackup()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 200;
            WriteSave(m_legacyStorageRoot, 1, legacy);
            var current = CreateValidSave();
            current.savedAtTicks = 100;
            current.investigator.name = "需要第二份回退";
            WriteSave(m_storageRoot, 1, current);
            var currentJson = File.ReadAllText(GetSlotPath(1));
            var baseBackupPath = GetSlotPath(1) + ".before_identity_migration.bak";
            File.WriteAllText(baseBackupPath, "existing backup");

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.BackupsCreated, Is.EqualTo(1));
            Assert.That(File.ReadAllText(baseBackupPath), Is.EqualTo("existing backup"));
            Assert.That(
                File.ReadAllText(baseBackupPath + ".1"),
                Is.EqualTo(currentJson));
        }

        [Test]
        public void TryMigrateLegacySaves_MarkerPreventsSecondMigrationOrResurrection()
        {
            var legacy = CreateValidSave();
            legacy.savedAtTicks = 100;
            legacy.investigator.name = "第一次迁移";
            WriteSave(m_legacyStorageRoot, 1, legacy);
            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var firstReport,
                out var firstError), Is.True, firstError);
            Assert.That(firstReport.MigratedSlots, Is.EqualTo(1));
            var firstMigratedJson = File.ReadAllText(GetSlotPath(1));

            legacy.savedAtTicks = 999;
            legacy.investigator.name = "旧构建后来写入";
            WriteSave(m_legacyStorageRoot, 1, legacy);
            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var secondReport,
                out var secondError), Is.True, secondError);
            Assert.That(secondReport.AlreadyCompleted, Is.True);
            Assert.That(File.ReadAllText(GetSlotPath(1)), Is.EqualTo(firstMigratedJson));

            File.Delete(GetSlotPath(1));
            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var thirdReport,
                out var thirdError), Is.True, thirdError);
            Assert.That(thirdReport.AlreadyCompleted, Is.True);
            Assert.That(File.Exists(GetSlotPath(1)), Is.False,
                "删除新路径存档后，旧路径存档不得复活。");
        }

        [Test]
        public void TryMigrateLegacySaves_DoesNotTouchUnsupportedSlots()
        {
            const string slotTwoSentinel = "slot 2 must stay byte-for-byte unchanged";
            File.WriteAllText(GetLegacySlotPath(2), slotTwoSentinel);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.LegacySlotsFound, Is.Zero);
            Assert.That(File.ReadAllText(GetLegacySlotPath(2)),
                Is.EqualTo(slotTwoSentinel));
            Assert.That(File.Exists(GetSlotPath(2)), Is.False);
        }

        [Test]
        public void TryMigrateLegacySaves_SameRootIsANoOp()
        {
            var current = CreateValidSave();
            current.investigator.name = "同一路径";
            WriteSave(m_storageRoot, 1, current);
            var originalJson = File.ReadAllText(GetSlotPath(1));
            SaveSystem.SetLegacyStorageRootOverrideForTests(m_storageRoot);

            Assert.That(SaveSystem.TryMigrateLegacySaves(
                out var report,
                out var error), Is.True, error);

            Assert.That(report.LegacySlotsFound, Is.Zero);
            Assert.That(File.ReadAllText(GetSlotPath(1)), Is.EqualTo(originalJson));
            Assert.That(File.Exists(GetMigrationMarkerPath()), Is.False);
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(-1)]
        public void SaveApis_InvalidSlot_ReturnFailure(int slot)
        {
            Assert.That(SaveSystem.IsValidSlot(slot), Is.False);
            Assert.That(SaveSystem.HasSave(slot), Is.False);
            Assert.That(SaveSystem.TrySave(slot, CreateValidSave(), out var saveError), Is.False);
            Assert.That(saveError, Is.Not.Empty);
            Assert.That(SaveSystem.TryLoad(slot, out var loaded, out var loadError), Is.False);
            Assert.That(loaded, Is.Null);
            Assert.That(loadError, Is.Not.Empty);
        }

        [Test]
        public void TryValidateSaveData_NormalizesLegacyCollectionsAndTime()
        {
            var data = CreateValidSave();
            data.version = 0;
            data.flags = null;
            data.inventoryItemIds = null;
            data.investigator.skills = null;
            data.currentDay = 0;
            data.currentPeriod = "unknown";

            Assert.That(GameStateManager.TryValidateSaveData(data, out var error), Is.True, error);
            Assert.That(data.version, Is.EqualTo(GameSaveData.CurrentVersion));
            Assert.That(data.flags, Is.Not.Null.And.Empty);
            Assert.That(data.inventoryItemIds, Is.Not.Null.And.Empty);
            Assert.That(data.investigator.skills, Is.Not.Null.And.Empty);
            Assert.That(data.currentDay, Is.EqualTo(1));
            Assert.That(data.currentPeriod, Is.EqualTo("morning"));
        }

        [Test]
        public void TryValidateSaveData_RejectsUnknownReferencesAndMissingInvestigator()
        {
            var unknownScenario = CreateValidSave();
            unknownScenario.scenarioId = "missing";
            Assert.That(GameStateManager.TryValidateSaveData(unknownScenario, out _), Is.False);

            var unknownNode = CreateValidSave();
            unknownNode.nodeId = "missing";
            Assert.That(GameStateManager.TryValidateSaveData(unknownNode, out _), Is.False);

            var unknownLocation = CreateValidSave();
            unknownLocation.locationId = "missing";
            Assert.That(GameStateManager.TryValidateSaveData(unknownLocation, out _), Is.False);

            var missingInvestigator = CreateValidSave();
            missingInvestigator.investigator = null;
            Assert.That(GameStateManager.TryValidateSaveData(missingInvestigator, out _), Is.False);
        }

        string GetSlotPath(int slot)
        {
            return Path.Combine(m_storageRoot, $"trpg_save_{slot}.json");
        }

        string GetLegacySlotPath(int slot)
        {
            return Path.Combine(m_legacyStorageRoot, $"trpg_save_{slot}.json");
        }

        string GetMigrationMarkerPath()
        {
            return Path.Combine(
                m_storageRoot,
                ".identity_migration_defaultcompany_v1.done");
        }

        static void WriteSave(string root, int slot, GameSaveData data)
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(
                Path.Combine(root, $"trpg_save_{slot}.json"),
                JsonUtility.ToJson(data, true));
        }

        static GameSaveData CreateValidSave()
        {
            return new GameSaveData
            {
                version = GameSaveData.CurrentVersion,
                scenarioId = ScenarioRegistry.DefaultScenarioId,
                nodeId = "hub_explore",
                locationId = "cafe_storage",
                currentDay = 3,
                currentPeriod = "night",
                flags = new List<string> { "found_coin", "mei_trust" },
                inventoryItemIds = new List<string> { "rusty_key", "owner_diary" },
                investigator = new InvestigatorData
                {
                    name = "测试调查员",
                    STR = 50,
                    CON = 50,
                    POW = 45,
                    DEX = 55,
                    APP = 50,
                    INT = 60,
                    EDU = 65,
                    SIZ = 50,
                    HP = 7,
                    MaxHP = 10,
                    SAN = 40,
                    MaxSAN = 45,
                    MP = 8,
                    MaxMP = 9,
                    skills = new List<SkillEntry>
                    {
                        new SkillEntry { id = "spot_hidden", value = 65 }
                    }
                }
            };
        }
    }
}
