using System.Collections;
using Game.Core;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// 게임 흐름 관리자는 씬 진입점(GameBootstrap)만 만들고, 나머지는 Current로 읽기만 한다.
    /// 진입점이 먼저 실행되므로 같은 씬의 화면(일시정지 · 결과)이 OnEnable에서 구독에 성공해야 한다.
    /// </summary>
    public class GameBootstrapTests
    {
        readonly InGameScene scene = new();

        [UnitySetUp]
        public IEnumerator Load() => scene.Load(); // 기존 매니저를 버리고 InGame을 새로 연다

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator G1_EntryPointCreatesOneManager_ScreensSubscribed()
        {
            Assert.AreEqual(1, Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length, "매니저는 하나");
            Assert.IsNotNull(Object.FindFirstObjectByType<GameBootstrap>(), "InGame 씬에 진입점");
            Assert.AreEqual(GameState.Play, GameManager.Current.CurrentState);

            var pause = Object.FindFirstObjectByType<PauseMenu>();
            Assert.IsTrue(GameManager.TryChangeState(GameState.Pause));
            yield return null;
            Assert.IsTrue(pause.Panel.activeSelf, "일시정지 화면이 Current로 구독되어 있다");

            Assert.IsTrue(GameManager.TryChangeState(GameState.Play));
            Assert.IsTrue(GameManager.TryChangeState(GameState.GameOver));
            yield return null;
            Assert.IsTrue(Object.FindFirstObjectByType<ResultScreen>().Panel.activeSelf, "결과 화면도 구독되어 있다");
        }
    }
}
