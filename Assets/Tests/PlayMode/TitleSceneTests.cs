using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>[기능 1~4] 타이틀 씬: 제목 UI, 하단 버튼 2개, 나가기(비어 있음), 게임 시작 → InGame.</summary>
    public class TitleSceneTests
    {
        TitleMenu menu;

        [UnitySetUp]
        public IEnumerator LoadTitle()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Title, LoadSceneMode.Single);
            yield return null;
            menu = Object.FindFirstObjectByType<TitleMenu>();
            Assert.IsNotNull(menu, "Title 씬에 TitleMenu가 없다");
        }

        [TearDown]
        public void DestroyGameManager()
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
                Object.Destroy(gm.gameObject);
        }

        static Text FindText(string objectName)
        {
            var go = GameObject.Find(objectName);
            Assert.IsNotNull(go, $"{objectName} 오브젝트가 없다");
            var text = go.GetComponent<Text>();
            Assert.IsNotNull(text, $"{objectName}에 Text가 없다");
            return text;
        }

        [UnityTest]
        public IEnumerator F1_TitleText_IsOnCanvas()
        {
            yield return null;
            var title = FindText("TitleText");
            Assert.IsFalse(string.IsNullOrWhiteSpace(title.text));
            var canvas = title.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas, "제목이 Canvas 아래에 있어야 한다");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.IsNotNull(canvas.GetComponent<CanvasScaler>());
        }

        [UnityTest]
        public IEnumerator F2_StartAndExitButtons_AreBelowTitle()
        {
            yield return null;
            var title = FindText("TitleText").rectTransform;
            var start = menu.StartButton;
            var exit = menu.ExitButton;
            Assert.IsNotNull(start, "StartButton 연결 누락");
            Assert.IsNotNull(exit, "ExitButton 연결 누락");

            Assert.AreEqual("게임 시작", start.GetComponentInChildren<Text>().text);
            Assert.AreEqual("나가기", exit.GetComponentInChildren<Text>().text);

            float titleBottom = WorldRect(title).yMin;
            Assert.Less(WorldRect((RectTransform)start.transform).yMax, titleBottom, "게임 시작 버튼이 제목보다 아래에 있어야 한다");
            Assert.Less(WorldRect((RectTransform)exit.transform).yMax, titleBottom, "나가기 버튼이 제목보다 아래에 있어야 한다");
            Assert.Less(WorldRect((RectTransform)start.transform).center.y, Screen.height * 0.5f, "버튼은 화면 하단부에 있어야 한다");
            Assert.IsFalse(WorldRect((RectTransform)start.transform).Overlaps(WorldRect((RectTransform)exit.transform)), "두 버튼이 겹치면 안 된다");
        }

        [UnityTest]
        public IEnumerator F2_Buttons_ReceiveMouseRaycast()
        {
            yield return null;
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "EventSystem이 없으면 버튼을 클릭할 수 없다");
            Assert.IsNotNull(eventSystem.GetComponent<InputSystemUIInputModule>(), "Input System 전용 프로젝트이므로 InputSystemUIInputModule이 필요하다");

            foreach (var button in new[] { menu.StartButton, menu.ExitButton })
            {
                var data = new PointerEventData(eventSystem) { position = WorldRect((RectTransform)button.transform).center };
                var hits = new List<RaycastResult>();
                eventSystem.RaycastAll(data, hits);
                Assert.IsNotEmpty(hits, $"{button.name} 위치에 레이캐스트가 닿지 않는다");
                Assert.AreSame(button, hits[0].gameObject.GetComponentInParent<Button>(), $"{button.name}이 최상단 클릭 대상이 아니다");
            }
        }

        [UnityTest]
        public IEnumerator F3_ExitButton_DoesNothingYet()
        {
            menu.ExitButton.onClick.Invoke();
            for (int i = 0; i < 5; i++)
                yield return null;

            Assert.AreEqual(SceneNames.Title, SceneManager.GetActiveScene().name);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator F4_StartButton_LoadsInGameScene()
        {
            menu.StartButton.onClick.Invoke();
            // 연타해도 한 번만 처리되어야 한다.
            menu.StartButton.onClick.Invoke();

            float timeout = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.InGame && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.AreEqual(SceneNames.InGame, SceneManager.GetActiveScene().name);
            Assert.AreEqual(GameState.Play, GameManager.Instance.CurrentState);
            LogAssert.NoUnexpectedReceived();
        }

        static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
