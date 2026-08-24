using WalkingIntoNight.TRPG.Core;
using UnityEngine;

namespace WalkingIntoNight.TRPG.UI
{
    public static class MainMenuUI
    {
        public static void Build()
        {
            UIRoot.EnsureCanvas();

            var panel = UIBuilder.Panel("MainMenu", UIRoot.Layer,
                new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f), Vector2.zero, Vector2.zero);

            UIBuilder.CreateText(
                panel,
                "Title",
                ProductIdentity.ChineseDisplayName,
                48,
                TMPro.TextAlignmentOptions.Top);

            var feedback = UIBuilder.CreateText(
                panel,
                "Feedback",
                "",
                18,
                TMPro.TextAlignmentOptions.Center);
            feedback.rectTransform.anchorMin = new Vector2(0.08f, 0.02f);
            feedback.rectTransform.anchorMax = new Vector2(0.92f, 0.14f);
            feedback.rectTransform.offsetMin = Vector2.zero;
            feedback.rectTransform.offsetMax = Vector2.zero;

            var buttons = UIBuilder.VerticalLayout(panel, "Buttons", 12f);
            buttons.anchorMin = new Vector2(0.15f, 0.15f);
            buttons.anchorMax = new Vector2(0.85f, 0.65f);

            UIBuilder.CreateButton(buttons, "\u65b0\u6e38\u620f", () =>
            {
                GameStateManager.Instance.ResetForNewGame();
                SceneLoader.LoadCharacterCreate();
            });

            UIBuilder.CreateButton(buttons, "\u7ee7\u7eed", () =>
            {
                if (!SaveSystem.TryLoad(1, out var data, out var loadError))
                {
                    feedback.text = "\u7ee7\u7eed\u5931\u8d25\uff1a" + (loadError ?? "\u65e0\u6cd5\u8bfb\u53d6\u5b58\u6863\u3002");
                    return;
                }

                if (!GameStateManager.TryValidateSaveData(data, out var validationError))
                {
                    feedback.text = "\u7ee7\u7eed\u5931\u8d25\uff1a" + validationError;
                    return;
                }

                try
                {
                    GameStateManager.Instance.LoadFromSaveData(data);
                    GameStateManager.Instance.SaveSlot = 1;
                    SceneLoader.LoadGameplay();
                }
                catch (System.Exception ex)
                {
                    feedback.text = "\u7ee7\u7eed\u5931\u8d25\uff1a" + ex.Message;
                }
            }).interactable = SaveSystem.HasSave(1);

            UIBuilder.CreateButton(buttons, "\u9000\u51fa", Application.Quit);
        }
    }
}
