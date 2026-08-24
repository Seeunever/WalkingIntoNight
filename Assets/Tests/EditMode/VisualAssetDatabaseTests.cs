using NUnit.Framework;
using WalkingIntoNight.TRPG.NPC;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class VisualAssetDatabaseTests
    {
        [SetUp]
        public void SetUp()
        {
            NPCDatabase.Reload();
            PortraitDatabase.ClearCache();
            LocationArtDatabase.ClearCache();
        }

        [TestCase("barista_mei")]
        [TestCase("regular_chen")]
        [TestCase("stray_cat")]
        public void CurrentCafeNpcPortraits_LoadAsSprites(string npcId)
        {
            var npc = NPCDatabase.GetNpc(npcId);

            Assert.That(npc, Is.Not.Null);
            Assert.That(PortraitDatabase.Get(npc.portraitId), Is.Not.Null);
        }

        [Test]
        public void CafeMainBackground_LoadsAsSprite()
        {
            var location = NPCDatabase.GetLocation("cafe_main");

            Assert.That(location, Is.Not.Null);
            Assert.That(location.backgroundId, Is.EqualTo("cafe_main_v1"));
            Assert.That(LocationArtDatabase.Get(location.backgroundId), Is.Not.Null);
        }
    }
}
