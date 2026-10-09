using System;
using Game.Enemies;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>직선으로 날아가 처음 닿은 적에게 데미지를 주고 풀로 돌아가는 투사체.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        Rigidbody2D body;
        Vector2 velocity;
        float damage;
        float lifeRemaining;
        bool live;
        Action<Projectile> releaseToPool;

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

        public void Launch(Vector2 direction, float speed, float hitDamage, float lifetime, Action<Projectile> release)
        {
            velocity = direction.normalized * speed;
            damage = hitDamage;
            lifeRemaining = lifetime;
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
            if (enemy == null || !enemy.IsAlive)
                return;

            enemy.TakeDamage(damage);
            Despawn();
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
