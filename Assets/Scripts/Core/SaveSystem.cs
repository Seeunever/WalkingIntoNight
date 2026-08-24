using System;
using System.Collections.Generic;
using System.IO;
using WalkingIntoNight.TRPG.Character;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Core
{
    public sealed class SaveMigrationReport
    {
        public int LegacySlotsFound { get; internal set; }
        public int MigratedSlots { get; internal set; }
        public int PreservedCurrentSlots { get; internal set; }
        public int InvalidLegacySlots { get; internal set; }
        public int BackupsCreated { get; internal set; }
        public bool AlreadyCompleted { get; internal set; }
    }

    public static class SaveSystem
    {
        const int SlotCount = 3;
        const string EditorTestRootEnvironmentVariable = "WALKING_INTO_NIGHT_TEST_SAVE_ROOT";
        const string EditorTestSentinelFileName =
            ".walking_into_night_editor_tests";
        const string IdentityMigrationBackupSuffix = ".before_identity_migration.bak";
        const int IdentityMigrationSlot = 1;

#if UNITY_EDITOR
        static string s_testStorageRootOverride;
        static string s_testLegacyStorageRootOverride;
#endif

        public static int SlotCountMax => SlotCount;

        public static bool IsValidSlot(int slot)
        {
            return slot >= 1 && slot <= SlotCount;
        }

        public static bool HasSave(int slot)
        {
            if (!IsValidSlot(slot)) return false;

            try
            {
                return File.Exists(GetPath(slot));
            }
            catch
            {
                return false;
            }
        }

        public static bool TrySave(int slot, GameSaveData data, out string error)
        {
            error = null;
            if (!IsValidSlot(slot))
            {
                error = $"无效存档槽位：{slot}。";
                return false;
            }

            if (data == null)
            {
                error = "没有可保存的游戏数据。";
                return false;
            }

            string temporaryPath = null;
            try
            {
                data.version = GameSaveData.CurrentVersion;
                data.savedAtTicks = DateTime.UtcNow.Ticks;

                var json = JsonUtility.ToJson(data, true);
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "生成存档数据失败。";
                    return false;
                }

                var root = GetStorageRoot();
                Directory.CreateDirectory(root);
                var path = GetPath(slot);
                temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(path))
                    ReplaceExistingFile(temporaryPath, path);
                else
                    File.Move(temporaryPath, path);

                temporaryPath = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"保存失败：{GetSafeMessage(ex)}";
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporaryPath))
                    TryDeleteTemporaryFile(temporaryPath);
            }
        }

        public static bool TryLoad(int slot, out GameSaveData data, out string error)
        {
            data = null;
            error = null;
            if (!IsValidSlot(slot))
            {
                error = $"无效存档槽位：{slot}。";
                return false;
            }

            try
            {
                return TryReadSaveFile(GetPath(slot), out data, out error);
            }
            catch (Exception ex)
            {
                data = null;
                error = $"读取存档失败：{GetSafeMessage(ex)}";
                return false;
            }
        }

        public static bool Save(int slot, GameSaveData data)
        {
            return TrySave(slot, data, out _);
        }

        public static GameSaveData Load(int slot)
        {
            return TryLoad(slot, out var data, out _) ? data : null;
        }

        public static bool Delete(int slot)
        {
            if (!IsValidSlot(slot)) return false;

            try
            {
                var path = GetPath(slot);
                if (File.Exists(path)) File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryMigrateLegacySaves(
            out SaveMigrationReport report,
            out string error)
        {
            report = new SaveMigrationReport();
            error = null;

            try
            {
                var currentRoot = GetStorageRoot();
                var legacyRoot = GetLegacyStorageRoot();
                if (string.IsNullOrWhiteSpace(legacyRoot) ||
                    PathsEqual(currentRoot, legacyRoot))
                    return true;

                var markerPath = Path.Combine(
                    currentRoot,
                    ProductIdentity.IdentityMigrationMarkerName);
                if (File.Exists(markerPath))
                {
                    report.AlreadyCompleted = true;
                    return true;
                }

                if (Directory.Exists(legacyRoot))
                {
                    var legacyPath = GetPathAtRoot(
                        legacyRoot,
                        IdentityMigrationSlot);
                    if (File.Exists(legacyPath))
                    {
                        report.LegacySlotsFound++;
                        var currentPath = GetPathAtRoot(
                            currentRoot,
                            IdentityMigrationSlot);
                        if (!TryReadValidatedSaveFile(
                                legacyPath,
                                out var legacyData,
                                out _,
                                out _))
                        {
                            report.InvalidLegacySlots++;
                            if (File.Exists(currentPath))
                            {
                                var currentIsValid = TryReadValidatedSaveFile(
                                    currentPath,
                                    out _,
                                    out var currentIsFutureVersion,
                                    out _);
                                if (currentIsValid || currentIsFutureVersion)
                                {
                                    report.PreservedCurrentSlots++;
                                    WriteIdentityMigrationMarker(markerPath);
                                }
                            }

                            return true;
                        }

                        if (File.Exists(currentPath))
                        {
                            var currentIsValid = TryReadValidatedSaveFile(
                                currentPath,
                                out var currentData,
                                out var currentIsFutureVersion,
                                out _);
                            if (currentIsFutureVersion ||
                                (currentIsValid &&
                                    CurrentSaveWins(currentData, legacyData)))
                            {
                                report.PreservedCurrentSlots++;
                                WriteIdentityMigrationMarker(markerPath);
                                return true;
                            }
                        }

                        Directory.CreateDirectory(currentRoot);
                        if (File.Exists(currentPath))
                        {
                            CreateIdentityMigrationBackup(currentPath);
                            report.BackupsCreated++;
                        }

                        CopyFileAtomically(legacyPath, currentPath);
                        report.MigratedSlots++;
                    }
                }

                Directory.CreateDirectory(currentRoot);
                WriteIdentityMigrationMarker(markerPath);
                return true;
            }
            catch (Exception ex)
            {
                error = $"旧存档迁移失败：{GetSafeMessage(ex)}";
                return false;
            }
        }

#if UNITY_EDITOR
        public static void SetStorageRootOverrideForTests(string root)
        {
            s_testStorageRootOverride = root;
        }

        public static void SetLegacyStorageRootOverrideForTests(string root)
        {
            s_testLegacyStorageRootOverride = root;
        }
#endif

        static string GetPath(int slot)
        {
            return GetPathAtRoot(GetStorageRoot(), slot);
        }

        static string GetPathAtRoot(string root, int slot)
        {
            return Path.Combine(root, $"trpg_save_{slot}.json");
        }

        static string GetStorageRoot()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(s_testStorageRootOverride))
                return s_testStorageRootOverride;

            var environmentOverride = Environment.GetEnvironmentVariable(EditorTestRootEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentOverride))
                return ValidateEditorTestStorageRoot(environmentOverride);

            if (IsAutomatedUnityTestRun())
            {
                throw new InvalidOperationException(
                    $"自动化 Unity 测试必须设置 {EditorTestRootEnvironmentVariable} " +
                    $"并在该绝对目录创建 {EditorTestSentinelFileName}；" +
                    "拒绝回落到玩家真实存档目录。");
            }
