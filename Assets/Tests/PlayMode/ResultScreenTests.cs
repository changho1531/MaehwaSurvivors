using System.Collections;
using Game.Core;
using Game.Player;
using Game.Progression;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [7-B절] 처치 수 집계, HUD 배치(HP 바 위 · XP 바 아래), 사망 → 결과 화면 페이드인(생존 시간·처치 수),
    /// 남은 오브젝트는 멈춘 채 유지, 재도전 = 씬 재로드로 새 런, 타이틀 이동, 버튼 연타는 첫 입력만.
    /// </summary>
    public class ResultScreenTests
    {
        readonly InGameScene scene = new();
        PlayerHealth health;
        RunStats stats;
        ResultScreen result;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            health = scene.Player.GetComponent<PlayerHealth>();
            stats = Object.FindFirstObjectByType<RunStats>();
            result = Object.FindFirstObjectByType<ResultScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(result, "결과 화면 없음");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        IEnumerator Die()
        {
            health.TakeDamage(health.MaxHp + 1f);
            yield return null;
            Assert.AreEqual(GameState.GameOver, GameManager.Instance.CurrentState);
        }

        static IEnumerator WaitRealtime(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator R1_Kills_CountEveryEnemyKilled()
        {
            Assert.AreEqual(0, stats.Kills);
            for (int i = 0; i < 3; i++)
            {
                var enemy = scene.SpawnAt(new Vector2(5f + i, 5f), chase: false);
                enemy.TakeDamage(enemy.Data.maxHp);
            }
            yield return null;
            Assert.AreEqual(3, stats.Kills, "Enemy.Killed 1회 = 1");
        }

        [UnityTest]
        public IEnumerator R2_HudLayout_HpBarOnTop_XpBarBelow()
        {
            yield return null;
            var hud = Object.FindFirstObjectByType<HudView>();
            var hp = (RectTransform)hud.HpBar.transform;
            var xp = (RectTransform)hud.XpBar.transform;
            var corners = new Vector3[4];
            hp.GetWorldCorners(corners);
            float hpBottom = corners[0].y;
            xp.GetWorldCorners(corners);
            float xpTop = corners[1].y;
            Assert.GreaterOrEqual(hpBottom, xpTop, "HP 바가 위, 경험치 바가 그 바로 아래");
            Assert.AreEqual("Lv 1", hud.LevelText.text);
        }

        [UnityTest]
        public IEnumerator R3_Death_ShowsResult_FadesIn_WithTimeAndKills()
        {
            var enemy = scene.SpawnAt(new Vector2(5f, 5f), chase: false);
            enemy.TakeDamage(enemy.Data.maxHp);
            var survivor = scene.SpawnAt(new Vector2(-5f, 5f), chase: false);
            yield return null;
            Assert.IsFalse(result.Panel.activeSelf, "살아 있는 동안 숨김");

            yield return Die();
            Assert.IsTrue(result.Panel.activeSelf, "사망 즉시 결과 화면");
            Assert.Less(result.Group.alpha, 1f, "페이드인 시작");
            StringAssert.Contains(RunStats.Format(stats.Elapsed), result.TimeText.text, "생존 시간 표시");
            StringAssert.Contains("1", result.KillsText.text, "처치 수 표시");

            yield return WaitRealtime(result.FadeSeconds + 0.2f);
            Assert.AreEqual(1f, result.Group.alpha, 0.001f, "게임 시간이 멈춰도 실시간으로 페이드 완료");
            Assert.IsTrue(survivor.gameObject.activeInHierarchy, "남은 적은 지우지 않고 멈춘 채 배경으로");
        }

        [UnityTest]
        public IEnumerator R4_Retry_ReloadsScene_NewRun()
        {
            var enemy = scene.SpawnAt(new Vector2(5f, 5f), chase: false);
            enemy.TakeDamage(enemy.Data.maxHp);
            yield return Die();

            var oldStats = stats;
            result.RetryButton.onClick.Invoke();
            result.RetryButton.onClick.Invoke(); // 연타
            float end = Time.realtimeSinceStartup + 5f;
            while (oldStats != null && Time.realtimeSinceStartup < end)
                yield return null; // 씬 재로드로 이전 런 오브젝트가 파괴될 때까지
            yield return null;

            Assert.AreEqual(SceneNames.InGame, SceneManager.GetActiveScene().name);
            Assert.AreEqual(GameState.Play, GameManager.Instance.CurrentState, "재도전 = 새 런");
            Assert.AreEqual(1f, Time.timeScale);
            var newStats = Object.FindFirstObjectByType<RunStats>();
            Assert.AreEqual(0, newStats.Kills, "새 런은 처치 수 0부터");
            var newHealth = Object.FindFirstObjectByType<PlayerHealth>();
            Assert.AreEqual(newHealth.MaxHp, newHealth.CurrentHp, "HP 가득");
        }

        [UnityTest]
        public IEnumerator R5_Title_LoadsTitle_DoubleClickIgnored()
        {
            yield return Die();
            result.TitleButton.onClick.Invoke();
            Assert.IsFalse(result.RetryButton.interactable, "첫 입력 후 다른 버튼도 잠김");
            result.RetryButton.onClick.Invoke(); // 다른 버튼 연타 → 무시

            float end = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.Title && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(SceneNames.Title, SceneManager.GetActiveScene().name);
            Assert.AreEqual(GameState.Title, GameManager.Instance.CurrentState, "연타한 재도전은 처리되지 않음");
        }
    }
}
