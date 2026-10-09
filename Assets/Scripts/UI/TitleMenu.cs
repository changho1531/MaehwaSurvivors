using System;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>타이틀 화면(MU-A-001)의 버튼 처리. [나가기]는 확인창을 거쳐 게임을 종료한다 (설계서 7-C 규칙 5).</summary>
    public class TitleMenu : MonoBehaviour
    {
        const string QuitMessage = "게임을 종료하시겠습니까?";

        [SerializeField] Button startButton;
        [SerializeField] Button exitButton;
        [SerializeField] ConfirmDialog confirm;

        /// <summary>
        /// 실제 종료 동작. 기본값은 빌드에서 Application.Quit, 에디터에서는 플레이 모드 종료.
        /// 테스트가 에디터를 끄지 않고 "종료 요청이 갔는지"만 확인할 수 있도록 교체 가능하게 둔다.
        /// </summary>
        public static Action QuitHandler { get; set; } = QuitApplication;

        public Button StartButton => startButton;
        public Button ExitButton => exitButton;
        public ConfirmDialog Confirm => confirm;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            QuitHandler = QuitApplication;
        }

        void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            exitButton.onClick.AddListener(OnExitClicked);
        }

        void OnStartClicked()
        {
            if (confirm.IsOpen)
                return; // 확인창이 떠 있는 동안에는 뒤 버튼을 받지 않는다
            GameManager.Instance.StartGame();
        }

        void OnExitClicked()
        {
            confirm.Show(QuitMessage, () => QuitHandler?.Invoke());
        }

        static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
