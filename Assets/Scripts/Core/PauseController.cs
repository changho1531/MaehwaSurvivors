using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    /// <summary>
    /// ESC 일시정지 (설계서 4절 Play ↔ Pause, Pause → Title). 상태 전이만 하고, 화면 표시는 PauseMenuView가
    /// 상태 변경 이벤트를 구독해 처리한다. 시간 정지는 TimeScalePolicy가 상태에 맞춰 한곳에서 한다.
    /// LevelUp·GameOver 중 ESC는 FSM 전이표에 없으므로 자연히 무시된다.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        void Update()
        {
            // timeScale 0인 Pause 중에도 Update와 입력은 돌아가므로 같은 키로 재개할 수 있다.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Toggle();
        }

        public bool Toggle()
        {
            var state = GameManager.Instance.CurrentState;
            if (state == GameState.Play)
                return Pause();
            if (state == GameState.Pause)
                return Resume();
            return false;
        }

        public bool Pause() => GameManager.Instance.StateMachine.TryChangeState(GameState.Pause);

        public bool Resume() => GameManager.Instance.StateMachine.TryChangeState(GameState.Play);

        public bool QuitToTitle() => GameManager.Instance.StateMachine.TryChangeState(GameState.Title);
    }
}
