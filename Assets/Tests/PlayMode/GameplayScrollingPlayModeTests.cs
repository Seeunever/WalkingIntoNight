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
using WalkingIntoNight.TRPG.Core;
using WalkingIntoNight.TRPG.Narrative;
using WalkingIntoNight.TRPG.UI;

namespace WalkingIntoNight.TRPG.Tests.PlayMode
{
    public class GameplayScrollingPlayModeTests
    {
        int m_originalWidth;
        int m_originalHeight;
        bool m_originalFullscreen;

        [SetUp]
        public void SetUp()
        {
            DialoguePresentationSettings.TypewriterEnabled = false;
            m_originalWidth = Screen.width;
            m_originalHeight = Screen.height;
            m_originalFullscreen = Screen.fullScreen;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DialoguePresentationSettings.ResetDefaults();
            Screen.SetResolution(m_originalWidth, m_originalHeight, m_originalFullscreen);
            if (UIRoot.Canvas != null)
                Object.Destroy(UIRoot.Canvas.gameObject);
            if (EventSystem.current != null)
                Object.Destroy(EventSystem.current.gameObject);
            if (GameStateManager.Instance != null)
                Object.Destroy(GameStateManager.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LongContent_UsesClippedScrollViewsAcrossTargetResolutions()
        {
            var stateObject = new GameObject("GameStateManager_GameplayScrollingPlayModeTest");
            var state = stateObject.AddComponent<GameStateManager>();
            state.ResetForNewGame();
            state.SetInvestigator(Investigator());
            foreach (var itemId in new[]
            {
                "flashlight",
                "notebook",
                "rusty_key",
                "first_aid_kit",
                "calming_tea",
                "silver_coin",
                "owner_diary"
            })
                state.Inventory.AddItem(itemId);

            GameplayUI.Build();
            yield return null;

            var runner = new ScenarioRunner(state, realtimeProvider: () => 10f);
            SetPrivateField("s_runner", runner);
            InvokePrivateMethod("WireEvents");
            runner.LoadScenario(LongScenario());
            runner.StartFrom("long_content");
            var sidebarContent = PrivateField<RectTransform>("s_sidebarContent");
            for (var i = 0; i < 14; i++)
                UIBuilder.CreateButton(sidebarContent, $"额外调查入口 {i + 1}", null);
            for (var i = 0; i < 32; i++)
                InvokePrivateMethod("AppendLog", $"第 {i + 1} 条调查日志：潮湿脚印仍在向地下室延伸。");
            yield return null;

            var scrollRects = new[]
            {
                PrivateField<ScrollRect>("s_narrativeScroll"),
                PrivateField<ScrollRect>("s_choiceScroll"),
                PrivateField<ScrollRect>("s_sidebarScroll"),
                PrivateField<ScrollRect>("s_logScroll")
            };
            foreach (var scrollRect in scrollRects)
            {
                Assert.That(scrollRect, Is.Not.Null);
                Assert.That(scrollRect.vertical, Is.True);
                Assert.That(scrollRect.horizontal, Is.False);
                Assert.That(scrollRect.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            }

            var resolutions = new[]
            {
                new Vector2Int(1920, 1080),
                new Vector2Int(1600, 900),
                new Vector2Int(1280, 720),
                new Vector2Int(1280, 800),
                new Vector2Int(1024, 640)
            };
            foreach (var resolution in resolutions)
            {
                Screen.SetResolution(resolution.x, resolution.y, false);
                yield return null;
                Canvas.ForceUpdateCanvases();

                var gameplay = UIRoot.Layer.Find("Gameplay") as RectTransform;
                Assert.That(gameplay, Is.Not.Null);
                foreach (var panelName in new[] { "Header", "Sidebar", "Main", "Log" })
                {
                    var panel = gameplay.Find(panelName) as RectTransform;
                    Assert.That(panel, Is.Not.Null, panelName);
                    AssertContained(gameplay, panel, $"{panelName} 越出 {resolution.x}×{resolution.y} 画面。");
                }

                foreach (var scrollRect in scrollRects)
                    AssertContained(gameplay, scrollRect.viewport,
                        $"{scrollRect.name} 越出 {resolution.x}×{resolution.y} 画面。");
            }

            Canvas.ForceUpdateCanvases();
            foreach (var scrollRect in scrollRects)
            {
                Assert.That(scrollRect.content.rect.height,
                    Is.GreaterThan(scrollRect.viewport.rect.height),
                    $"{scrollRect.name} 的长内容没有形成可滚动区域。");
                scrollRect.verticalNormalizedPosition = 1f;
                var scrollEvent = new PointerEventData(EventSystem.current)
                {
                    scrollDelta = new Vector2(0f, -6f)
                };
                scrollRect.OnScroll(scrollEvent);
                Assert.That(scrollRect.verticalNormalizedPosition, Is.LessThan(1f),
                    $"{scrollRect.name} 没有响应滚轮输入。");
            }

            var choiceScroll = PrivateField<ScrollRect>("s_choiceScroll");
            choiceScroll.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var lastChoice = choiceScroll.content.GetComponentsInChildren<Button>(true).Single(button =>
                button.GetComponentInChildren<TMP_Text>().text == "调查行动 12");
            Assert.That(lastChoice.interactable, Is.True);
            AssertOverlaps(choiceScroll.viewport, lastChoice.GetComponent<RectTransform>(),
                "滚到底部后，最后一个剧情选项仍应出现在可点击区域。" );
            lastChoice.onClick.Invoke();
            yield return null;

            Assert.That(state.CurrentNodeId, Is.EqualTo("ending"));
        }

        static ScenarioFile LongScenario()
        {
            var choices = Enumerable.Range(1, 12)
                .Select(index => new StoryChoiceData
                {
                    text = $"调查行动 {index}",
                    nextNodeId = "ending"
                })
                .ToList();
            return new ScenarioFile
            {
                scenarioId = "scroll-test",
                startNodeId = "long_content",
                nodes = new List<StoryNodeData>
                {
                    new StoryNodeData
                    {
                        id = "long_content",
                        type = "location",
                        allowExploration = true,
                        speaker = "旁白",
                        text = string.Concat(Enumerable.Repeat(
                            "雨水敲打玻璃，调查笔记上密密麻麻写满了失踪者留下的时间、地点与互相矛盾的证词。",
                            28)),
                        choices = choices
                    },
                    new StoryNodeData
                    {
                        id = "ending",
                        type = "end",
                        speaker = "结局",
                        text = "滚动测试完成。"
                    }
                }
            };
        }

        static void AssertContained(RectTransform outer, RectTransform inner, string message)
        {
            var outerCorners = new Vector3[4];
            var innerCorners = new Vector3[4];
            outer.GetWorldCorners(outerCorners);
            inner.GetWorldCorners(innerCorners);
            var bounds = new Rect(
                outerCorners[0].x,
                outerCorners[0].y,
                outerCorners[2].x - outerCorners[0].x,
                outerCorners[2].y - outerCorners[0].y);
            foreach (var corner in innerCorners)
                Assert.That(bounds.Contains(corner) || OnBoundary(bounds, corner), Is.True, message);
        }

        static void AssertOverlaps(RectTransform viewport, RectTransform element, string message)
        {
            var viewportCorners = new Vector3[4];
            var elementCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);
            element.GetWorldCorners(elementCorners);
            var viewportRect = Rect.MinMaxRect(
                viewportCorners[0].x,
                viewportCorners[0].y,
                viewportCorners[2].x,
                viewportCorners[2].y);
            var elementRect = Rect.MinMaxRect(
                elementCorners[0].x,
                elementCorners[0].y,
                elementCorners[2].x,
                elementCorners[2].y);
            Assert.That(viewportRect.Overlaps(elementRect), Is.True, message);
        }

        static bool OnBoundary(Rect rect, Vector3 point)
        {
            const float tolerance = 0.5f;
            return point.x >= rect.xMin - tolerance && point.x <= rect.xMax + tolerance &&
                point.y >= rect.yMin - tolerance && point.y <= rect.yMax + tolerance;
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

        static void InvokePrivateMethod(string name, params object[] arguments)
        {
            var method = typeof(GameplayUI).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"缺少 GameplayUI.{name} 测试入口。");
            method.Invoke(null, arguments);
        }

        static Investigator Investigator()
        {
            return new Investigator
            {
                Name = "长内容测试调查员",
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
