using System;
using Game.Player;
using Game.Pooling;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 혈영궁수의 화살 (설계서 8-A절 규칙 5·6). 직선 비행, 플레이어에게 닿으면 1회 데미지 후 풀 반환, 빗나가면 비행 거리 끝에서 반환.
    /// 콜라이더 없이 플레이어와의 거리만 보므로 다른 적·플레이어 공격과는 서로 영향이 없다.
    /// 게임 시간(deltaTime)으로 움직여 일시정지·레벨업·게임오버 중에는 멈춘다.
    /// </summary>
    public class Arrow : MonoBehaviour
    {
        [Tooltip("화살 중심과 플레이어 원점(발, 판정 반경 0.3) 사이 이 거리 안이면 명중")]
        [SerializeField, Min(0.01f)] float hitRadius = 0.4f;

        PlayerHealth target;
        Vector2 velocity;
        float damage;
        float travelRemaining;
        Action<Arrow> releaseToPool;

        public bool IsLive { get; private set; }
        public Vector2 Velocity => velocity;

        /// <summary>화살 프리팹별 공유 풀. 쏜 개체가 처치돼도 날아가던 화살은 끝까지 간다.</summary>
        public static ComponentPool<Arrow> PoolFor(Arrow prefab) => PrefabPools.For(prefab, 20);

        public void Launch(Vector2 direction, float speed, float hitDamage, float travelDistance, PlayerHealth player, Action<Arrow> release)
        {
            velocity = direction.normalized * speed;
            damage = hitDamage;
            travelRemaining = travelDistance;
            target = player;
            releaseToPool = release;
            IsLive = true;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        void OnDisable()
        {
            IsLive = false;
            target = null;
            releaseToPool = null;
        }

        void Update()
        {
            if (!IsLive)
                return;
            float dt = Time.deltaTime;
            var step = velocity * dt;
            transform.position += (Vector3)step;
            travelRemaining -= step.magnitude;

            if (target != null && ((Vector2)(target.transform.position - transform.position)).sqrMagnitude <= hitRadius * hitRadius)
            {
                target.TakeDamage(damage); // 플레이어 관통 없음: 1회 데미지 후 사라짐
                Despawn();
            }
            else if (travelRemaining <= 0f)
                Despawn();
        }

        void Despawn()
        {
            IsLive = false;
            var release = releaseToPool;
            if (release != null)
                release(this);
            else
                gameObject.SetActive(false);
        }
    }
}
