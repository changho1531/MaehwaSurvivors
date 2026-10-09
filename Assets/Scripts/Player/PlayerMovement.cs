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

        public float MoveSpeed => moveSpeed;
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

            body.MovePosition(body.position + CurrentInput * (moveSpeed * Time.fixedDeltaTime));
        }
    }
}
