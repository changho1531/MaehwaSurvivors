using System;
using System.Collections.Generic;
using Game.Enemies;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 지속 데미지 범위 공통 컴포넌트 (설계서 7-E 장판, 7-F 만천화우). 생성 위치에 고정되어 지속 시간 동안 원 안의 적에게 시간에 비례한 데미지를 주고 풀로 돌아간다.
    /// 생성 때 받은 수치로 끝까지 가므로 지속 중 레벨업의 영향을 받지 않는다. 장판끼리는 각자 따로 데미지를 준다.
    /// </summary>
    public class DamageZone : MonoBehaviour
    {
        [SerializeField] Transform art;
        [Tooltip("아트 스케일 1일 때 그림 속 장판 반경(월드 단위). 판정 반경에 맞춰 스케일한다")]
        [SerializeField, Min(0.01f)] float artRadiusAtScale1 = 2.45f;
        [Tooltip("끝 폭발 연출. 범위가 풀로 돌아간 뒤에도 재생되도록 범위 밖에 따로 둔다")]
        [SerializeField] BurstEffect burstPrefab;

        readonly List<Enemy> targets = new();
        BurstEffect burst;
        float radius;
        float remaining;
        float damagePerSecond;
        float explosionDamage;
        Action<DamageZone> release;

        /// <summary>(폭발 여부, 폭발에 맞은 적 수)</summary>
        public event Action<bool, int> Ended;

        /// <param name="endExplosionDamage">0보다 크면 끝나는 순간 원 안 적 전부에 1회 데미지</param>
        public void Begin(float zoneRadius, float seconds, float dps, Action<DamageZone> releaseToPool, float endExplosionDamage = 0f)
        {
            radius = zoneRadius;
            remaining = seconds;
            damagePerSecond = dps;
            explosionDamage = endExplosionDamage;
            release = releaseToPool;
            if (art != null)
                art.localScale = Vector3.one * (radius / artRadiusAtScale1);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, remaining); // 마지막 프레임이 길어도 총 데미지가 지속 시간 × 초당 데미지를 넘지 않게
            EnemyRegistry.CollectInRadius(transform.position, radius, targets);
            foreach (var enemy in targets)
                enemy.TakeDamage(damagePerSecond * dt);

            remaining -= dt;
            if (remaining <= 0f)
                Finish();
        }

        void Finish()
        {
            bool explodes = explosionDamage > 0f;
            int hits = 0;
            if (explodes)
            {
                EnemyRegistry.CollectInRadius(transform.position, radius, targets);
                hits = targets.Count;
                foreach (var enemy in targets)
                    enemy.TakeDamage(explosionDamage);
                if (burstPrefab != null)
                {
                    if (burst == null)
                        burst = Instantiate(burstPrefab);
                    burst.Play(transform.position, radius);
                }
            }
            Ended?.Invoke(explodes, hits);
            release?.Invoke(this);
        }
    }
}
