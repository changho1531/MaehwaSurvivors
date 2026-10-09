using System;
using Game.Enemies;
using Game.Pooling;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 투사체형 무기. 사거리 안에 적이 있으면 가장 가까운 적을 향해 투사체를 풀에서 꺼내 발사한다.
    /// 쿨타임 루프는 Weapon 베이스가 담당하고, 여기서는 "누구를 향해 무엇을 쏘나"만 구현한다.
    /// </summary>
    public class ProjectileWeapon : Weapon
    {
        [SerializeField] ProjectileWeaponData data;

        ComponentPool<Projectile> pool;

        /// <summary>(발사한 투사체, 조준한 적)</summary>
        public event Action<Projectile, Enemy> Fired;

        public ProjectileWeaponData Data => data;
        public ComponentPool<Projectile> Pool => pool;

        protected override float Cooldown => data.cooldown;

        void Awake()
        {
            var root = new GameObject($"[Pool] {data.projectilePrefab.name}").transform;
            pool = new ComponentPool<Projectile>(data.projectilePrefab, root, data.prewarmCount);
        }

        protected override bool TryStartAttack()
        {
            var target = EnemyRegistry.FindNearest(transform.position, data.range);
            if (target == null)
                return false;

            Fire(target);
            return true;
        }

        void Fire(Enemy target)
        {
            var origin = transform.position;
            var direction = (Vector2)(target.transform.position - origin);
            if (direction == Vector2.zero)
                direction = Vector2.right;

            var projectile = pool.Get(origin);
            projectile.Launch(direction, data.projectileSpeed, data.damage, data.projectileLifetime, pool.Release);
            Fired?.Invoke(projectile, target);
        }
    }
}
