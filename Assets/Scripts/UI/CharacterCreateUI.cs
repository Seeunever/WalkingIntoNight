using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using TMPro;
using UnityEngine;

namespace WalkingIntoNight.TRPG.UI
{
    public static class CharacterCreateUI
    {
        static TMP_Text s_statsText;
        static TMP_InputField s_nameField;
        static Investigator s_preview;

        public static void Build()
        {
            UIRoot.EnsureCanvas();

            var panel = UIBuilder.Panel("CharCreate", UIRoot.Layer,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);

            UIBuilder.CreateText(panel, "Title", "\u89d2\u8272\u521b\u5efa", 36, TextAlignmentOptions.Top);

            s_nameField = CreateInput(panel, "\u8c03\u67e5\u5458\u59d3\u540d");
            s_statsText = UIBuilder.CreateText(panel, "Stats", "", 22);
            s_statsText.rectTransform.anchorMin = new Vector2(0.05f, 0.25f);
            s_statsText.rectTransform.anchorMax = new Vector2(0.95f, 0.75f);

            var buttons = UIBuilder.VerticalLayout(panel, "Buttons");
            buttons.anchorMin = new Vector2(0.3f, 0.02f);
            buttons.anchorMax = new Vector2(0.7f, 0.22f);

            UIBuilder.CreateButton(buttons, "\u91cd\u65b0\u6eda\u70b9", () =>
            {
                s_preview = CharacterCreator.RollRandom(GetName());
                RefreshStats();
            });

            UIBuilder.CreateButton(buttons, "\u5f00\u59cb\u8c03\u67e5", () =>
            {
                if (s_preview == null)
                    s_preview = CharacterCreator.RollRandom(GetName());

                s_preview.Name = GetName();
                GameStateManager.Instance.SetInvestigator(s_preview);
                GameStateManager.Instance.Inventory.AddItem("notebook");
                GameStateManager.Instance.Inventory.AddItem("flashlight");
                SceneLoader.LoadGameplay();
            });

            UIBuilder.CreateButton(buttons, "\u8fd4\u56de", SceneLoader.LoadMainMenu);

            s_preview = CharacterCreator.RollRandom("\u8c03\u67e5\u5458");
            RefreshStats();
        }

        static string GetName()
        {
            return s_nameField != null && !string.IsNullOrWhiteSpace(s_nameField.text)
                ? s_nameField.text
                : "\u8c03\u67e5\u5458";
        }

        static void RefreshStats()
        {
            if (s_preview == null || s_statsText == null) return;
            s_statsText.text =
                $"{s_preview.Name}\n" +
                $"STR {s_preview.STR}  CON {s_preview.CON}  POW {s_preview.POW}  DEX {s_preview.DEX}\n" +
                $"APP {s_preview.APP}  INT {s_preview.INT}  EDU {s_preview.EDU}  SIZ {s_preview.SIZ}\n" +
                $"HP {s_preview.HP}/{s_preview.MaxHP}  SAN {s_preview.SAN}/{s_preview.MaxSAN}  MP {s_preview.MP}/{s_preview.MaxMP}\n\n" +
                $"\u4fa6\u67e5 {s_preview.GetSkill("spot_hidden")}  " +
                $"\u804a\u542c {s_preview.GetSkill("listen")}  " +
                $"\u6587\u732e {s_preview.GetSkill("library_use")}\n" +
                $"\u5fc3\u7406 {s_preview.GetSkill("psychology")}  " +
                $"\u8bf4\u670d {s_preview.GetSkill("persuade")}  " +
                $"\u683c\u6597 {s_preview.GetSkill("fight")}";
        }

        static TMP_InputField CreateInput(Transform parent, string placeholder)
        {
            var go = new GameObject("NameInput");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.78f);
            rt.anchorMax = new Vector2(0.8f, 0.86f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var input = go.AddComponent<TMP_InputField>();
            var text = UIBuilder.CreateText(go.transform, "Text", "", 24, TextAlignmentOptions.MidlineLeft);
            input.textComponent = text;

            var placeholderText = UIBuilder.CreateText(go.transform, "Placeholder", placeholder, 24,
                TextAlignmentOptions.MidlineLeft);
            placeholderText.color = new Color(0.65f, 0.65f, 0.7f, 0.75f);
            placeholderText.fontStyle = FontStyles.Italic;
            input.placeholder = placeholderText;
            return input;
        }
    }
}
