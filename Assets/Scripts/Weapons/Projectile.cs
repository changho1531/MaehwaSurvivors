using System;
using System.Collections.Generic;
using Game.Enemies;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 직선으로 날아가 닿은 적에게 데미지를 주는 투사체. 관통 수가 0이면 첫 적에서, 남은 관통 수가 다하면 풀로 돌아간다.
    /// 관통 -1은 무제한(최대 레벨 매화검기) — 비행 거리 끝까지 경로의 모든 적을 맞히고, 같은 적은 한 번만 맞는다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        Rigidbody2D body;
        Vector2 velocity;
        float damage;
        float lifeRemaining;
        bool live;
        Action<Projectile> releaseToPool;
        int pierceRemaining;
        readonly HashSet<Enemy> hitEnemies = new();

        public Vector2 Direction => velocity.normalized;
        public bool IsLive => live;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        void OnDisable()
        {
            live = false;
            releaseToPool = null;
        }

        public void Launch(Vector2 direction, float speed, float hitDamage, float lifetime, int pierce, Action<Projectile> release)
        {
            velocity = direction.normalized * speed;
            damage = hitDamage;
            lifeRemaining = lifetime;
            pierceRemaining = pierce;
            hitEnemies.Clear();
            releaseToPool = release;
            live = true;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            body.position = transform.position;
            body.rotation = angle;
        }

        void FixedUpdate()
        {
            if (!live)
                return;

            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
            lifeRemaining -= Time.fixedDeltaTime;
            if (lifeRemaining <= 0f)
                Despawn();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!live)
                return;

            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive || !hitEnemies.Add(enemy))
                return;

            enemy.TakeDamage(damage);
            if (pierceRemaining < 0)
                return; // 무제한 관통: 비행 거리 끝에서만 사라진다
            if (pierceRemaining == 0)
                Despawn();
            else
                pierceRemaining--;
        }

        void Despawn()
        {
            live = false;
            var release = releaseToPool;
            if (release != null)
                release(this);
            else
                gameObject.SetActive(false);
        }
    }
}
