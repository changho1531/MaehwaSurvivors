using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>타이틀 화면(MU-A-001)의 버튼 처리.</summary>
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button exitButton;

        public Button StartButton => startButton;
        public Button ExitButton => exitButton;

        void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            exitButton.onClick.AddListener(OnExitClicked);
        }

        void OnStartClicked()
        {
            GameManager.Instance.StartGame();
        }

        void OnExitClicked()
        {
            // TODO: 기획서 MU-A-001 — 확인 팝업 후 종료. 현재 단계에서는 의도적으로 비워 둔다.
        }
    }
}
