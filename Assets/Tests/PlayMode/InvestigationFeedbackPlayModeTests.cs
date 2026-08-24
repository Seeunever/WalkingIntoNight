using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Dice;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.UI;

namespace WalkingIntoNight.TRPG.Tests.PlayMode
{
    public class InvestigationFeedbackPlayModeTests
    {
        [SetUp]
        public void SetUp() => DialoguePresentationSettings.TypewriterEnabled = false;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DialoguePresentationSettings.ResetDefaults();
            if (UIRoot.Canvas != null)
                Object.Destroy(UIRoot.Canvas.gameObject);
            if (EventSystem.current != null)
                Object.Destroy(EventSystem.current.gameObject);
            if (GameStateManager.Instance != null)
                Object.Destroy(GameStateManager.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedChoicesShowReasons_AndConsumableButtonUsesConfiguredEffect()
        {
            var stateObject = new GameObject("GameStateManager_InvestigationFeedbackPlayModeTest");
            var state = stateObject.AddComponent<GameStateManager>();
            state.ResetForNewGame();
            state.SetInvestigator(new Investigator
            {
                Name = "调查反馈测试员",
                HP = 6,
                MaxHP = 10,
                SAN = 40,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            });
            state.Inventory.AddItem("first_aid_kit");

            GameplayUI.Build();
            yield return null;

            var runner = new ScenarioRunner(
                state,
                realtimeProvider: () => 10f,
                skillCheck: (skill, difficulty, skillId, bonusDice, penaltyDice) =>
                    new CheckResult { ResultType = CheckResultType.RegularSuccess });
            SetPrivateField("s_runner", runner);
            InvokePrivateMethod("WireEvents");
            runner.LoadScenario(ScenarioRegistry.DefaultScenarioId);
            runner.StartFrom("hub_explore");
            yield return null;

            var basement = ButtonStartingWith("深入地下室");
            var basementLabel = basement.GetComponentInChildren<TMP_Text>().text;
            Assert.That(basement.interactable, Is.False);
            Assert.That(basementLabel, Does.Contain("未解锁：需要先在储藏室找到生锈的钥匙。"));

            var useKit = ButtonWithLabel("使用 急救包");
            Assert.That(useKit.interactable, Is.True);
            useKit.onClick.Invoke();
            yield return null;

            Assert.That(state.Investigator.HP, Is.EqualTo(9));
            Assert.That(state.Inventory.HasItem("first_aid_kit"), Is.False);
            Assert.That(PrivateField<TMP_Text>("s_logText").text,
                Does.Contain("【使用物品】急救包：HP +3"));
            Assert.That(ButtonLabels(), Does.Not.Contain("使用 急救包"));

            state.Inventory.AddItem("rusty_key");
            InvokePrivateMethod("RefreshNarrativeActions");
            yield return null;

            basement = ButtonWithLabel("深入地下室（需钥匙）");
            Assert.That(basement.interactable, Is.True);

            runner.AdvanceTo("final_choice");
            yield return null;

            var ritual = ButtonStartingWith("用符号拓片打断仪式");
            var ritualLabel = ritual.GetComponentInChildren<TMP_Text>().text;
            Assert.That(ritual.interactable, Is.False);
            Assert.That(ritualLabel, Does.Contain("未解锁：需要先读懂地下室墙上的仪式符号。"));
        }

        static Button ButtonWithLabel(string label)
        {
            return UIRoot.Layer.GetComponentsInChildren<Button>(true).Single(button =>
                button.GetComponentInChildren<TMP_Text>().text == label);
        }

        static Button ButtonStartingWith(string prefix)
        {
            return UIRoot.Layer.GetComponentsInChildren<Button>(true).Single(button =>
                button.GetComponentInChildren<TMP_Text>().text.StartsWith(prefix));
        }

        static string[] ButtonLabels()
        {
            return UIRoot.Layer.GetComponentsInChildren<Button>(true)
                .Select(button => button.GetComponentInChildren<TMP_Text>().text)
                .ToArray();
        }

        static T PrivateField<T>(string name) where T : class
        {
            var field = typeof(GameplayUI).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"缺少 GameplayUI.{name} 测试入口。");
            return field.GetValue(null) as T;
        }

        static void SetPrivateField(string name, object value)
        {
            var field = typeof(GameplayUI).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"缺少 GameplayUI.{name} 测试入口。");
            field.SetValue(null, value);
        }

        static void InvokePrivateMethod(string name)
        {
            var method = typeof(GameplayUI).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"缺少 GameplayUI.{name} 测试入口。");
            method.Invoke(null, null);
        }
    }
}
