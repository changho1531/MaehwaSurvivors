using System.Collections;
using Game.Core;
using Game.Player;
using Game.Progression;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [7-C절] ESC 일시정지(페이드인, ESC/버튼 재개, 페이드 중 즉시 재개), 카드 선택 중 일시정지 → 카드로 복귀,
    /// 타이틀 이동 확인창(취소·ESC·확인), 결과 화면 ESC 무시, 생존 시간 표시, HP·XP 바.
    /// </summary>
    public class HudPauseTests : InGameInputFixture
    {
        readonly InGameScene scene = new();
        Keyboard keyboard;
        PauseMenu pause;
        HudView hud;
        SurvivalTimeView timeView;
        RunStats stats;

        IEnumerator Prepare()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            pause = Object.FindFirstObjectByType<PauseMenu>();
            hud = Object.FindFirstObjectByType<HudView>();
            timeView = Object.FindFirstObjectByType<SurvivalTimeView>();
            stats = Object.FindFirstObjectByType<RunStats>();
            Assert.IsNotNull(pause, "일시정지 화면 없음");
            Assert.IsNotNull(hud, "HUD 없음");
            Assert.IsNotNull(timeView, "생존 시간 표시 없음");
            Assert.IsNotNull(stats, "RunStats 없음");
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

        static IEnumerator WaitRealtime(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
                yield return null;
        }

        static GameState State => GameManager.Instance.CurrentState;

        [UnityTest]
        public IEnumerator P1_Esc_Pauses_FadesIn_EscAgainResumes()
        {
            yield return Prepare();
            Assert.IsFalse(pause.Panel.activeSelf, "시작 시 일시정지 화면 숨김");

            yield return PressEsc();
            Assert.AreEqual(GameState.Pause, State);
            Assert.AreEqual(0f, Time.timeScale, "일시정지 중 게임 시간 정지");
            Assert.IsTrue(pause.Panel.activeSelf, "일시정지 화면 표시");
            Assert.Less(pause.Group.alpha, 1f, "페이드인 중");

            yield return WaitRealtime(1f);
            Assert.AreEqual(1f, pause.Group.alpha, 0.001f, "실시간으로 페이드 완료");

            yield return PressEsc();
            Assert.AreEqual(GameState.Play, State, "ESC 한 번 더 → 재개");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(pause.Panel.activeSelf);
        }

        [UnityTest]
        public IEnumerator P2_ResumeButton_ReturnsToPlay()
        {
            yield return Prepare();
            yield return PressEsc();
            pause.ResumeButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameState.Play, State);
            Assert.IsFalse(pause.Panel.activeSelf);
        }

        [UnityTest]
        public IEnumerator P3_EscDuringFadeIn_ResumesImmediately()
        {
            yield return Prepare();
            yield return PressEsc();
            Assert.Less(pause.Group.alpha, 1f);
            yield return PressEsc();
            Assert.AreEqual(GameState.Play, State, "페이드 완료를 기다리지 않음");
        }

        [UnityTest]
        public IEnumerator P4_PauseDuringLevelUp_CoversCards_ResumeReturnsToCards()
        {
            yield return Prepare();
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return WaitRealtime(0.3f); // 카드 입력 잠금 해제
            Assert.AreEqual(GameState.LevelUp, State);
            var cards = Object.FindFirstObjectByType<LevelUpCardView>();
            Assert.IsTrue(cards.Panel.activeSelf);

            yield return PressEsc();
            Assert.AreEqual(GameState.Pause, State, "카드 선택 중에도 일시정지");
            Assert.IsTrue(pause.Panel.activeSelf);
            Assert.IsTrue(cards.Panel.activeSelf, "카드는 아래에 그대로");
            Assert.Greater(pause.GetComponentInParent<Canvas>().sortingOrder, cards.GetComponentInParent<Canvas>().sortingOrder, "일시정지 화면이 카드 위");
            Assert.IsFalse(scene.LevelUpFlow.Choose(0), "일시정지 중 카드는 선택되지 않음");

            yield return PressEsc();
            Assert.AreEqual(GameState.LevelUp, State, "재개 → 카드 선택으로 복귀");
            Assert.AreEqual(0f, Time.timeScale, "LevelUp이라 게임은 여전히 정지");
            Assert.IsTrue(scene.LevelUpFlow.Choose(0), "카드 선택 이어감");
        }

        [UnityTest]
        public IEnumerator P5_QuitToTitle_AsksConfirm_CancelAndEscKeepPause()
        {
            yield return Prepare();
            yield return PressEsc();

            pause.TitleButton.onClick.Invoke();
            Assert.IsTrue(pause.Confirm.IsOpen, "확인창");
            Assert.AreEqual("정말 나가시겠습니까?", pause.Confirm.MessageText.text);

            pause.Confirm.CancelButton.onClick.Invoke();
            Assert.IsFalse(pause.Confirm.IsOpen);
            Assert.AreEqual(GameState.Pause, State, "[취소] → 일시정지 유지");

            pause.TitleButton.onClick.Invoke();
            yield return PressEsc();
            Assert.IsFalse(pause.Confirm.IsOpen, "확인창이 떠 있을 때 ESC = 취소");
            Assert.AreEqual(GameState.Pause, State, "재개되지 않고 일시정지 유지");
        }

        [UnityTest]
        public IEnumerator P6_QuitToTitle_Confirm_LoadsTitle()
        {
            yield return Prepare();
            yield return PressEsc();
            pause.TitleButton.onClick.Invoke();
            pause.Confirm.ConfirmButton.onClick.Invoke();

            float end = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.Title && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(SceneNames.Title, SceneManager.GetActiveScene().name, "[확인] → 타이틀 씬");
            Assert.AreEqual(GameState.Title, State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator P7_EscOnResultScreen_IsIgnored()
        {
            yield return Prepare();
            var health = scene.Player.GetComponent<PlayerHealth>();
            health.TakeDamage(health.MaxHp + 1f);
            yield return null;
            Assert.AreEqual(GameState.GameOver, State);

            yield return PressEsc();
            Assert.AreEqual(GameState.GameOver, State, "결과 화면에서 ESC 무시");
            Assert.IsFalse(pause.Panel.activeSelf);
        }

        [UnityTest]
        public IEnumerator P8_SurvivalTime_ShownTopRight_StopsInPause()
        {
            yield return Prepare();
            float end = Time.time + 1.1f;
            while (Time.time < end)
                yield return null;
            Assert.That(stats.Elapsed, Is.GreaterThanOrEqualTo(1f));
            Assert.AreEqual(RunStats.Format(stats.Elapsed), timeView.Text.text, "생존 시간 mm:ss");
            var rect = (RectTransform)timeView.transform;
            Assert.AreEqual(new Vector2(1f, 1f), rect.anchorMin, "우측 상단");

            yield return PressEsc();
            float paused = stats.Elapsed;
            yield return WaitRealtime(0.5f);
            Assert.AreEqual(paused, stats.Elapsed, "일시정지 중 생존 시간 정지");
        }

        [UnityTest]
        public IEnumerator P9_HpAndXpBars_FollowEvents()
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
