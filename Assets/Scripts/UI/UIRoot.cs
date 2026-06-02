using UnityEngine;

namespace WalkingIntoNight.TRPG.UI
{
    public static class UIRoot
    {
        public static Canvas Canvas { get; private set; }
        public static RectTransform Layer { get; private set; }

        public static void EnsureCanvas()
        {
            if (Canvas != null) return;

            var go = new GameObject("TRPG_Canvas");
            Canvas = go.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var layerGo = new GameObject("Layer");
            layerGo.transform.SetParent(go.transform, false);
            Layer = layerGo.AddComponent<RectTransform>();
            Layer.anchorMin = Vector2.zero;
            Layer.anchorMax = Vector2.one;
            Layer.offsetMin = Vector2.zero;
            Layer.offsetMax = Vector2.zero;

            Object.DontDestroyOnLoad(go);
        }

        public static void Clear()
        {
            if (Layer == null) return;
            for (var i = Layer.childCount - 1; i >= 0; i--)
                Object.Destroy(Layer.GetChild(i).gameObject);
        }
    }
}
