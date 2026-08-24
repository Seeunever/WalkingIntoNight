using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class AssetImportSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            NPCDatabase.Reload();
            PortraitDatabase.ClearCache();
        }

        [TestCase("Assets/Resources/Art/Characters/mei_barista_v1.png", 1024)]
        [TestCase("Assets/Resources/Art/Characters/chen_regular_v2.png", 1024)]
        [TestCase("Assets/Resources/Art/Characters/shop_cat_v1.png", 1024)]
        public void Portraits_AreSpritesWithoutMipmapsAndUseBoundedImportSize(
            string assetPath,
            int expectedMaxSize)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            Assert.That(importer, Is.Not.Null, assetPath);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.GetDefaultPlatformTextureSettings().maxTextureSize,
                Is.EqualTo(expectedMaxSize));
        }

        [Test]
        public void ScenarioNpcPortraitMapping_UsesExistingExpectedAssets()
        {
            var expected = new Dictionary<string, string>
            {
                { "barista_mei", "mei_barista_v1" },
                { "regular_chen", "chen_regular_v2" },
                { "stray_cat", "shop_cat_v1" }
            };

            foreach (var pair in expected)
            {
                var npc = NPCDatabase.GetNpc(pair.Key);
                Assert.That(npc, Is.Not.Null, pair.Key);
                Assert.That(npc.portraitId, Is.EqualTo(pair.Value), pair.Key);
                Assert.That(PortraitDatabase.Get(npc.portraitId), Is.Not.Null, pair.Value);
            }

            Assert.That(NPCDatabase.GetNpc("silent_woman").portraitId, Is.Empty,
                "黑衣女人的正式头像尚未完成，不应回退到水印占位图。");
            Assert.That(NPCDatabase.GetNpc("owner_ghost").portraitId, Is.Empty,
                "老板影子的正式头像尚未完成，不应回退到水印占位图。");
        }

        [TestCase("bear")]
        [TestCase("fox")]
        [TestCase("rabbit")]
        [TestCase("swan")]
        public void RuntimePortraits_DoNotExposeRetiredWatermarkedPlaceholders(
            string legacyPortraitId)
        {
            Assert.That(PortraitDatabase.Get(legacyPortraitId), Is.Null);
        }

        [Test]
        public void FontLicenses_ArePresentBesideDistributedFontAssets()
        {
            Assert.That(File.Exists("Assets/Resources/Fonts/NotoSansSC-LICENSE.txt"), Is.True);
            Assert.That(File.Exists("Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt"), Is.True);
        }
    }
}
