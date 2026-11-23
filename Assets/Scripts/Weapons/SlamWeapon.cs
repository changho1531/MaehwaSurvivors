using System;
using System.Collections.Generic;
using Game.Enemies;
using Game.Pooling;
using Game.Visuals;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 내려찍기 (낙매진각, 설계서 7-E절). 적 유무와 관계없이 쿨타임마다 즉발: 무기 원점 기준 원 안의 모든 적에게
    /// 데미지를 주고 원의 테두리까지 밀어내도록 요청한다. 이동은 Enemy가 처리하고, 최대 레벨이면 그 자리에 장판을 남긴다.
    /// </summary>
    public class SlamWeapon : Weapon<SlamWeaponData>
    {
        readonly List<Enemy> targets = new();
        ComponentPool<DamageZone> zonePool;
        BurstEffect effect;

        /// <summary>(맞은 적 수)</summary>
        public event Action<int> Slammed;

        public ComponentPool<DamageZone> ZonePool => zonePool;
        public BurstEffect Effect => effect;

        protected override void OnInit()
        {
            if (Data.effectPrefab != null)
            {
                effect = Instantiate(Data.effectPrefab); // 내려찍은 자리에 남으므로 플레이어 자식이 아니다
                effect.gameObject.SetActive(false);
            }
            if (Data.zonePrefab != null)
            {
                var root = new GameObject($"[Pool] {Data.zonePrefab.name}").transform;
                zonePool = new ComponentPool<DamageZone>(Data.zonePrefab, root, Data.zonePrewarmCount);
            }
        }

        protected override bool TryStartAttack()
        {
            Vector2 origin = Origin;
            float radius = Data.GetRadius(Level);
            float speed = Data.GetKnockbackSpeed(Level);
            float damage = Data.GetDamage(Level);

            EnemyRegistry.CollectInRadius(origin, radius, targets);
            foreach (var enemy in targets)
            {
                Vector2 position = enemy.transform.position;
                enemy.TakeDamage(damage);
                enemy.TryKnockback(KnockbackTarget(origin, position, radius, UnityEngine.Random.insideUnitCircle.normalized), speed); // 처치됐으면 Enemy가 무시
            }

            if (effect != null)
                effect.Play(origin, radius);
            if (Level >= Data.maxLevel && zonePool != null)
                zonePool.Get(origin).Begin(radius, Data.zoneSeconds, Data.zoneDamagePerSecond, zonePool.Release);

            Slammed?.Invoke(targets.Count);
            return true; // 적이 없어도 발동
        }

        /// <summary>중심에서 바깥 방향으로 반경만큼 떨어진 지점. 중심과 겹친 적은 방향이 없으므로 fallback 방향으로 민다.</summary>
        public static Vector2 KnockbackTarget(Vector2 origin, Vector2 position, float radius, Vector2 fallback)
        {
            var offset = position - origin;
            var direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : fallback;
            return origin + direction * radius;
        }
    }
}
