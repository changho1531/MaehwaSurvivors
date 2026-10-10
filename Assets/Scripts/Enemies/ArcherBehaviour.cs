using System;
using Game.Player;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 혈영궁수 (설계서 8-A절 규칙 1~4, 8). 사거리 밖이면 다가가고, 사거리 안이면 멈춰서
    /// 떨림(0.5초, 쏘기 직전 신호) → 그 순간의 플레이어 위치로 화살 1발 → 대기 를 반복한다. 가까이 와도 물러나지 않는다.
    /// 떨림 도중 플레이어가 사거리를 벗어나면 그 발은 취소하고 다시 따라간다.
    /// </summary>
    public class ArcherBehaviour : EnemyBehaviour
    {
        [Tooltip("떨림 연출로 흔들 그림(자식). 몸통(물리)을 흔들지 않도록 그림만 움직인다")]
        [SerializeField] Transform art;

        RangedEnemyData data;
        PlayerHealth targetHealth;
        PlayerMovement targetMovement;
        Transform cachedTarget;
        float windupRemaining = -1f; // < 0 이면 떨림 중이 아님
        float cooldownRemaining;
        float shakeTime;

        /// <summary>(발사한 화살)</summary>
        public event Action<Arrow> Fired;

        public bool IsWindingUp => windupRemaining >= 0f;
        public RangedEnemyData Data => data;

        protected override void Awake()
        {
            base.Awake();
            data = Owner.Data as RangedEnemyData;
        }

        public override void ResetState()
        {
            windupRemaining = -1f;
            cooldownRemaining = 0f;
            SetShake(0f);
        }

        public override void Tick(Transform target, float dt)
        {
            if (data == null)
                return;
            CacheTarget(target);
            cooldownRemaining -= dt;

            var toTarget = (Vector2)target.position - Body.position;
            if (toTarget.sqrMagnitude > data.attackRange * data.attackRange)
            {
                // 사거리 밖: 따라간다. 떨림 중이었으면 그 발은 취소 (규칙 4)
                windupRemaining = -1f;
                SetShake(0f);
                Body.linearVelocity = toTarget.normalized * data.moveSpeed;
                return;
            }

            Body.linearVelocity = Vector2.zero; // 사거리 안: 멈춰서 쏜다, 물러나지 않음 (규칙 3)

            if (!IsWindingUp)
            {
                // 다시 들어와도 발사 간격은 지킨다: 사거리를 들락날락하며 연사하지 못하게
                if (cooldownRemaining > 0f)
                    return;
                windupRemaining = data.windupSeconds;
                shakeTime = 0f;
            }

            windupRemaining -= dt;
            shakeTime += dt;
            SetShake(Mathf.Sin(shakeTime * data.shakeFrequency * Mathf.PI * 2f) * data.shakeAmplitude);
            if (windupRemaining > 0f)
                return;

            Fire(toTarget);
            windupRemaining = -1f;
            SetShake(0f);
            cooldownRemaining = data.fireInterval - data.windupSeconds; // 대기 1.5 + 다음 떨림 0.5 = 발사 간격 2초
        }

        void Fire(Vector2 toTarget)
        {
            if (data.arrowPrefab == null)
                return;
            // 쏘는 순간의 플레이어 위치를 향해 곧게 (규칙 2). 속도는 플레이어 최고 속도의 배수라 움직이면 피할 수 있다.
            float playerSpeed = targetMovement != null ? targetMovement.MoveSpeed : 5f;
            var pool = Arrow.PoolFor(data.arrowPrefab);
            var arrow = pool.Get(Body.position);
            arrow.Launch(toTarget, playerSpeed * data.arrowSpeedMultiplier, data.arrowDamage, data.ArrowTravelDistance, targetHealth, pool.Release);
            Fired?.Invoke(arrow);
        }

        void CacheTarget(Transform target)
        {
            if (cachedTarget == target)
                return;
            cachedTarget = target;
            targetHealth = target.GetComponent<PlayerHealth>();
            targetMovement = target.GetComponent<PlayerMovement>();
        }

        void SetShake(float x)
        {
            if (art != null)
                art.localPosition = new Vector3(x, 0f, 0f);
        }
    }
}
