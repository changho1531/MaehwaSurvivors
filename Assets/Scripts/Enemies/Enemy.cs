using System;
using Game.Player;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 임시 근거리 적. 대상(플레이어)을 향해 직선으로 추적하고, HP가 0이 되면 Dead 처리 후 풀로 돌아간다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        /// <summary>적이 처치될 때 발행. 처치 수·XP 드롭 등은 이 이벤트를 구독해 붙인다.</summary>
        public static event Action<Enemy> Killed;

        [SerializeField] EnemyData data;

        Rigidbody2D body;
        Transform target;
        Action<Enemy> releaseToPool;

        public EnemyData Data => data;
        public float CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0f && gameObject.activeInHierarchy;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        void OnEnable()
        {
            CurrentHp = data != null ? data.maxHp : 1f;
            EnemyRegistry.Register(this);
        }

        void OnDisable()
        {
            EnemyRegistry.Unregister(this);
            target = null;
            releaseToPool = null;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        /// <summary>스폰 직후 호출. release는 처치 시 풀로 되돌리는 콜백.</summary>
        public void Init(Transform chaseTarget, Action<Enemy> release)
        {
            target = chaseTarget;
            releaseToPool = release;
        }

        void FixedUpdate()
        {
            if (target == null || data == null)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            var toTarget = (Vector2)target.position - body.position;
            body.linearVelocity = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized * data.moveSpeed : Vector2.zero;
        }

        // 접촉 데미지 (7-B 규칙 1): 닿아 있는 물리 스텝마다 contactDps × 스텝 시간만큼 깎는다.
        // 적마다 따로 호출되므로 여러 마리에게 둘러싸이면 마리 수만큼 자연히 합산된다 (상한 없음, 무적 시간 없음).
        // 데미지 양은 공격하는 쪽(적 데이터)이, 사망·GameOver 판정은 맞는 쪽(PlayerHealth)이 정한다.
        void OnCollisionStay2D(Collision2D collision)
        {
            if (data == null || data.contactDps <= 0f || !IsAlive)
                return;
            if (collision.gameObject.TryGetComponent(out PlayerHealth player))
                player.TakeDamage(data.contactDps * Time.fixedDeltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            CurrentHp -= amount;
            if (CurrentHp <= 0f)
                Die();
        }

        void Die()
        {
            CurrentHp = 0f;
            var release = releaseToPool;
            Killed?.Invoke(this);

            if (release != null)
                release(this);
            else
                gameObject.SetActive(false);
        }
    }
}
