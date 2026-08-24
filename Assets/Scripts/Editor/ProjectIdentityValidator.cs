using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using WalkingIntoNight.TRPG.Core;

namespace WalkingIntoNight.TRPG.Editor
{
    public static class ProjectIdentityValidator
    {
        public static bool TryValidate(out string error)
        {
            if (PlayerSettings.companyName != ProductIdentity.CompanyName)
            {
                error = $"companyName 应为 {ProductIdentity.CompanyName}，" +
                    $"当前为 {PlayerSettings.companyName}。";
                return false;
            }

            if (PlayerSettings.productName != ProductIdentity.ProductName)
            {
                error = $"productName 应为 {ProductIdentity.ProductName}，" +
                    $"当前为 {PlayerSettings.productName}。";
                return false;
            }

            if (PlayerSettings.bundleVersion != ProductIdentity.ProductVersion)
            {
                error = $"bundleVersion 应为 {ProductIdentity.ProductVersion}，" +
                    $"当前为 {PlayerSettings.bundleVersion}。";
                return false;
            }

            var identifier = PlayerSettings.GetApplicationIdentifier(
                NamedBuildTarget.Standalone);
            if (identifier != ProductIdentity.ApplicationIdentifier)
            {
                error = "Standalone applicationIdentifier 应为 " +
                    $"{ProductIdentity.ApplicationIdentifier}，当前为 {identifier}。";
                return false;
            }

            error = null;
            return true;
        }

        public static void ValidateOrThrow()
        {
            if (!TryValidate(out var error))
                throw new InvalidOperationException("产品身份检查失败：" + error);
        }
    }

    public sealed class ProjectIdentityBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            ProjectIdentityValidator.ValidateOrThrow();
        }
    }
}
