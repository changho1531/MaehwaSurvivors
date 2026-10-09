using System;
using Game.Enemies;
using Game.Player;
using Game.Pooling;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 투사체형 무기 (매화검기, 설계서 7절 규칙). 맵 전체에서 가장 가까운 적을 향해, 적이 없으면 바라보는 방향으로
    /// 투사체를 풀에서 꺼내 발사한다. 사거리 제한은 없고 대신 비행 거리 제한으로 너무 먼 적까지 닿는 것을 막는다.
    /// 쿨타임 루프는 Weapon 베이스가 담당하고, 여기서는 "누구를 향해 무엇을 쏘나"만 구현한다.
    /// </summary>
    public class ProjectileWeapon : Weapon<ProjectileWeaponData>
    {
        ComponentPool<Projectile> pool;
        PlayerFacing facing;

        /// <summary>(발사한 투사체, 조준한 적 — 적이 없어 바라보는 방향으로 쐈으면 null)</summary>
        public event Action<Projectile, Enemy> Fired;

        public ComponentPool<Projectile> Pool => pool;

        protected override void OnInit()
        {
            facing = GetComponent<PlayerFacing>();
            var root = new GameObject($"[Pool] {Data.projectilePrefab.name}").transform;
            pool = new ComponentPool<Projectile>(Data.projectilePrefab, root, Data.prewarmCount);
        }

        protected override bool TryStartAttack()
        {
            var origin = (Vector2)transform.position;
            var target = EnemyRegistry.FindNearest(origin, float.PositiveInfinity);

            Vector2 direction;
            if (target != null)
                direction = (Vector2)target.transform.position - origin;
            else
                direction = facing != null ? facing.Direction : PlayerFacing.Initial;
            if (direction == Vector2.zero)
                direction = PlayerFacing.Initial;

            float speed = Data.GetProjectileSpeed(Level);
            var projectile = pool.Get(origin);
            projectile.Launch(direction, speed, Data.GetDamage(Level), Data.maxTravelDistance / speed, pool.Release);
            Fired?.Invoke(projectile, target);
            return true;
        }
    }
}
