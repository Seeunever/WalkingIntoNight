using System;
using System.IO;
using NUnit.Framework;
using WalkingIntoNight.TRPG.Core;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class DevelopmentSmokeEnvironmentTests
    {
        string m_testRoot;
        string m_platformRoot;
        string m_persistentDataPath;

        [SetUp]
        public void SetUp()
        {
            m_testRoot = Path.Combine(
                Path.GetTempPath(),
                "WalkingIntoNightSmokeEnvironmentTests",
                Guid.NewGuid().ToString("N"));
            m_platformRoot = Path.Combine(m_testRoot, "LocalLow");
            m_persistentDataPath = Path.Combine(
                m_platformRoot,
                ProductIdentity.CompanyName,
                ProductIdentity.ProductName);
            Directory.CreateDirectory(m_persistentDataPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_testRoot))
                Directory.Delete(m_testRoot, true);
        }

        [Test]
        public void ValidateRootOrThrow_AcceptsExternalSentinelDirectory()
        {
            var smokeRoot = Path.Combine(m_testRoot, "ExternalSmoke");
            Directory.CreateDirectory(smokeRoot);
            File.WriteAllText(Path.Combine(
                smokeRoot,
                DevelopmentSmokeEnvironment.SentinelFileName), "smoke");

            var result = DevelopmentSmokeEnvironment.ValidateRootOrThrow(
                smokeRoot,
                m_persistentDataPath);

            Assert.That(Path.GetFullPath(result),
                Is.EqualTo(Path.GetFullPath(smokeRoot)));
        }

        [Test]
        public void ValidateRootOrThrow_RejectsMissingSentinel()
        {
            var smokeRoot = Path.Combine(m_testRoot, "NoSentinel");
            Directory.CreateDirectory(smokeRoot);

            Assert.Throws<InvalidOperationException>(() =>
                DevelopmentSmokeEnvironment.ValidateRootOrThrow(
                    smokeRoot,
                    m_persistentDataPath));
        }

        [Test]
        public void ValidateRootOrThrow_RejectsRelativePath()
        {
            Assert.Throws<InvalidOperationException>(() =>
                DevelopmentSmokeEnvironment.ValidateRootOrThrow(
                    "relative-smoke-root",
                    m_persistentDataPath));
        }

        [Test]
        public void ValidateRootOrThrow_RejectsAnyDirectoryInsideLocalLow()
        {
            var smokeRoot = Path.Combine(m_platformRoot, "DedicatedSmoke");
            Directory.CreateDirectory(smokeRoot);
            File.WriteAllText(Path.Combine(
                smokeRoot,
                DevelopmentSmokeEnvironment.SentinelFileName), "smoke");

            Assert.Throws<InvalidOperationException>(() =>
                DevelopmentSmokeEnvironment.ValidateRootOrThrow(
                    smokeRoot,
                    m_persistentDataPath));
        }
    }
}
