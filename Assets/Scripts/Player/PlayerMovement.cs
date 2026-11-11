using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>키보드 입력으로 플레이어를 이동시킨다. Kinematic 바디라 적에게 밀리지 않는다.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Tooltip("※ 예시값")]
        [SerializeField] float moveSpeed = 5f;

        Rigidbody2D body;

        /// <summary>
        /// 기본 이동속도 (매화보 적용 전). 화살 속도·XP 끌림처럼 "플레이어 속도 기준"으로 맞춘 값은 이것을 쓴다 —
        /// 매화보로 함께 흔들리지 않게 (7-H 규칙 4).
        /// </summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>이동속도 배율 (기본 1). 매화보만 바꾼다.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>실제로 움직이는 속도 = 기본 속도 × 배율</summary>
        public float CurrentSpeed => moveSpeed * Mathf.Max(0f, SpeedMultiplier);
        public Vector2 CurrentInput { get; private set; }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            CurrentInput = MoveInput.Read(Keyboard.current);
        }

        void FixedUpdate()
        {
            if (CurrentInput == Vector2.zero)
                return;

            body.MovePosition(body.position + CurrentInput * (CurrentSpeed * Time.fixedDeltaTime));
        }
    }
}
