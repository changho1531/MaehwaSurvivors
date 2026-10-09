using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 일시정지 (MU-B-002, 설계서 7-C절). ESC 하나로 상황에 맞게 동작한다:
    /// 확인창이 떠 있으면 닫기(취소) → 일시정지 중이면 재개 → Play·LevelUp 중이면 일시정지. 그 외(GameOver 등)는 무시.
    /// 재개는 FSM이 기억한 직전 상태로 돌아가므로, 이 클래스는 어디서 멈췄는지 몰라도 된다.
    /// 화면 표시는 FSM StateChanged 구독으로만 정한다 (버튼으로 재개하든 ESC로 재개하든 같은 경로).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        const string QuitToTitleMessage = "정말 나가시겠습니까?";

        [SerializeField] UiSettings settings;
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] Button resumeButton;
        [SerializeField] Button titleButton;
        [SerializeField] ConfirmDialog confirm;

        float fadeElapsed;

        public GameObject Panel => panel;
        public CanvasGroup Group => group;
        public Button ResumeButton => resumeButton;
        public Button TitleButton => titleButton;
        public ConfirmDialog Confirm => confirm;

        static GameStateMachine Fsm => GameManager.Instance.StateMachine;

        void Awake()
        {
            resumeButton.onClick.AddListener(Resume);
            titleButton.onClick.AddListener(AskQuitToTitle);
            panel.SetActive(false);
        }

        void OnEnable()
        {
            Fsm.StateChanged += OnStateChanged;
            panel.SetActive(Fsm.Current == GameState.Pause);
        }

        void OnDisable()
        {
            // 씬 종료 순서상 GameManager가 먼저 사라졌을 수 있어 Instance로 새로 만들지 않게 확인한다.
            var manager = Object.FindFirstObjectByType<GameManager>();
            if (manager != null && manager.StateMachine != null)
                manager.StateMachine.StateChanged -= OnStateChanged;
        }

        void Update()
        {
            // timeScale 0인 Pause·LevelUp 중에도 Update와 입력은 돌아간다.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                OnEscape();

            if (panel.activeSelf && group.alpha < 1f)
            {
                fadeElapsed += Time.unscaledDeltaTime; // 게임 시간이 멈춰 있으므로 실시간
                group.alpha = Mathf.Clamp01(fadeElapsed / settings.fadeSeconds);
            }
        }

        public void OnEscape()
        {
            if (confirm.IsOpen)
            {
                confirm.Cancel(); // 확인창 닫기 = 취소, 일시정지 유지
                return;
            }

            var state = Fsm.Current;
            if (state == GameState.Pause)
                Resume(); // 페이드 도중이어도 즉시 재개
            else if (state is GameState.Play or GameState.LevelUp)
                Fsm.TryChangeState(GameState.Pause);
        }

        public void Resume() => Fsm.TryResume();

        void AskQuitToTitle()
        {
            // 확인 시 Title 상태 → GameManager가 타이틀 씬 로드 (현재 런 폐기). 취소 시 일시정지 화면 유지.
            confirm.Show(QuitToTitleMessage, () => Fsm.TryChangeState(GameState.Title));
        }

        void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.Pause)
            {
                fadeElapsed = 0f;
                group.alpha = 0f;
                panel.SetActive(true);
            }
            else
            {
                panel.SetActive(false);
                if (confirm.IsOpen)
                    confirm.Cancel();
            }
        }
    }
}
