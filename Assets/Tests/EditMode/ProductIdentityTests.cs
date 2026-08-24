using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Editor;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class ProductIdentityTests
    {
        [Test]
        public void PlayerSettings_MatchLockedProductIdentity()
        {
            Assert.That(PlayerSettings.companyName,
                Is.EqualTo(ProductIdentity.CompanyName));
            Assert.That(PlayerSettings.productName,
                Is.EqualTo(ProductIdentity.ProductName));
            Assert.That(PlayerSettings.bundleVersion,
                Is.EqualTo(ProductIdentity.ProductVersion));
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(
                    NamedBuildTarget.Standalone),
                Is.EqualTo(ProductIdentity.ApplicationIdentifier));
            Assert.That(ProjectIdentityValidator.TryValidate(out var error),
                Is.True, error);
        }

        [Test]
        public void ProjectSettings_ExplicitlyOverrideStandaloneIdentifier()
        {
            var projectSettingsPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "ProjectSettings",
                "ProjectSettings.asset"));
            var yaml = File.ReadAllText(projectSettingsPath)
                .Replace("\r\n", "\n");

            StringAssert.Contains(
                "applicationIdentifier:\n" +
                "    Standalone: " + ProductIdentity.ApplicationIdentifier,
                yaml);
            StringAssert.Contains(
                "bundleVersion: " + ProductIdentity.ProductVersion,
                yaml);
            StringAssert.Contains("overrideDefaultApplicationIdentifier: 1", yaml);
        }

        [Test]
        public void LegacyStorageRoot_UsesFormerCompanyAndProductFolders()
        {
            var platformRoot = Path.Combine(Path.GetTempPath(), "LocalLow");
            var currentRoot = Path.Combine(
                platformRoot,
                ProductIdentity.CompanyName,
                ProductIdentity.ProductName);

            var legacyRoot = ProductIdentity.GetLegacyStorageRoot(currentRoot);

            Assert.That(
                Path.GetFullPath(legacyRoot),
                Is.EqualTo(Path.GetFullPath(Path.Combine(
                    platformRoot,
                    ProductIdentity.LegacyCompanyName,
                    ProductIdentity.LegacyProductName))));
        }
    }
}
