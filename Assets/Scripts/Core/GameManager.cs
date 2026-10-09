using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>
    /// 씬을 넘어 유지되는 게임 흐름 관리자. FSM을 소유하고, 상태 전이에 맞춰 씬을 이동한다.
    /// 어느 씬에서 플레이를 시작하든 Instance 첫 접근 시 자동 생성된다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        static GameManager instance;

        /// <summary>이미 있는 GameManager. 없으면 null (Instance와 달리 새로 만들지 않음 — 씬 종료 중 구독 해제용).</summary>
        public static GameManager Current => instance;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject(nameof(GameManager));
                    instance = go.AddComponent<GameManager>();
                }
                return instance;
            }
        }

        public GameStateMachine StateMachine { get; private set; }

        public GameState CurrentState => StateMachine.Current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // 에디터에서 InGame 씬을 바로 실행한 경우에도 상태가 맞도록 현재 씬으로 초기 상태를 정한다.
            var initial = SceneManager.GetActiveScene().name == SceneNames.InGame ? GameState.Play : GameState.Title;
            StateMachine = new GameStateMachine(initial);
            StateMachine.StateChanged += OnStateChanged;
            Time.timeScale = TimeScalePolicy.For(initial);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>타이틀의 [게임 시작] → 새 런 시작.</summary>
        public bool StartGame()
        {
            return StateMachine.TryChangeState(GameState.Play);
        }

        void OnStateChanged(GameState previous, GameState next)
        {
            // Pause·LevelUp 등에서 게임 시간을 멈춘다. 각 시스템은 상태를 몰라도 deltaTime만으로 정지된다.
            Time.timeScale = TimeScalePolicy.For(next);

            if (next == GameState.Play && previous is GameState.Title or GameState.GameOver or GameState.GameClear)
                SceneManager.LoadScene(SceneNames.InGame);
            else if (next == GameState.Title)
                SceneManager.LoadScene(SceneNames.Title);
        }
    }
}
