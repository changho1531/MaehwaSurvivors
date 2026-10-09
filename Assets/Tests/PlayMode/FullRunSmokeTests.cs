using System.Collections;
using Game.Core;
using Game.Player;
using Game.Progression;
using Game.UI;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// 한 판 통째로: 타이틀 [게임 시작] → 자동 전투(입력 없이 제자리) → 레벨업마다 첫 카드 선택 → 사망 → 결과 화면 → 재도전.
    /// 개별 기능 테스트가 놓치는 "시스템끼리 이어 붙였을 때"의 문제(에러 로그, 멈춤, 상태 꼬임)를 잡는다.
    /// </summary>
    public class FullRunSmokeTests
    {
        const float Speed = 4f;          // 게임 시간 가속 (실제 대기 시간 단축)
        const float MaxGameSeconds = 400f;

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator OneFullRun_TitleToGameOverToRetry()
        {
            var old = Object.FindFirstObjectByType<GameManager>();
            if (old != null)
                Object.Destroy(old.gameObject);
            yield return SceneManager.LoadSceneAsync(SceneNames.Title, LoadSceneMode.Single);
            yield return null;

            Object.FindFirstObjectByType<TitleMenu>().StartButton.onClick.Invoke();
            yield return WaitScene(SceneNames.InGame);
            yield return null;
            Assert.AreEqual(GameState.Play, GameManager.Instance.CurrentState);

            var health = Object.FindFirstObjectByType<PlayerHealth>();
            var level = Object.FindFirstObjectByType<PlayerLevel>();
            var flow = Object.FindFirstObjectByType<LevelUpFlow>();
            var stats = Object.FindFirstObjectByType<RunStats>();
            var inventory = Object.FindFirstObjectByType<WeaponInventory>();
            var result = Object.FindFirstObjectByType<ResultScreen>(FindObjectsInactive.Include);

            int levelUps = 0;
            float firstLevelUpAt = -1f;
            float firstHitAt = -1f;
            var picks = new System.Text.StringBuilder();

            while (GameManager.Instance.CurrentState != GameState.GameOver && stats.Elapsed < MaxGameSeconds)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameState.Play && Time.timeScale != Speed)
                    Time.timeScale = Speed;

                if (state == GameState.LevelUp && flow.CanAcceptChoice)
                {
                    if (firstLevelUpAt < 0f)
                        firstLevelUpAt = stats.Elapsed;
                    var card = flow.CurrentCards[0];
                    picks.Append($"{card.Weapon.displayName}{(card.IsNew ? "(신규)" : $"(Lv{card.NextLevel})")} ");
                    Assert.IsTrue(flow.Choose(0));
                    levelUps++;
                }

                if (firstHitAt < 0f && health.CurrentHp < health.MaxHp)
                    firstHitAt = stats.Elapsed;
                yield return null;
            }

            string report =
                $"생존 {RunStats.Format(stats.Elapsed)} ({stats.Elapsed:F1}s) / 처치 {stats.Kills} / 레벨 {level.Level} / 레벨업 {levelUps}회\n" +
                $"첫 피격 {firstHitAt:F1}s / 첫 레벨업 {firstLevelUpAt:F1}s / HP {health.CurrentHp:F0}/{health.MaxHp:F0}\n" +
                $"카드 선택: {picks}\n" +
                $"보유 무기: {string.Join(", ", System.Linq.Enumerable.Select(inventory.Owned, w => $"{w.Data.displayName} Lv{w.Level}"))}";
            bool diedNaturally = GameManager.Instance.CurrentState == GameState.GameOver;
            Debug.Log($"[SMOKE] 자연 사망 {(diedNaturally ? "예" : "아니오")}\n" + report);

            // 밸런스상 안 죽을 수도 있다(그 자체는 기록만). 결과 화면 흐름을 보려고 강제로 사망시킨다.
            if (!diedNaturally)
            {
                Time.timeScale = 1f;
                health.TakeDamage(health.MaxHp + 1f);
                yield return null;
            }
            Assert.AreEqual(GameState.GameOver, GameManager.Instance.CurrentState);
            Assert.Greater(stats.Kills, 0, "무기가 적을 처치해야 한다");
            Assert.GreaterOrEqual(levelUps, 1, "한 판에 최소 한 번은 레벨업");
            Assert.AreEqual(0f, Time.timeScale, "GameOver 중 시간 정지");

            // 결과 화면 → 재도전
            float fadeEnd = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < fadeEnd)
                yield return null;
            Assert.IsTrue(result.Panel.activeSelf);
            Assert.AreEqual(1f, result.Group.alpha, 0.001f);
            StringAssert.Contains(stats.Kills.ToString(), result.KillsText.text);

            result.RetryButton.onClick.Invoke();
            float end = Time.realtimeSinceStartup + 5f;
            while (stats != null && Time.realtimeSinceStartup < end)
                yield return null;
            yield return null;
            Assert.AreEqual(GameState.Play, GameManager.Instance.CurrentState, "재도전 = 새 런");
            Assert.AreEqual(0, Object.FindFirstObjectByType<RunStats>().Kills);
        }

        static IEnumerator WaitScene(string name)
        {
            float end = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(name, SceneManager.GetActiveScene().name);
        }
    }
}
