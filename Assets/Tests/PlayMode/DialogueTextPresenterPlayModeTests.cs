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
    public class DialogueTextPresenterPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            DialoguePresentationSettings.ResetDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            DialoguePresentationSettings.ResetDefaults();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Presenter_CancelsOldRevealAndUsesUnscaledTimeWithAdvanceGuard()
        {
            var canvasObject = new GameObject("DialoguePresenterCanvas", typeof(Canvas));
            var textObject = new GameObject("Dialogue", typeof(RectTransform));
            textObject.transform.SetParent(canvasObject.transform, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            var presenter = textObject.AddComponent<DialogueTextPresenter>();
            presenter.Initialize(text);
            DialoguePresentationSettings.CharactersPerSecond = 10f;
            DialoguePresentationSettings.RevealAdvanceGuardSeconds = 0.2f;
            var oldCompletions = 0;
            var newCompletions = 0;
            var originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            try
            {
                presenter.Present("abcdefghij", () => oldCompletions++);
                yield return new WaitForSecondsRealtime(0.12f);
                Assert.That(presenter.IsRevealing, Is.True);

                presenter.Present("新文本", () => newCompletions++);
                Assert.That(oldCompletions, Is.EqualTo(0), "切换节点必须取消旧回调。");
                Assert.That(presenter.RevealImmediately(), Is.True);
                Assert.That(newCompletions, Is.EqualTo(1));
                Assert.That(presenter.CanAdvance, Is.False);

                yield return new WaitForSecondsRealtime(0.22f);
                Assert.That(presenter.CanAdvance, Is.True);

                DialoguePresentationSettings.CharactersPerSecond = 100f;
                presenter.Present("自然完成");
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(presenter.IsRevealing, Is.False);
                Assert.That(presenter.CanAdvance, Is.True);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(canvasObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayContinue_FirstClickRevealsSecondAdvancesAndChoicesWaitForText()
        {
            DialoguePresentationSettings.CharactersPerSecond = 1f;
            DialoguePresentationSettings.RevealAdvanceGuardSeconds = 0.2f;
            var state = GameStateManager.Instance;
            var stateObject = state != null
                ? state.gameObject
                : new GameObject("GameStateManager_DialoguePlayModeTest");
            if (state == null)
                state = stateObject.AddComponent<GameStateManager>();
            state.ResetForNewGame();
            state.SetInvestigator(Investigator());

            GameplayUI.Build();
            yield return null;

            var runner = new ScenarioRunner(state);
            SetPrivateField("s_runner", runner);
            InvokePrivateMethod("WireEvents");
            runner.LoadScenario(state.CurrentScenarioId);
            runner.StartFrom("intro_02");
            yield return null;

            var choiceArea = PrivateField<RectTransform>("s_choiceArea");
            var presenter = PrivateField<DialogueTextPresenter>("s_dialoguePresenter");
            Assert.That(presenter.IsRevealing, Is.True);
            var firstRevealButton = ButtonWithLabel(choiceArea, "继续");

            firstRevealButton.onClick.Invoke();
            firstRevealButton.onClick.Invoke();
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("intro_02"));
            Assert.That(presenter.IsRevealing, Is.False);

            yield return new WaitForSecondsRealtime(0.22f);
            ButtonWithLabel(choiceArea, "继续").onClick.Invoke();
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("intro_cat_mirror"));
            Assert.That(presenter.IsRevealing, Is.True);
            firstRevealButton.onClick.Invoke();
            Assert.That(presenter.IsRevealing, Is.True, "旧节点按钮不得补全新节点文本。");

            var secondRevealButton = ButtonWithLabel(choiceArea, "继续");
            secondRevealButton.onClick.Invoke();
            secondRevealButton.onClick.Invoke();
            Assert.That(state.CurrentNodeId, Is.EqualTo("intro_cat_mirror"));

            yield return new WaitForSecondsRealtime(0.8f);
            ButtonWithLabel(choiceArea, "继续").onClick.Invoke();
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("intro_03"));
            Assert.That(presenter.IsRevealing, Is.True);
            Assert.That(ButtonLabels(choiceArea), Is.EqualTo(new[] { "继续" }));

            ButtonWithLabel(choiceArea, "继续").onClick.Invoke();
            yield return null;

            Assert.That(presenter.IsRevealing, Is.False);
            Assert.That(ButtonLabels(choiceArea), Does.Contain("出示匿名邮件"));
            Assert.That(ButtonLabels(choiceArea), Does.Contain("先问第三只杯子给谁"));
            Assert.That(ButtonLabels(choiceArea), Does.Contain("观察大厅环境"));

            if (UIRoot.Canvas != null)
                Object.Destroy(UIRoot.Canvas.gameObject);
            if (EventSystem.current != null)
                Object.Destroy(EventSystem.current.gameObject);
            Object.Destroy(stateObject);
            yield return null;
        }

        static Button ButtonWithLabel(RectTransform root, string label)
        {
            return root.GetComponentsInChildren<Button>(true).Single(button =>
                button.GetComponentInChildren<TMP_Text>().text == label);
        }

        static string[] ButtonLabels(RectTransform root)
        {
            return root.GetComponentsInChildren<Button>(true)
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

        static Investigator Investigator()
        {
            return new Investigator
            {
                Name = "逐字演出测试调查员",
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
