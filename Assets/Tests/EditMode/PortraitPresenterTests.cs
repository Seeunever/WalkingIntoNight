using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using WalkingIntoNight.TRPG.UI;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class PortraitPresenterTests
    {
        GameObject m_rootObject;
        GameObject m_frameObject;
        GameObject m_portraitObject;
        Texture2D m_texture;
        Sprite m_sprite;
        RectTransform m_root;
        RectTransform m_frame;
        RectTransform m_portrait;
        Image m_image;
        PortraitPresenter m_presenter;

        [SetUp]
        public void SetUp()
        {
            m_rootObject = new GameObject("PortraitTestRoot", typeof(RectTransform));
            m_root = m_rootObject.GetComponent<RectTransform>();
            m_root.pivot = new Vector2(0.5f, 0.5f);

            m_frameObject = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
            m_frame = m_frameObject.GetComponent<RectTransform>();
            m_frame.SetParent(m_root, false);
            m_frame.anchorMin = new Vector2(0.67f, 0.35f);
            m_frame.anchorMax = new Vector2(0.98f, 0.98f);
            m_frame.offsetMin = Vector2.zero;
            m_frame.offsetMax = Vector2.zero;
            m_frameObject.AddComponent<RectMask2D>();

            m_portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            m_portrait = m_portraitObject.GetComponent<RectTransform>();
            m_portrait.SetParent(m_frame, false);
            m_portrait.anchorMin = Vector2.zero;
            m_portrait.anchorMax = Vector2.one;
            m_portrait.offsetMin = new Vector2(12f, 12f);
            m_portrait.offsetMax = new Vector2(-12f, -12f);
            m_image = m_portraitObject.GetComponent<Image>();

            m_presenter = m_frameObject.AddComponent<PortraitPresenter>();
            m_presenter.Initialize(m_image, m_portrait);
            m_presenter.SetAnimationsEnabled(false);

            m_texture = new Texture2D(4, 4);
            m_sprite = Sprite.Create(m_texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_rootObject);
            Object.DestroyImmediate(m_sprite);
            Object.DestroyImmediate(m_texture);
        }

        [Test]
        public void StaticFallback_ShowTalkingEmotionAndHide_EndInStableStates()
        {
            Assert.That(m_image.preserveAspect, Is.True);
            Assert.That(m_image.raycastTarget, Is.False);
            Assert.That(m_frameObject.activeSelf, Is.False);

            m_presenter.Show(m_sprite);
            m_presenter.SetTalking(true);

            Assert.That(m_presenter.IsVisible, Is.True);
            Assert.That(m_presenter.CurrentSprite, Is.SameAs(m_sprite));
            Assert.That(m_presenter.IsTransitioning, Is.False);
            Assert.That(m_portrait.anchoredPosition, Is.EqualTo(m_presenter.RestPosition));
            Assert.That(m_portrait.localScale.x, Is.InRange(1.01f, 1.02f));

            m_presenter.Show(m_sprite);
            m_presenter.PlayEmotion(PortraitEmotion.Emphasis);
            Assert.That(m_presenter.IsTransitioning, Is.False);
            Assert.That(m_portrait.localScale.x, Is.InRange(1.01f, 1.02f));

            m_presenter.Hide();
            Assert.That(m_frameObject.activeSelf, Is.False);
            Assert.That(m_presenter.CurrentSprite, Is.Null);
        }

        [TestCase(1024f, 768f)]
        [TestCase(1920f, 1080f)]
        [TestCase(800f, 600f)]
        public void PortraitRemainsInsideFrameAtCommonViewSizes(float width, float height)
        {
            m_root.sizeDelta = new Vector2(width, height);
            m_presenter.Show(m_sprite, animated: false);
            m_presenter.SetTalking(true);
            m_root.ForceUpdateRectTransforms();

            var frameCorners = new Vector3[4];
            var portraitCorners = new Vector3[4];
            m_frame.GetWorldCorners(frameCorners);
            m_portrait.GetWorldCorners(portraitCorners);

            Assert.That(portraitCorners[0].x, Is.GreaterThanOrEqualTo(frameCorners[0].x - 0.01f));
            Assert.That(portraitCorners[0].y, Is.GreaterThanOrEqualTo(frameCorners[0].y - 0.01f));
            Assert.That(portraitCorners[2].x, Is.LessThanOrEqualTo(frameCorners[2].x + 0.01f));
            Assert.That(portraitCorners[2].y, Is.LessThanOrEqualTo(frameCorners[2].y + 0.01f));
        }
    }
}
