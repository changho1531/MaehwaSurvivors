using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 일시정지 화면 (MU-B-002): [재개] / [타이틀로]. FSM 상태 변경 이벤트만 보고 표시·숨김을 정한다.
    /// [설정]은 검토 대기 항목(설계서 15절)이라 넣지 않았다.
    /// </summary>
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField] PauseController controller;
        [SerializeField] GameObject panel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button titleButton;

        public GameObject Panel => panel;
        public Button ResumeButton => resumeButton;
        public Button TitleButton => titleButton;

        void Awake()
        {
            resumeButton.onClick.AddListener(() => controller.Resume());
            titleButton.onClick.AddListener(() => controller.QuitToTitle());
            panel.SetActive(false);
        }

        void OnEnable()
        {
            GameManager.Instance.StateMachine.StateChanged += OnStateChanged;
            panel.SetActive(GameManager.Instance.CurrentState == GameState.Pause);
        }

        void OnDisable()
        {
            // 씬 종료 순서상 GameManager가 먼저 사라졌을 수 있어 Instance로 새로 만들지 않게 확인한다.
            var manager = Object.FindFirstObjectByType<GameManager>();
            if (manager != null && manager.StateMachine != null)
                manager.StateMachine.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(GameState previous, GameState next)
        {
            panel.SetActive(next == GameState.Pause);
        }
    }
}