#endif
            if (IdentityMigrationSmokeEnvironment.IsRequested())
            {
                return IdentityMigrationSmokeEnvironment.GetCurrentRootOrThrow();
            }

            if (DevelopmentSmokeEnvironment.IsRequested())
            {
                return DevelopmentSmokeEnvironment.GetRootOrThrow();
            }
            return Application.persistentDataPath;
        }

        static string GetLegacyStorageRoot()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(s_testLegacyStorageRootOverride))
                return s_testLegacyStorageRootOverride;
#endif
            if (IdentityMigrationSmokeEnvironment.IsRequested())
            {
                return IdentityMigrationSmokeEnvironment.GetLegacyRootOrThrow();
            }

            if (HasStorageRootOverride()) return null;

            return ProductIdentity.GetLegacyStorageRoot(
                Application.persistentDataPath);
        }

        static bool HasStorageRootOverride()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(s_testStorageRootOverride) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                    EditorTestRootEnvironmentVariable)) ||
                IsAutomatedUnityTestRun())
                return true;
#endif
            return DevelopmentSmokeEnvironment.IsRequested() ||
                IdentityMigrationSmokeEnvironment.IsRequested();
        }

        static bool TryReadSaveFile(
            string path,
            out GameSaveData data,
            out string error)
        {
            return TryReadSaveFile(
                path,
                out data,
                out _,
                out error);
        }

        static bool TryReadSaveFile(
            string path,
            out GameSaveData data,
            out bool isFutureVersion,
            out string error)
        {
            data = null;
            isFutureVersion = false;
            error = null;
            if (!File.Exists(path))
            {
                error = "没有找到存档。";
                return false;
            }

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "存档内容为空。";
                    return false;
                }

                data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                {
                    error = "存档内容无法解析。";
                    return false;
                }

                if (data.version < 0)
                {
                    data = null;
                    error = "存档版本无效。";
                    return false;
                }

                if (data.version > GameSaveData.CurrentVersion)
                {
                    var futureVersion = data.version;
                    data = null;
                    isFutureVersion = true;
                    error = $"存档版本 {futureVersion} 高于当前支持版本 {GameSaveData.CurrentVersion}。";
                    return false;
                }

                NormalizeLoadedData(data);
                return true;
            }
            catch (Exception ex)
            {
                data = null;
                error = $"读取存档失败：{GetSafeMessage(ex)}";
                return false;
            }
        }

        static bool TryReadValidatedSaveFile(
            string path,
            out GameSaveData data,
            out bool isFutureVersion,
            out string error)
        {
            if (!TryReadSaveFile(
                    path,
                    out data,
                    out isFutureVersion,
                    out error))
                return false;
            if (GameStateManager.TryValidateSaveData(data, out error)) return true;

            data = null;
            return false;
        }

        static bool CurrentSaveWins(
            GameSaveData currentData,
            GameSaveData legacyData)
        {
            if (currentData.savedAtTicks <= 0 || legacyData.savedAtTicks <= 0)
                return true;
            return currentData.savedAtTicks >= legacyData.savedAtTicks;
        }

        static void CreateIdentityMigrationBackup(string currentPath)
        {
            var baseBackupPath = currentPath + IdentityMigrationBackupSuffix;
            var backupPath = baseBackupPath;
            for (var suffix = 1; File.Exists(backupPath); suffix++)
                backupPath = baseBackupPath + "." + suffix;

            File.Copy(currentPath, backupPath, false);
        }

        static void WriteIdentityMigrationMarker(string markerPath)
        {
            if (File.Exists(markerPath)) return;

            var temporaryPath = markerPath + "." +
                Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(
                    temporaryPath,
                    $"Legacy={ProductIdentity.LegacyCompanyName}/" +
                    ProductIdentity.LegacyProductName +
                    Environment.NewLine +
                    $"CompletedAtUtc={DateTime.UtcNow:O}" +
                    Environment.NewLine);
                File.Move(temporaryPath, markerPath);
            }
            finally
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }

        static void CopyFileAtomically(string sourcePath, string destinationPath)
        {
            var temporaryPath = destinationPath + "." +
                Guid.NewGuid().ToString("N") + ".migration.tmp";
            try
            {
                File.Copy(sourcePath, temporaryPath, false);
                if (File.Exists(destinationPath))
                    ReplaceExistingFile(temporaryPath, destinationPath);
                else
                    File.Move(temporaryPath, destinationPath);
            }
            finally
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }

        static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) ||
                string.IsNullOrWhiteSpace(right))
                return false;

            var normalizedLeft = Path.GetFullPath(left).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            var normalizedRight = Path.GetFullPath(right).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            return string.Equals(
                normalizedLeft,
                normalizedRight,
                StringComparison.OrdinalIgnoreCase);
        }

