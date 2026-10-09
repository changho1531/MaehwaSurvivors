using UnityEngine;

namespace Game.Player
{
    /// <summary>플레이어 그림이 보여 줄 4방향 (설계서 7-A절). 왼쪽은 오른쪽 그림을 좌우 반전한다.</summary>
    public enum PlayerSpriteDirection { Front, Back, Right, Left }

    /// <summary>
    /// 이동 여부 + PlayerFacing 방향 → 재생할 클립 결정 (설계서 7-A절).
    /// 방향은 무기와 같은 PlayerFacing 하나만 기준으로 삼아, 그림과 공격 방향이 어긋나지 않게 한다.
    /// 판정은 순수 함수(Resolve)로 분리해 EditMode에서 테스트하고, 이 컴포넌트는 결과를 Animator에 전달만 한다.
    /// </summary>
    [DefaultExecutionOrder(15)] // PlayerFacing(10)이 이번 프레임 방향을 정한 뒤에 그림을 고른다
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(PlayerFacing))]
    public class PlayerAnimator : MonoBehaviour
    {
        // Animator Controller(Player.controller)의 상태 이름과 같아야 한다.
        public static readonly int IdleState = Animator.StringToHash("Idle");
        public static readonly int WalkFrontState = Animator.StringToHash("Walk_Front");
        public static readonly int WalkBackState = Animator.StringToHash("Walk_Back");
        public static readonly int WalkRightState = Animator.StringToHash("Walk_Right");

        Animator animator;
        SpriteRenderer spriteRenderer;
        PlayerFacing facing;
        PlayerMovement movement;
        int currentState;

        public int CurrentState => currentState;

        void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            facing = GetComponent<PlayerFacing>();
            movement = GetComponent<PlayerMovement>();
        }

        void Update()
        {
            if (Time.deltaTime <= 0f)
                return; // 일시정지 중에는 그림도 그대로 (Animator도 timeScale 0이면 멈춘다)

            bool moving = movement != null && movement.CurrentInput.sqrMagnitude > 0.0001f;
            var (state, flipX) = Resolve(moving, facing.Direction);
            spriteRenderer.flipX = flipX;

            // 같은 상태를 매 프레임 Play하면 첫 프레임으로 되감기므로, 바뀔 때만 전환한다.
            if (state == currentState)
                return;
            currentState = state;
            animator.Play(state, 0, 0f);
        }

        /// <summary>
        /// 정지면 방향과 관계없이 Idle(정면 첫 프레임, 임시). 이동 중이면 바라보는 방향의 가장 가까운 4방향.
        /// 정확히 45° 대각선은 좌우 그림을 쓴다 [임시 결정: 옆모습이 진행 방향을 더 잘 보여 줌].
        /// </summary>
        public static (int state, bool flipX) Resolve(bool moving, Vector2 facing)
        {
            if (!moving)
                return (IdleState, false);

            return ToFourWay(facing) switch
            {
                PlayerSpriteDirection.Back => (WalkBackState, false),
                PlayerSpriteDirection.Right => (WalkRightState, false),
                PlayerSpriteDirection.Left => (WalkRightState, true),
                _ => (WalkFrontState, false),
            };
        }

        public static PlayerSpriteDirection ToFourWay(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) && direction.x != 0f)
                return direction.x > 0f ? PlayerSpriteDirection.Right : PlayerSpriteDirection.Left;
            return direction.y > 0f ? PlayerSpriteDirection.Back : PlayerSpriteDirection.Front;
        }
    }
}
