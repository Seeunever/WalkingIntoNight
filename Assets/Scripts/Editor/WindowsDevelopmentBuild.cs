using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using WalkingIntoNight.TRPG.Core;

namespace WalkingIntoNight.TRPG.Editor
{
    public static class WindowsDevelopmentBuild
    {
        const string OutputEnvironmentVariable = "WALKING_INTO_NIGHT_BUILD_EXE";

        [MenuItem("WalkingIntoNight/Build/Windows Development")]
        public static void Build()
        {
            ProjectIdentityValidator.ValidateOrThrow();

            var outputPath = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    "Builds",
                    "WindowsDevelopment",
                    ProductIdentity.ProductName + ".exe"));
            }

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("Build Settings 中没有启用场景。");

            var directory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("构建输出路径无效。");
            Directory.CreateDirectory(directory);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Windows Development Build 失败：{report.summary.result}，" +
                    $"错误 {report.summary.totalErrors}，警告 {report.summary.totalWarnings}。");

            File.WriteAllText(
                Path.Combine(directory, "BUILD_INFO.txt"),
                $"{ProductIdentity.ProductName} Windows Development Build{Environment.NewLine}" +
                $"BuiltAtUtc={DateTime.UtcNow:O}{Environment.NewLine}" +
                $"Editor={Application.unityVersion}{Environment.NewLine}" +
                $"Version={ProductIdentity.ProductVersion}{Environment.NewLine}" +
                $"Company={ProductIdentity.CompanyName}{Environment.NewLine}" +
                $"Product={ProductIdentity.ProductName}{Environment.NewLine}" +
                $"Identifier={ProductIdentity.ApplicationIdentifier}{Environment.NewLine}" +
                $"Scenes={string.Join(",", scenes)}{Environment.NewLine}" +
                $"SizeBytes={report.summary.totalSize}{Environment.NewLine}");
            Debug.Log($"WINDOWS_DEVELOPMENT_BUILD_PASS: {outputPath}");
        }
    }
}
