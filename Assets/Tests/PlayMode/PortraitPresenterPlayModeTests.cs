using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.NPC;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.UI;

namespace WalkingIntoNight.TRPG.Tests.PlayMode
{
    public class PortraitPresenterPlayModeTests
    {
        [UnityTest]
        public IEnumerator AnimationLab_RapidSwitchNarratorAndEmotion_EndInStableStateWhilePaused()
        {
            var frame = new GameObject("AnimationLabFrame", typeof(RectTransform), typeof(Image));
            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(frame.transform, false);
            var portraitRect = portrait.GetComponent<RectTransform>();
            var image = portrait.GetComponent<Image>();
            var presenter = frame.AddComponent<PortraitPresenter>();
            presenter.Initialize(image, portraitRect);
            var lab = frame.AddComponent<PortraitAnimationLab>();
            lab.Initialize(presenter);

            var originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                foreach (var portraitId in PortraitAnimationLab.PortraitIds)
                {
                    Assert.That(lab.Preview(portraitId), Is.True, portraitId);
                    yield return null;
                }

                for (var i = 0; i < 10; i++)
                {
                    var portraitId = PortraitAnimationLab.PortraitIds[i % PortraitAnimationLab.PortraitIds.Count];
                    Assert.That(lab.Preview(portraitId), Is.True, portraitId);
                    yield return null;
                }

                var finalId = PortraitAnimationLab.PortraitIds[9 % PortraitAnimationLab.PortraitIds.Count];
                yield return new WaitForSecondsRealtime(0.4f);

                Assert.That(presenter.IsTransitioning, Is.False);
                Assert.That(presenter.IsVisible, Is.True);
                Assert.That(presenter.CurrentSprite, Is.SameAs(PortraitDatabase.Get(finalId)));
                Assert.That(portraitRect.anchoredPosition, Is.EqualTo(presenter.RestPosition));

                Assert.That(lab.Preview(finalId), Is.True);
                Assert.That(presenter.IsTransitioning, Is.False,
                    "同一角色连续说话不应重新开始入场动画。");

                lab.Emphasize();
                Assert.That(presenter.IsTransitioning, Is.True);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That(presenter.IsTransitioning, Is.False);
                Assert.That(presenter.IsVisible, Is.True);

                lab.PreviewNarrator();
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(presenter.IsVisible, Is.False);
                Assert.That(frame.activeSelf, Is.False);

                Assert.That(lab.Preview("shop_cat_v1"), Is.True);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That(presenter.IsVisible, Is.True);
                Assert.That(presenter.CurrentSprite,
                    Is.SameAs(PortraitDatabase.Get("shop_cat_v1")));
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(frame);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayPortrait_NpcNarratorNpcAndEnding_UsePresenterCorrectly()
        {
            var state = GameStateManager.Instance;
            var stateObject = state != null
                ? state.gameObject
                : new GameObject("GameStateManager_PortraitPlayModeTest");
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
            var presenter = PrivateField<PortraitPresenter>("s_portraitPresenter");

            runner.AdvanceTo("npc_mei_talk");
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(presenter.CurrentSprite, Is.SameAs(PortraitDatabase.Get("mei_barista_v1")));
            Assert.That(presenter.IsVisible, Is.True);

            runner.AdvanceTo("npc_mei_talk");
            Assert.That(presenter.IsTransitioning, Is.False,
                "同一 NPC 连续两句不应重复闪烁。");

            runner.AdvanceTo("intro_01");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(presenter.IsVisible, Is.False);

            runner.AdvanceTo("npc_chen_talk");
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(presenter.CurrentSprite, Is.SameAs(PortraitDatabase.Get("chen_regular_v2")));
            Assert.That(presenter.IsVisible, Is.True);

            runner.AdvanceTo("end_neutral");
            yield return null;
            Assert.That(presenter.IsVisible, Is.False);

            if (UIRoot.Canvas != null)
                Object.Destroy(UIRoot.Canvas.gameObject);
            if (EventSystem.current != null)
                Object.Destroy(EventSystem.current.gameObject);
            Object.Destroy(stateObject);
            yield return null;
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
                Name = "头像演出测试调查员",
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
