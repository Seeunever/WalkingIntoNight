using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WalkingIntoNight.TRPG.UI
{
    public sealed class UIScrollView
    {
        public RectTransform Root { get; }
        public RectTransform Viewport { get; }
        public RectTransform Content { get; }
        public ScrollRect ScrollRect { get; }

        public UIScrollView(
            RectTransform root,
            RectTransform viewport,
            RectTransform content,
            ScrollRect scrollRect)
        {
            Root = root;
            Viewport = viewport;
            Content = content;
            ScrollRect = scrollRect;
        }
    }

    public static class UIBuilder
    {
        static TMP_FontAsset s_runtimeFont;

        public static RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.color = color ?? new Color(0.08f, 0.08f, 0.12f, 0.92f);
            return rt;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, int fontSize,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16, 16);
            rt.offsetMax = new Vector2(-16, -16);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = GetRuntimeFont();
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.92f, 0.9f, 0.85f);
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        static TMP_FontAsset GetRuntimeFont()
        {
            if (s_runtimeFont != null) return s_runtimeFont;

            var sourceFont = Resources.Load<Font>("Fonts/NotoSansSC-VF");
            if (sourceFont != null)
            {
                s_runtimeFont = TMP_FontAsset.CreateFontAsset(sourceFont);
                if (s_runtimeFont != null)
                    s_runtimeFont.name = sourceFont.name + " TMP Runtime";
            }

            if (s_runtimeFont == null)
                s_runtimeFont = TMP_Settings.defaultFontAsset;

            return s_runtimeFont;
        }

        public static Button CreateButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var multiline = !string.IsNullOrEmpty(label) && label.Contains("\n");
            rt.sizeDelta = new Vector2(400, multiline ? 68 : 48);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.28f, 0.35f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var text = CreateText(go.transform, "Label", label, multiline ? 18 : 22, TextAlignmentOptions.Center);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return btn;
        }

        public static UIScrollView ScrollableVerticalLayout(
            Transform parent,
            string name,
            float spacing = 8f,
            int padding = 8)
        {
            var rootGo = new GameObject(name);
            rootGo.transform.SetParent(parent, false);
            var root = rootGo.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(root, false);
            var viewport = viewportGo.AddComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewport, false);
            var content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = rootGo.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.verticalNormalizedPosition = 1f;

            return new UIScrollView(root, viewport, content, scrollRect);
        }

        public static RectTransform VerticalLayout(Transform parent, string name, float spacing = 8f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(8, 8, 8, 8);
            return rt;
        }
    }
}
