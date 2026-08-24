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
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.UI;

namespace WalkingIntoNight.TRPG.Tests.PlayMode
{
    public class LocationAccessPlayModeTests
    {
        [SetUp]
        public void SetUp() => DialoguePresentationSettings.TypewriterEnabled = false;

        [TearDown]
        public void TearDown() => DialoguePresentationSettings.ResetDefaults();

        [UnityTest]
        public IEnumerator Sidebar_KeyGateAndNightSchedule_UpdateImmediately()
        {
            var state = GameStateManager.Instance;
            var stateObject = state != null
                ? state.gameObject
                : new GameObject("GameStateManager_LocationAccessPlayModeTest");
            if (state == null)
                state = stateObject.AddComponent<GameStateManager>();
            state.ResetForNewGame();
            state.SetInvestigator(Investigator());
            state.SetTime(1, TimePeriod.Evening);

            GameplayUI.Build();
            yield return null;

            var runner = new ScenarioRunner(state);
            SetPrivateField("s_runner", runner);
            InvokePrivateMethod("WireEvents");
            runner.LoadScenario(state.CurrentScenarioId);
            runner.AdvanceTo("hub_explore");
            yield return null;

            var lockedBasement = ButtonWithLabel("地下室（需要「生锈的钥匙」）");
            Assert.That(lockedBasement.interactable, Is.False);

            state.Inventory.AddItem("rusty_key");
            InvokePrivateMethod("RefreshInteractionControls");
            yield return null;

            var unlockedBasement = ButtonWithLabel("地下室");
            Assert.That(unlockedBasement.interactable, Is.True);
            unlockedBasement.onClick.Invoke();
            yield return null;

            Assert.That(state.CurrentLocationId, Is.EqualTo("cafe_basement"));
            Assert.That(ButtonLabels(), Does.Contain("[当前] 地下室"));
            Assert.That(ButtonLabels(), Does.Contain("老板的影子"));
            Assert.That(ButtonLabels(), Does.Not.Contain("黑衣女人"));

            ButtonWithLabel("下一时段").onClick.Invoke();
            yield return null;

            Assert.That(state.CurrentTime.day, Is.EqualTo(1));
            Assert.That(state.CurrentTime.period, Is.EqualTo(TimePeriod.Night));
            Assert.That(ButtonLabels(), Does.Contain("黑衣女人"));

            ButtonWithLabel("下一天").onClick.Invoke();
            yield return null;

            Assert.That(state.CurrentTime.day, Is.EqualTo(2));
            Assert.That(state.CurrentTime.period, Is.EqualTo(TimePeriod.Morning));
            Assert.That(ButtonLabels(), Does.Not.Contain("黑衣女人"));

            if (UIRoot.Canvas != null)
                Object.Destroy(UIRoot.Canvas.gameObject);
            if (EventSystem.current != null)
                Object.Destroy(EventSystem.current.gameObject);
            Object.Destroy(stateObject);
            yield return null;
        }

        static Button ButtonWithLabel(string label)
        {
            return UIRoot.Layer.GetComponentsInChildren<Button>(true).Single(button =>
                button.GetComponentInChildren<TMP_Text>().text == label);
        }

        static string[] ButtonLabels()
        {
            return UIRoot.Layer.GetComponentsInChildren<Button>(true)
                .Select(button => button.GetComponentInChildren<TMP_Text>().text)
                .ToArray();
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

        static Investigator Investigator()
        {
            return new Investigator
            {
                Name = "侧栏地点测试调查员",
                HP = 12,
                MaxHP = 12,
                SAN = 50,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            };
        }
    }
}
