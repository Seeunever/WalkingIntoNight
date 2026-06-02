using AnimalCafe.TRPG.Core;
using UnityEngine;

namespace AnimalCafe.TRPG.UI
{
    public static class MainMenuUI
    {
        public static void Build()
        {
            UIRoot.EnsureCanvas();

            var panel = UIBuilder.Panel("MainMenu", UIRoot.Layer,
                new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f),
                Vector2.zero, Vector2.zero);

            UIBuilder.CreateText(panel, "Title", "克苏鲁式跑团\n咖啡馆关店后的失踪", 42, TMPro.TextAlignmentOptions.Center);

            var buttons = UIBuilder.VerticalLayout(panel, "Buttons");
            buttons.anchorMin = new Vector2(0.15f, 0.1f);
            buttons.anchorMax = new Vector2(0.85f, 0.45f);

            UIBuilder.CreateButton(buttons, "新游戏", () =>
            {
                GameStateManager.Instance.ResetForNewGame();
                SceneLoader.LoadCharacterCreate();
            });

            UIBuilder.CreateButton(buttons, "继续游戏（槽位1）", () =>
            {
                if (!SaveSystem.HasSave(0))
                {
                    Debug.Log("无存档");
                    return;
                }

                var data = SaveSystem.Load(0);
                GameStateManager.Instance.ResetForNewGame();
                GameStateManager.Instance.LoadFromSaveData(data);
                GameStateManager.Instance.SaveSlot = 0;
                SceneLoader.LoadGameplay();
            });

            UIBuilder.CreateButton(buttons, "退出", Application.Quit);
        }
    }
}
