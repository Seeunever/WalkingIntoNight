using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Dice;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.UI;

namespace WalkingIntoNight.TRPG.Tests.PlayMode
{
    public class CombatUiPlayModeTests
    {
        [SetUp]
        public void SetUp() => DialoguePresentationSettings.TypewriterEnabled = false;

        [TearDown]
        public void TearDown() => DialoguePresentationSettings.ResetDefaults();

        [UnityTest]
        public IEnumerator CombatUi_DodgeAttackAndFlee_AllCompleteThroughUiWithoutStaleButtons()
        {
            var state = GameStateManager.Instance;
            var stateObject = state != null
                ? state.gameObject
                : new GameObject("GameStateManager_CombatUiPlayModeTest");
            if (state == null)
                state = stateObject.AddComponent<GameStateManager>();
            state.ResetForNewGame();
            state.SetInvestigator(Investigator());

            GameplayUI.Build();
            yield return null;

            var checks = new Queue<bool>(new[] { false, true, true });
            var combat = new CombatManager(
                (skill, difficulty, skillId, bonusDice, penaltyDice) => new CheckResult
                {
                    ResultType = checks.Dequeue()
                        ? CheckResultType.RegularSuccess
                        : CheckResultType.Failure
                },
                (minimum, maximum) => maximum - 1);
            var runner = new ScenarioRunner(state, combatManager: combat);
            SetPrivateField("s_runner", runner);
            InvokePrivateMethod("WireEvents");
            runner.LoadScenario(state.CurrentScenarioId);

            var portraitFrame = PrivateField<RectTransform>("s_portraitFrame");
            var narrativeText = PrivateField<TMP_Text>("s_narrativeText");
            var choiceArea = PrivateField<RectTransform>("s_choiceArea");

            runner.AdvanceTo("npc_mei_talk");
            yield return null;
            Assert.That(portraitFrame.gameObject.activeSelf, Is.True);

            runner.AdvanceTo("combat_shadow_rat");
            yield return null;

            Assert.That(portraitFrame.gameObject.activeSelf, Is.False);
            Assert.That(narrativeText.text, Does.Contain("第 1 轮"));
            Assert.That(narrativeText.text, Does.Contain("影鼠"));
            Assert.That(narrativeText.text, Does.Contain("HP"));

            var firstButtons = choiceArea.GetComponentsInChildren<Button>(true);
            Assert.That(firstButtons, Has.Length.EqualTo(3));
            var dodgeButton = ButtonWithLabel(firstButtons, "闪避");

            dodgeButton.onClick.Invoke();
            yield return null;

            Assert.That(runner.Combat.State, Is.Not.Null);
            Assert.That(runner.Combat.State.turnNumber, Is.EqualTo(2));
            Assert.That(narrativeText.text, Does.Contain("第 2 轮"));
            Assert.That(choiceArea.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(3));
            Assert.That(firstButtons, Has.All.Matches<Button>(button => button == null));

            var attackButton = choiceArea.GetComponentsInChildren<Button>(true)
                .Single(button => button.GetComponentInChildren<TMP_Text>().text.StartsWith("攻击"));
            attackButton.onClick.Invoke();
            yield return null;

            Assert.That(runner.Combat.State, Is.Null);
            Assert.That(state.CurrentNodeId, Is.EqualTo("after_rat_win"));
            Assert.That(narrativeText.text, Does.Contain("击退了影鼠"));

            ButtonWithLabel(choiceArea.GetComponentsInChildren<Button>(true), "继续")
                .onClick.Invoke();
            yield return null;
            Assert.That(state.CurrentNodeId, Is.EqualTo("hub_explore"));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Exploration));

            runner.AdvanceTo("combat_shadow_rat");
            yield return null;
            var fleeButton = ButtonWithLabel(
                choiceArea.GetComponentsInChildren<Button>(true),
                "逃走");
            fleeButton.onClick.Invoke();
            yield return null;

            Assert.That(runner.Combat.State, Is.Null);
            Assert.That(state.CurrentNodeId, Is.EqualTo("hub_explore"));
            Assert.That(runner.InteractionMode, Is.EqualTo(ScenarioInteractionMode.Exploration));
            Assert.That(state.HasPendingCombatReturn, Is.False);

            if (UIRoot.Canvas != null)
                Object.Destroy(UIRoot.Canvas.gameObject);
            if (EventSystem.current != null)
                Object.Destroy(EventSystem.current.gameObject);
            Object.Destroy(stateObject);
            yield return null;
        }

        static Button ButtonWithLabel(IEnumerable<Button> buttons, string label)
        {
            return buttons.Single(button =>
                button.GetComponentInChildren<TMP_Text>().text == label);
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
                Name = "PlayMode 调查员",
                STR = 10,
                SIZ = 10,
                HP = 12,
                MaxHP = 12,
                SAN = 50,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            };
            investigator.Skills["fight"] = 60;
            investigator.Skills["dodge"] = 100;
            return investigator;
        }
    }
}
