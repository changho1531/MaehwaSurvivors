using System.Collections;
using Game.Core;
using Game.Player;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[ESC 일시정지 · 최소 HUD] ESC로 Play↔Pause, 일시정지 화면 버튼, LevelUp 중 ESC 무시, HP·XP 바·레벨·생존 시간.</summary>
    public class HudPauseTests : InGameInputFixture
    {
        readonly InGameScene scene = new();
        Keyboard keyboard;
        PauseMenuView pauseMenu;
        HudView hud;
        RunClock clock;

        IEnumerator Prepare()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            pauseMenu = Object.FindFirstObjectByType<PauseMenuView>();
            hud = Object.FindFirstObjectByType<HudView>();
            clock = Object.FindFirstObjectByType<RunClock>();
            Assert.IsNotNull(pauseMenu, "일시정지 화면 없음");
            Assert.IsNotNull(hud, "HUD 없음");
            Assert.IsNotNull(clock, "RunClock 없음");
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
        }

        IEnumerator PressEsc()
        {
            Press(keyboard.escapeKey);
            yield return null;
            Release(keyboard.escapeKey);
            yield return null;
        }

        static GameState State => GameManager.Instance.CurrentState;

        [UnityTest]
        public IEnumerator P1_Esc_TogglesPause_StopsTime_ShowsMenu()
        {
            yield return Prepare();
            Assert.IsFalse(pauseMenu.Panel.activeSelf, "시작 시 일시정지 화면 숨김");

            yield return PressEsc();
            Assert.AreEqual(GameState.Pause, State);
            Assert.AreEqual(0f, Time.timeScale, "일시정지 중 게임 시간 정지");
            Assert.IsTrue(pauseMenu.Panel.activeSelf, "일시정지 화면 표시");

            yield return PressEsc();
            Assert.AreEqual(GameState.Play, State, "ESC로 재개");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(pauseMenu.Panel.activeSelf);
        }

        [UnityTest]
        public IEnumerator P2_ResumeButton_ReturnsToPlay()
        {
            yield return Prepare();
            yield return PressEsc();
            Assert.AreEqual(GameState.Pause, State);

            pauseMenu.ResumeButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameState.Play, State);
            Assert.IsFalse(pauseMenu.Panel.activeSelf);
        }

        [UnityTest]
        public IEnumerator P3_TitleButton_LoadsTitle()
        {
            yield return Prepare();
            yield return PressEsc();

            pauseMenu.TitleButton.onClick.Invoke();
            float end = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.Title && Time.realtimeSinceStartup < end)
                yield return null;

            Assert.AreEqual(SceneNames.Title, SceneManager.GetActiveScene().name, "Pause → Title 씬 이동");
            Assert.AreEqual(GameState.Title, State);
            Assert.AreEqual(1f, Time.timeScale, "타이틀에서는 시간 정상");
        }

        [UnityTest]
        public IEnumerator P4_EscDuringLevelUp_IsIgnored()
        {
            yield return Prepare();
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;
            Assert.AreEqual(GameState.LevelUp, State);

            yield return PressEsc();
            Assert.AreEqual(GameState.LevelUp, State, "LevelUp 중 ESC 무시");
            Assert.IsFalse(pauseMenu.Panel.activeSelf);
        }

        [UnityTest]
        public IEnumerator P5_SurvivalTime_CountsInPlay_StopsInPause()
        {
            yield return Prepare();
            float end = Time.time + 1.1f;
            while (Time.time < end)
                yield return null;
            Assert.That(clock.Elapsed, Is.GreaterThanOrEqualTo(1f));
            Assert.AreEqual(RunClock.Format(clock.Elapsed), hud.TimeText.text, "우측 상단 생존 시간 mm:ss");
            StringAssert.StartsWith("00:0", hud.TimeText.text);

            yield return PressEsc();
            float paused = clock.Elapsed;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(paused, clock.Elapsed, "일시정지 중 생존 시간 정지");
        }

        [UnityTest]
        public IEnumerator P6_HpAndXpBars_FollowEvents()
        {
            yield return Prepare();
            var health = scene.Player.GetComponent<PlayerHealth>();
            Assert.AreEqual(1f, hud.HpBar.Ratio, 0.001f, "시작 HP 바 가득");
            Assert.AreEqual(0f, hud.XpBar.Ratio, 0.001f, "시작 XP 바 비어 있음");
            Assert.AreEqual("Lv 1", hud.LevelText.text);

            health.TakeDamage(health.MaxHp * 0.25f);
            Assert.AreEqual(0.75f, hud.HpBar.Ratio, 0.001f, "피격 시 HP 바 감소");

            int required = scene.Level.RequiredXp;
            scene.Level.AddXp(required / 2);
            Assert.AreEqual((float)(required / 2) / required, hud.XpBar.Ratio, 0.001f, "XP 획득 시 XP 바 증가");
        }
    }
}
