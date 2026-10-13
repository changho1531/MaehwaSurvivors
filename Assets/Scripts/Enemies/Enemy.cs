using System;
using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 모든 적의 공통 부분: HP, 피격, 사망·Killed 이벤트, 풀 반환, 접촉 데미지 (설계서 8-A절 적 행동 분리).
    /// 이동·공격은 같은 오브젝트의 EnemyBehaviour(근거리 = 추적, 궁수 = 사거리 유지 + 사격)에 맡긴다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        /// <summary>적이 처치될 때 발행. 처치 수·XP 드롭 등은 이 이벤트를 구독해 붙인다.</summary>
        public static event Action<Enemy> Killed;

        [SerializeField] EnemyData data;

        Rigidbody2D body;
        EnemyBehaviour behaviour;
        Transform target;
        Action<Enemy> releaseToPool;
        ContactFilter2D enemyFilter;

        public EnemyData Data => data;
        public float CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0f && gameObject.activeInHierarchy;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            behaviour = GetComponent<EnemyBehaviour>();
            enemyFilter.SetLayerMask(LayerMask.GetMask(GameLayers.Enemy));
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
            if (behaviour != null)
                behaviour.ResetState();
        }

        void FixedUpdate()
        {
            if (target == null || data == null || behaviour == null)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
            behaviour.Tick(target, Time.fixedDeltaTime);
            body.linearVelocity += Separation();
        }

        static readonly Collider2D[] neighbours = new Collider2D[16];

        // 적끼리는 물리 충돌을 끄고(Enemy 레이어) 대신 약한 분리 힘만 준다 (7-B 규칙 1-A).
        // 몸으로 밀면 플레이어 둘레에 1~2마리만 닿지만, 겹치도록 허용하면 포위 시 여러 마리가 동시에 닿는다.
        Vector2 Separation()
        {
            if (data.separationStrength <= 0f || data.separationRadius <= 0f)
                return Vector2.zero;
            int count = Physics2D.OverlapCircle(body.position, data.separationRadius, enemyFilter, neighbours);
            var push = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                var away = body.position - (Vector2)neighbours[i].transform.position;
                float dist = away.magnitude;
                if (neighbours[i].attachedRigidbody == body || dist >= data.separationRadius)
                    continue;
                // 완전히 겹친 경우 방향이 없으므로 임의 방향으로 (한 점에 뭉치지 않게)
                var dir = dist > 0.0001f ? away / dist : UnityEngine.Random.insideUnitCircle.normalized;
                push += dir * (1f - dist / data.separationRadius);
            }
            return push * data.separationStrength;
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
