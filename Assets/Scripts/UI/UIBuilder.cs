using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WalkingIntoNight.TRPG.UI
{
    public static class UIBuilder
    {
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
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.92f, 0.9f, 0.85f);
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 48);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.28f, 0.35f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var text = CreateText(go.transform, "Label", label, 22, TextAlignmentOptions.Center);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return btn;
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
