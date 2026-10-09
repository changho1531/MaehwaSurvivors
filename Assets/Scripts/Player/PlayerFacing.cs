using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 플레이어가 바라보는 방향 = 마지막으로 0이 아니었던 이동 방향 (대각선 포함, 설계서 7절).
    /// 시작 시에는 위쪽. 무기(적이 없을 때 매화검기, 화산십이검 부채꼴)와 애니메이션이 함께 쓴다.
    /// </summary>
    [DefaultExecutionOrder(10)] // PlayerMovement가 이번 프레임 입력을 읽은 뒤에 갱신한다
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerFacing : MonoBehaviour
    {
        public static readonly Vector2 Initial = Vector2.up;

        PlayerMovement movement;

        public Vector2 Direction { get; private set; } = Initial;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
        }

        void Update()
        {
            Direction = Resolve(Direction, movement.CurrentInput);
        }

        /// <summary>입력이 있으면 그 방향(정규화), 없으면 이전 방향 유지.</summary>
        public static Vector2 Resolve(Vector2 current, Vector2 input)
        {
            return input.sqrMagnitude > 0.0001f ? input.normalized : current;
        }
    }
}
