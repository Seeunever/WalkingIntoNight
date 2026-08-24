using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace WalkingIntoNight.TRPG.UI
{
    public static class DialoguePresentationSettings
    {
        public const float DefaultCharactersPerSecond = 42f;
        public const float DefaultRevealAdvanceGuardSeconds = 0.2f;

        public static bool TypewriterEnabled { get; set; } = true;
        public static float CharactersPerSecond { get; set; } = DefaultCharactersPerSecond;
        public static float RevealAdvanceGuardSeconds { get; set; } =
            DefaultRevealAdvanceGuardSeconds;

        public static void ResetDefaults()
        {
            TypewriterEnabled = true;
            CharactersPerSecond = DefaultCharactersPerSecond;
            RevealAdvanceGuardSeconds = DefaultRevealAdvanceGuardSeconds;
        }
    }

    [DisallowMultipleComponent]
    public sealed class DialogueTextPresenter : MonoBehaviour
    {
        TMP_Text m_text;
        Coroutine m_revealRoutine;
        Action m_onCompleted;
        int m_totalCharacters;
        float m_advanceAllowedAt;
        bool m_initialized;

        public bool IsRevealing { get; private set; }
        public bool IsComplete => m_initialized && !IsRevealing;
        public bool CanAdvance => IsComplete && Time.unscaledTime >= m_advanceAllowedAt;
        public int TotalCharacterCount => m_totalCharacters;
        public int VisibleCharacterCount => m_text != null
            ? Mathf.Min(m_text.maxVisibleCharacters, m_totalCharacters)
            : 0;

        public void Initialize(TMP_Text text)
        {
            Cancel();
            m_text = text != null ? text : GetComponent<TMP_Text>();
            m_initialized = m_text != null;
            if (!m_initialized) return;
            m_text.maxVisibleCharacters = int.MaxValue;
            m_advanceAllowedAt = Time.unscaledTime;
        }

        public void Present(string text, Action onCompleted = null, bool animated = true)
        {
            if (!EnsureInitialized()) return;
            Cancel();

            m_text.text = text ?? string.Empty;
            m_text.ForceMeshUpdate();
            m_totalCharacters = m_text.textInfo.characterCount;
            m_onCompleted = onCompleted;

            var shouldAnimate = animated && Application.isPlaying &&
                DialoguePresentationSettings.TypewriterEnabled &&
                DialoguePresentationSettings.CharactersPerSecond > 0f &&
                m_totalCharacters > 0;
            if (!shouldAnimate)
            {
                FinishReveal(guardAdvance: false);
                return;
            }

            IsRevealing = true;
            m_text.maxVisibleCharacters = 0;
            m_advanceAllowedAt = float.PositiveInfinity;
            m_revealRoutine = StartCoroutine(RevealRoutine());
        }

        public bool RevealImmediately()
        {
            if (!IsRevealing || !EnsureInitialized()) return false;
            StopRevealRoutine();
            FinishReveal(guardAdvance: true);
            return true;
        }

        public void Cancel(bool revealCurrentText = false)
        {
            StopRevealRoutine();
            IsRevealing = false;
            m_onCompleted = null;
            if (!m_initialized || m_text == null) return;
            if (revealCurrentText)
                m_text.maxVisibleCharacters = int.MaxValue;
            m_advanceAllowedAt = Time.unscaledTime;
        }

        IEnumerator RevealRoutine()
        {
            var visible = 0f;
            while (visible < m_totalCharacters)
            {
                visible += DialoguePresentationSettings.CharactersPerSecond *
                    Time.unscaledDeltaTime;
                m_text.maxVisibleCharacters = Mathf.Clamp(
                    Mathf.FloorToInt(visible),
                    0,
                    m_totalCharacters);
                yield return null;
            }

            m_revealRoutine = null;
            FinishReveal(guardAdvance: false);
        }

        void FinishReveal(bool guardAdvance)
        {
            IsRevealing = false;
            m_text.maxVisibleCharacters = int.MaxValue;
            m_advanceAllowedAt = Time.unscaledTime + (guardAdvance
                ? Mathf.Max(0f, DialoguePresentationSettings.RevealAdvanceGuardSeconds)
                : 0f);
            var completed = m_onCompleted;
            m_onCompleted = null;
            completed?.Invoke();
        }

        bool EnsureInitialized()
        {
            if (m_initialized) return true;
            Initialize(m_text);
            return m_initialized;
        }

        void StopRevealRoutine()
        {
            if (m_revealRoutine == null) return;
            StopCoroutine(m_revealRoutine);
            m_revealRoutine = null;
        }

        void OnDestroy()
        {
            Cancel();
        }
    }
}
