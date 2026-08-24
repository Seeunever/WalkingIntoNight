using System;
using System.IO;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Core
{
    public static class IdentityMigrationSmokeEnvironment
    {
        public const string CommandLineFlag = "-winIdentityMigrationSmokeTest";
        public const string RootEnvironmentVariable =
            "WALKING_INTO_NIGHT_IDENTITY_SMOKE_ROOT";
        public const string PhaseEnvironmentVariable =
            "WALKING_INTO_NIGHT_IDENTITY_SMOKE_PHASE";
        public const string SentinelFileName =
            ".walking_into_night_identity_smoke";

        public static bool IsRequested()
        {
            return Debug.isDebugBuild && HasArgument(CommandLineFlag);
        }

        public static string GetRootOrThrow()
        {
            if (HasArgument("-winSmokeTest"))
            {
                throw new InvalidOperationException(
                    $"{CommandLineFlag} 不能与 -winSmokeTest 同时使用。");
            }

            var configuredRoot = Environment.GetEnvironmentVariable(
                RootEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(configuredRoot) ||
                !Path.IsPathRooted(configuredRoot))
            {
                throw new InvalidOperationException(
                    $"{RootEnvironmentVariable} 必须是非空绝对路径。");
            }

            var root = Path.GetFullPath(configuredRoot).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(root) ||
                !File.Exists(Path.Combine(root, SentinelFileName)))
            {
                throw new InvalidOperationException(
                    "身份迁移烟测目录不存在，或缺少专用哨兵文件。");
            }

            var actualCompanyDirectory = Directory.GetParent(
                Application.persistentDataPath);
            var actualPlatformRoot = actualCompanyDirectory?.Parent?.FullName;
            if (!string.IsNullOrWhiteSpace(actualPlatformRoot) &&
                IsPathWithin(root, actualPlatformRoot))
            {
                throw new InvalidOperationException(
                    "身份迁移烟测目录不能位于玩家真实 persistentDataPath 平台根内。");
            }

            var legacyRoot = Path.Combine(root, "Legacy");
            var currentRoot = Path.Combine(root, "Current");
            if (PathsEqual(legacyRoot, currentRoot))
                throw new InvalidOperationException("身份迁移烟测的新旧目录发生重叠。");

            return root;
        }

        public static string GetLegacyRootOrThrow()
        {
            return Path.Combine(GetRootOrThrow(), "Legacy");
        }

        public static string GetCurrentRootOrThrow()
        {
            return Path.Combine(GetRootOrThrow(), "Current");
        }

        public static string GetPhaseOrThrow()
        {
            var phase = Environment.GetEnvironmentVariable(
                PhaseEnvironmentVariable)?.Trim().ToLowerInvariant();
            if (phase == "first" || phase == "second") return phase;

            throw new InvalidOperationException(
                $"{PhaseEnvironmentVariable} 必须是 first 或 second。");
        }

        static bool HasArgument(string expected)
        {
            return Array.Exists(Environment.GetCommandLineArgs(), argument =>
                string.Equals(
                    argument,
                    expected,
                    StringComparison.OrdinalIgnoreCase));
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

        static bool PathsEqual(string left, string right)
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),
                Path.GetFullPath(right).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
