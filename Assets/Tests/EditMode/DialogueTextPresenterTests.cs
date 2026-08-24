using NUnit.Framework;
using TMPro;
using UnityEngine;
using WalkingIntoNight.TRPG.UI;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class DialogueTextPresenterTests
    {
        GameObject m_textObject;
        TMP_Text m_text;
        DialogueTextPresenter m_presenter;

        [SetUp]
        public void SetUp()
        {
            DialoguePresentationSettings.ResetDefaults();
            m_textObject = new GameObject("DialogueTextPresenterTest", typeof(RectTransform));
            m_text = m_textObject.AddComponent<TextMeshProUGUI>();
            m_presenter = m_textObject.AddComponent<DialogueTextPresenter>();
            m_presenter.Initialize(m_text);
        }

        [TearDown]
        public void TearDown()
        {
            DialoguePresentationSettings.ResetDefaults();
            Object.DestroyImmediate(m_textObject);
        }

        [Test]
        public void StaticFallback_PresentsCompleteTextAndInvokesCompletionOnce()
        {
            var completions = 0;

            m_presenter.Present("完整文本", () => completions++);

            Assert.That(m_text.text, Is.EqualTo("完整文本"));
            Assert.That(m_text.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            Assert.That(m_presenter.IsRevealing, Is.False);
            Assert.That(m_presenter.IsComplete, Is.True);
            Assert.That(m_presenter.CanAdvance, Is.True);
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void AccessibilitySetting_CanDisableTypewriterCentrally()
        {
            DialoguePresentationSettings.TypewriterEnabled = false;

            m_presenter.Present("立即显示", animated: true);

            Assert.That(m_presenter.IsRevealing, Is.False);
            Assert.That(m_text.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
        }
    }
}
