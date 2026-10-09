using Game.Core;
using Game.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 게임오버 결과 화면 (MU-C-001, 설계서 7-B 규칙 5·7): GameOver 상태가 되면 멈춘 게임 화면 위로 페이드인하고
    /// 생존 시간·처치 수를 보여 준다. [재도전]은 GameOver → Play(씬 재로드로 새 런), [타이틀로]는 GameOver → Title.
    /// 게임 시간이 멈춰 있으므로 페이드는 실시간 기준. 버튼 연타는 첫 입력만 처리한다.
    /// 게임 클리어 화면은 중간고사 빌드 범위 밖 (15절: 통합 여부 미정).
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] RunStats stats;
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] Text timeText;
        [SerializeField] Text killsText;
        [SerializeField] Button retryButton;
        [SerializeField] Button titleButton;
        [Tooltip("일시정지 화면과 같은 페이드 설정 (7-C 규칙 1)")]
        [SerializeField] UiSettings settings;

        float fadeElapsed;
        bool choiceMade;

        public GameObject Panel => panel;
        public CanvasGroup Group => group;
        public Text TimeText => timeText;
        public Text KillsText => killsText;
        public Button RetryButton => retryButton;
        public Button TitleButton => titleButton;
        public float FadeSeconds => settings.fadeSeconds;

        void Awake()
        {
            retryButton.onClick.AddListener(() => Choose(GameState.Play));
            titleButton.onClick.AddListener(() => Choose(GameState.Title));
            panel.SetActive(false);
        }

        void OnEnable() => GameManager.Instance.StateMachine.StateChanged += OnStateChanged;

        void OnDisable()
        {
            if (GameManager.Current != null)
                GameManager.Current.StateMachine.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.GameOver)
                Show();
        }

        void Show()
        {
            timeText.text = $"생존 시간  {RunStats.Format(stats.Elapsed)}";
            killsText.text = $"처치 수  {stats.Kills}";
            fadeElapsed = 0f;
            group.alpha = 0f;
            choiceMade = false;
            panel.SetActive(true);
        }

        void Update()
        {
            if (!panel.activeSelf || group.alpha >= 1f)
                return;
            fadeElapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(fadeElapsed / settings.fadeSeconds);
        }

        void Choose(GameState next)
        {
            if (choiceMade)
                return; // 연타 방지: 첫 입력만
            choiceMade = true;
            retryButton.interactable = false;
            titleButton.interactable = false;
            // 씬을 다시 불러와 적·구슬·투사체를 풀째로 정리한다 (GameManager가 전이에 맞춰 씬 로드).
            GameManager.Instance.StateMachine.TryChangeState(next);
        }
    }
}
