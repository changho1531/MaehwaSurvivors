using System;
using Game.Enemies;
using Game.Pooling;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 자동 공격 무기 (설계서 7절의 WeaponController 역할).
    /// 쿨타임이 끝났고 사거리 안에 적이 있으면 가장 가까운 적을 향해 투사체를 풀에서 꺼내 발사한다.
    /// 조건이 맞지 않으면 쿨타임이 끝난 상태로 대기한다.
    /// </summary>
    public class AutoWeapon : MonoBehaviour
    {
        [SerializeField] WeaponData data;

        ComponentPool<Projectile> pool;
        float cooldownRemaining;

        /// <summary>(발사한 투사체, 조준한 적)</summary>
        public event Action<Projectile, Enemy> Fired;

        public WeaponData Data => data;
        public ComponentPool<Projectile> Pool => pool;

        void Awake()
        {
            var root = new GameObject($"[Pool] {data.projectilePrefab.name}").transform;
            pool = new ComponentPool<Projectile>(data.projectilePrefab, root, data.prewarmCount);
        }

        void Update()
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
                return;
            }

            var target = EnemyRegistry.FindNearest(transform.position, data.range);
            if (target == null)
                return;

            Fire(target);
            cooldownRemaining = data.cooldown;
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
