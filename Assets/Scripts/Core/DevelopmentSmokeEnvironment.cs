using System;
using System.IO;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Core
{
    public static class DevelopmentSmokeEnvironment
    {
        public const string CommandLineFlag = "-winSmokeTest";
        public const string RootEnvironmentVariable =
            "WALKING_INTO_NIGHT_SMOKE_SAVE_ROOT";
        public const string SentinelFileName =
            ".walking_into_night_gameplay_smoke";

        public static bool IsRequested()
        {
            return Debug.isDebugBuild && HasArgument(CommandLineFlag);
        }

        public static string GetRootOrThrow()
        {
            if (HasArgument(IdentityMigrationSmokeEnvironment.CommandLineFlag))
            {
                throw new InvalidOperationException(
                    $"{CommandLineFlag} 不能与 " +
                    $"{IdentityMigrationSmokeEnvironment.CommandLineFlag} 同时使用。");
            }

            return ValidateRootOrThrow(
                Environment.GetEnvironmentVariable(RootEnvironmentVariable),
                Application.persistentDataPath);
        }

        public static string ValidateRootOrThrow(
            string configuredRoot,
            string persistentDataPath)
        {
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
                    "Windows 游戏流程烟测目录不存在，或缺少专用哨兵文件。");
            }

            var companyDirectory = string.IsNullOrWhiteSpace(persistentDataPath)
                ? null
                : Directory.GetParent(Path.GetFullPath(persistentDataPath));
            var platformRoot = companyDirectory?.Parent?.FullName;
            if (!string.IsNullOrWhiteSpace(platformRoot) &&
                IsPathWithin(root, platformRoot))
            {
                throw new InvalidOperationException(
                    "Windows 游戏流程烟测目录不能位于玩家真实 " +
                    "persistentDataPath 平台根内。");
            }

            return root;
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
            if (string.Equals(
                    normalizedCandidate,
                    normalizedParent,
                    StringComparison.OrdinalIgnoreCase))
                return true;

            return normalizedCandidate.StartsWith(
                normalizedParent + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