#if UNITY_EDITOR
        static bool IsAutomatedUnityTestRun()
        {
            return Array.Exists(Environment.GetCommandLineArgs(), argument =>
                string.Equals(
                    argument,
                    "-runTests",
                    StringComparison.OrdinalIgnoreCase));
        }

        static string ValidateEditorTestStorageRoot(string configuredRoot)
        {
            if (!Path.IsPathRooted(configuredRoot))
            {
                throw new InvalidOperationException(
                    $"{EditorTestRootEnvironmentVariable} 必须是绝对路径。");
            }

            var root = Path.GetFullPath(configuredRoot).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(root) ||
                !File.Exists(Path.Combine(root, EditorTestSentinelFileName)))
            {
                throw new InvalidOperationException(
                    "自动化 Unity 测试存档目录不存在，或缺少专用哨兵文件。");
            }

            var companyDirectory = Directory.GetParent(
                Application.persistentDataPath);
            var platformRoot = companyDirectory?.Parent?.FullName;
            if (!string.IsNullOrWhiteSpace(platformRoot) &&
                IsPathWithin(root, platformRoot))
            {
                throw new InvalidOperationException(
                    "自动化 Unity 测试存档目录不能位于玩家真实 " +
                    "persistentDataPath 平台根内。");
            }

            return root;
        }

        static bool IsPathWithin(string candidate, string parent)
        {
            var normalizedCandidate = Path.GetFullPath(candidate).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            var normalizedParent = Path.GetFullPath(parent).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            if (PathsEqual(normalizedCandidate, normalizedParent)) return true;

            return normalizedCandidate.StartsWith(
                normalizedParent + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
        }
#endif

        static void NormalizeLoadedData(GameSaveData data)
        {
            data.version = GameSaveData.CurrentVersion;
            data.flags ??= new List<string>();
            data.inventoryItemIds ??= new List<string>();
            if (data.investigator != null)
                data.investigator.skills ??= new List<SkillEntry>();
        }

        static void ReplaceExistingFile(string temporaryPath, string destinationPath)
        {
            try
            {
                File.Replace(temporaryPath, destinationPath, null);
            }
            catch (PlatformNotSupportedException)
            {
                var backupPath = destinationPath + ".backup";
                File.Copy(destinationPath, backupPath, true);
                try
                {
                    File.Copy(temporaryPath, destinationPath, true);
                    File.Delete(temporaryPath);
                    File.Delete(backupPath);
                }
                catch
                {
                    File.Copy(backupPath, destinationPath, true);
                    throw;
                }
            }
        }

        static void TryDeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // A failed cleanup must not hide the original save error.
            }
        }

        static string GetSafeMessage(Exception exception)
        {
            return string.IsNullOrWhiteSpace(exception.Message)
                ? exception.GetType().Name
                : exception.Message;
        }
    }
}
