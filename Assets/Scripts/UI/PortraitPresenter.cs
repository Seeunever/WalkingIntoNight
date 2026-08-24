using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WalkingIntoNight.TRPG.UI
{
    public enum PortraitEmotion
    {
        None,
        Emphasis
    }

    [DisallowMultipleComponent]
    public sealed class PortraitPresenter : MonoBehaviour
    {
        const float EnterDuration = 0.2f;
        const float ExitDuration = 0.1f;
        const float EmotionDuration = 0.18f;
        const float EnterOffset = 18f;
        const float TalkingScale = 1.015f;
        const float EmotionScale = 1.035f;

        Image m_image;
        CanvasGroup m_canvasGroup;
        RectTransform m_animatedRect;
        Coroutine m_transition;
        Vector2 m_restPosition;
        Vector3 m_restScale = Vector3.one;
        bool m_initialized;
        bool m_talking;
        bool m_animationsEnabled = true;

        public Sprite CurrentSprite => m_image != null ? m_image.sprite : null;
        public bool IsVisible => m_initialized && gameObject.activeSelf &&
            m_canvasGroup.alpha >= 0.999f && CurrentSprite != null;
        public bool IsTransitioning => m_transition != null;
        public bool AnimationsEnabled => m_animationsEnabled;
        public Vector2 RestPosition => m_restPosition;
        public Vector3 RestScale => m_restScale;

        public void Initialize(Image image, RectTransform animatedRect = null)
        {
            StopTransition();
            m_image = image != null ? image : GetComponent<Image>();
            m_animatedRect = animatedRect != null
                ? animatedRect
                : m_image != null ? m_image.rectTransform : transform as RectTransform;
            m_canvasGroup = GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (m_image != null)
            {
                m_image.preserveAspect = true;
                m_image.raycastTarget = false;
            }

            if (m_animatedRect != null)
            {
                m_restPosition = m_animatedRect.anchoredPosition;
                m_restScale = m_animatedRect.localScale;
                if (m_restScale == Vector3.zero)
                    m_restScale = Vector3.one;
            }

            m_initialized = m_image != null && m_animatedRect != null;
            ApplyHiddenState(clearSprite: true);
        }

        public void SetAnimationsEnabled(bool enabled)
        {
            if (m_animationsEnabled == enabled) return;
            m_animationsEnabled = enabled;
            if (enabled || !m_initialized) return;

            StopTransition();
            if (gameObject.activeSelf && CurrentSprite != null)
                ApplyVisibleState();
            else
                ApplyHiddenState(clearSprite: true);
        }

        public void Show(Sprite sprite, bool animated = true)
        {
            if (!EnsureInitialized()) return;
            if (sprite == null)
            {
                Hide(animated);
                return;
            }

            var sameVisibleSprite = gameObject.activeSelf && m_image.sprite == sprite;
            if (sameVisibleSprite)
            {
                // Consecutive lines from the same character keep the existing presentation.
                // A pending swap may still target another character, so cancel it
                // before keeping the currently requested sprite.
                StopTransition();
                ApplyVisibleState();
                return;
            }

            var hadVisiblePortrait = gameObject.activeSelf && m_image.sprite != null &&
                m_canvasGroup.alpha > 0.001f;
            StopTransition();
            gameObject.SetActive(true);

            if (!animated || !m_animationsEnabled || !Application.isPlaying)
            {
                m_image.sprite = sprite;
                ApplyVisibleState();
                return;
            }

            m_transition = StartCoroutine(hadVisiblePortrait
                ? SwapRoutine(sprite)
                : EnterRoutine(sprite));
        }

        public void Hide(bool animated = true)
        {
            if (!EnsureInitialized()) return;
            StopTransition();

            if (!gameObject.activeSelf || !animated || !m_animationsEnabled ||
                !Application.isPlaying)
            {
                ApplyHiddenState(clearSprite: true);
                return;
            }

            m_transition = StartCoroutine(HideRoutine());
        }

        public void SetTalking(bool talking)
        {
            m_talking = talking;
            if (!EnsureInitialized() || !gameObject.activeSelf) return;

            m_image.color = talking
                ? Color.white
                : new Color(0.93f, 0.93f, 0.96f, 1f);
            if (!IsTransitioning)
                m_animatedRect.localScale = TargetScale();
        }

        public void PlayEmotion(PortraitEmotion emotion)
        {
            if (emotion == PortraitEmotion.None || !EnsureInitialized() ||
                !gameObject.activeSelf || CurrentSprite == null)
                return;

            StopTransition();
            ApplyVisibleState();
            if (!m_animationsEnabled || !Application.isPlaying) return;
            m_transition = StartCoroutine(EmotionRoutine());
        }

        IEnumerator EnterRoutine(Sprite sprite)
        {
            m_image.sprite = sprite;
            m_canvasGroup.alpha = 0f;
            m_animatedRect.anchoredPosition = m_restPosition + Vector2.right * EnterOffset;
            m_animatedRect.localScale = TargetScale();
            var elapsed = 0f;
            while (elapsed < EnterDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / EnterDuration);
                m_canvasGroup.alpha = t;
                m_animatedRect.anchoredPosition = Vector2.Lerp(
                    m_restPosition + Vector2.right * EnterOffset,
                    m_restPosition,
                    Smooth(t));
                yield return null;
            }

            m_transition = null;
            ApplyVisibleState();
        }

        IEnumerator SwapRoutine(Sprite sprite)
        {
            var startAlpha = m_canvasGroup.alpha;
            var startPosition = m_animatedRect.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < ExitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / ExitDuration);
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                m_animatedRect.anchoredPosition = Vector2.Lerp(
                    startPosition,
                    m_restPosition - Vector2.right * 8f,
                    Smooth(t));
                yield return null;
            }

            m_image.sprite = sprite;
            m_canvasGroup.alpha = 0f;
            m_animatedRect.anchoredPosition = m_restPosition + Vector2.right * EnterOffset;
            elapsed = 0f;
            while (elapsed < EnterDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / EnterDuration);
                m_canvasGroup.alpha = t;
                m_animatedRect.anchoredPosition = Vector2.Lerp(
                    m_restPosition + Vector2.right * EnterOffset,
                    m_restPosition,
                    Smooth(t));
                yield return null;
            }

            m_transition = null;
            ApplyVisibleState();
        }

        IEnumerator HideRoutine()
        {
            var startAlpha = m_canvasGroup.alpha;
            var startPosition = m_animatedRect.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < ExitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / ExitDuration);
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                m_animatedRect.anchoredPosition = Vector2.Lerp(
                    startPosition,
                    m_restPosition - Vector2.right * 8f,
                    Smooth(t));
                yield return null;
            }

            m_transition = null;
            ApplyHiddenState(clearSprite: true);
        }

        IEnumerator EmotionRoutine()
        {
            var elapsed = 0f;
            while (elapsed < EmotionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / EmotionDuration);
                var pulse = Mathf.Sin(t * Mathf.PI);
                m_animatedRect.localScale = m_restScale * Mathf.Lerp(
                    m_talking ? TalkingScale : 1f,
                    EmotionScale,
                    pulse);
                yield return null;
            }

            m_transition = null;
            ApplyVisibleState();
        }

        bool EnsureInitialized()
        {
            if (m_initialized) return true;
            Initialize(m_image, m_animatedRect);
            return m_initialized;
        }

        void ApplyVisibleState()
        {
            if (!m_initialized) return;
            gameObject.SetActive(true);
            m_canvasGroup.alpha = 1f;
            m_animatedRect.anchoredPosition = m_restPosition;
            m_animatedRect.localScale = TargetScale();
            m_image.color = m_talking
                ? Color.white
                : new Color(0.93f, 0.93f, 0.96f, 1f);
        }

        void ApplyHiddenState(bool clearSprite)
        {
            if (!m_initialized) return;
            m_canvasGroup.alpha = 0f;
            m_animatedRect.anchoredPosition = m_restPosition;
            m_animatedRect.localScale = m_restScale;
            m_image.color = Color.white;
            if (clearSprite)
                m_image.sprite = null;
            gameObject.SetActive(false);
        }

        Vector3 TargetScale() => m_restScale * (m_talking ? TalkingScale : 1f);

        static float Smooth(float value) => value * value * (3f - 2f * value);

        void StopTransition()
        {
            if (m_transition == null) return;
            StopCoroutine(m_transition);
            m_transition = null;
        }

        void OnDisable()
        {
            if (m_transition != null)
            {
                StopCoroutine(m_transition);
                m_transition = null;
            }
        }
    }
}
