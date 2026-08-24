using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Dice;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.UI;

namespace WalkingIntoNight.TRPG.Tests.PlayMode
{
    public class ScenarioWalkthroughPlayModeTests
    {
        [SetUp]
        public void SetUp() => DialoguePresentationSettings.TypewriterEnabled = false;

        [TearDown]
        public void TearDown() => DialoguePresentationSettings.ResetDefaults();

        [UnityTest]
        public IEnumerator Scenario01_NeutralEndingThenNewGame_ClearsPreviousRunState()
        {
            var state = GameStateManager.Instance;
            var stateObject = state != null
                ? state.gameObject
                : new GameObject("GameStateManager_WalkthroughPlayModeTest");
            if (state == null)
                state = stateObject.AddComponent<GameStateManager>();
            state.ResetForNewGame();
            state.SetInvestigator(Investigator());
            state.Inventory.AddItem("notebook");
            state.Inventory.AddItem("flashlight");

            GameplayUI.Build();
            yield return null;

            var now = 0f;
            var runner = new ScenarioRunner(
                state,
                realtimeProvider: () => now,
                skillCheck: (skill, difficulty, skillId, bonusDice, penaltyDice) =>
                    new CheckResult { ResultType = CheckResultType.RegularSuccess });
            SetPrivateField("s_runner", runner);
            InvokePrivateMethod("WireEvents");
            runner.LoadScenario(state.CurrentScenarioId);
            runner.StartFrom(null);
            yield return null;

            var choiceArea = PrivateField<RectTransform>("s_choiceArea");
            var narrativeText = PrivateField<TMP_Text>("s_narrativeText");

            Click(choiceArea, "继续", ref now);
            yield return null;
            Click(choiceArea, "有人求救，我不想假装没看见。", ref now);
            yield return null;
            Assert.That(state.HasFlag("motive_kindness"), Is.True);

            Click(choiceArea, "继续", ref now);
            yield return null;
            Click(choiceArea, "继续", ref now);
            yield return null;
            Click(choiceArea, "继续", ref now);
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("intro_03"));
            Assert.That(state.HasFlag("cat_mirror"), Is.True);
            Click(choiceArea, "观察大厅环境", ref now);
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("spot_success"));
            Click(choiceArea, "继续", ref now);
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("intro_hall_threads"));
            Click(choiceArea, "先自己看看", ref now);
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("hub_explore"));
            Assert.That(state.Inventory.HasItem("silver_coin"), Is.True);
            Assert.That(state.HasFlag("found_coin"), Is.True);

            Click(choiceArea, "推进：午夜钟声", ref now);
            yield return null;
            Click(choiceArea, "继续", ref now);
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("final_choice"));
            Click(choiceArea, "举银币对准镜面（需发现银币）", ref now);
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("end_neutral"));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.End));
            Assert.That(narrativeText.text, Does.Contain("银币吸收镜面波纹"));
            Assert.That(ButtonLabels(choiceArea), Is.EqualTo(new[] { "返回主菜单" }));

            ButtonWithLabel(choiceArea, "返回主菜单").onClick.Invoke();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
            Assert.That(ButtonLabels(UIRoot.Layer), Does.Contain("新游戏"));

            ButtonWithLabel(UIRoot.Layer, "新游戏").onClick.Invoke();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.CharacterCreate));

            Assert.That(state.CurrentNodeId, Is.Null);
            Assert.That(state.Investigator, Is.Null);
            Assert.That(state.Flags, Is.Empty);
            Assert.That(state.Inventory.Items, Is.Empty);
            Assert.That(state.ActiveCombat, Is.Null);
            Assert.That(state.HasPendingCombatReturn, Is.False);
            Assert.That(state.PostCombatNodeId, Is.Null);

            if (UIRoot.Canvas != null)
                Object.Destroy(UIRoot.Canvas.gameObject);
            if (EventSystem.current != null)
                Object.Destroy(EventSystem.current.gameObject);
            Object.Destroy(stateObject);
            yield return null;
        }

        static void Click(RectTransform choiceArea, string label, ref float now)
        {
            now += 1f;
            var button = choiceArea.GetComponentsInChildren<Button>(true).Single(candidate =>
                candidate.GetComponentInChildren<TMP_Text>().text == label);
            button.onClick.Invoke();
        }

        static string[] ButtonLabels(RectTransform choiceArea)
        {
            return choiceArea.GetComponentsInChildren<Button>(true)
                .Select(button => button.GetComponentInChildren<TMP_Text>().text)
                .ToArray();
        }

        static Button ButtonWithLabel(Transform root, string label)
        {
            return root.GetComponentsInChildren<Button>(true).Single(candidate =>
                candidate.GetComponentInChildren<TMP_Text>().text == label);
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

        static Investigator Investigator()
        {
            var investigator = new Investigator
            {
                Name = "第一章通关测试调查员",
                STR = 10,
                SIZ = 10,
                HP = 12,
                MaxHP = 12,
                SAN = 50,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            };
            investigator.Skills["spot_hidden"] = 80;
            investigator.Skills["psychology"] = 80;
            return investigator;
        }
    }
}
