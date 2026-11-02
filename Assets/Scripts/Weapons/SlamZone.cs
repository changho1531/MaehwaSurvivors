using System;
using System.Collections.Generic;
using Game.Enemies;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 낙매진각 최대 레벨 장판 (설계서 7-E 규칙 8). 생성 위치에 고정되어 지속 시간 동안 원 안의 적에게 시간에 비례한 데미지를 주고 풀로 돌아간다.
    /// 생성 때 받은 수치로 끝까지 가므로 지속 중 레벨업의 영향을 받지 않는다. 장판끼리는 각자 따로 데미지를 준다.
    /// </summary>
    public class SlamZone : MonoBehaviour
    {
        [SerializeField] Transform art;
        [Tooltip("아트 스케일 1일 때 그림 속 장판 반경(월드 단위). 판정 반경에 맞춰 스케일한다")]
        [SerializeField, Min(0.01f)] float artRadiusAtScale1 = 2.45f;

        readonly List<Enemy> targets = new();
        float radius;
        float remaining;
        float damagePerSecond;
        Action<SlamZone> release;

        public void Begin(float zoneRadius, float seconds, float dps, Action<SlamZone> releaseToPool)
        {
            radius = zoneRadius;
            remaining = seconds;
            damagePerSecond = dps;
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
                release?.Invoke(this);
        }
    }
}
