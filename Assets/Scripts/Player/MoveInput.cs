using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>WASD와 방향키를 동시에 지원하는 이동 입력 해석.</summary>
    public static class MoveInput
    {
        public static Vector2 Read(Keyboard keyboard)
        {
            if (keyboard == null)
                return Vector2.zero;

            float x = Axis(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed,
                           keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
            float y = Axis(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed,
                           keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed);

            // 대각선 이동이 더 빨라지지 않도록 정규화한다.
            var move = new Vector2(x, y);
            return move.sqrMagnitude > 1f ? move.normalized : move;
        }

        static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
