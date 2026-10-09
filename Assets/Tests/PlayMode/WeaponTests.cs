using System.Collections;
using System.Collections.Generic;
using Game.Enemies;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[기능 10~11] 1초마다 가장 가까운 적에게 투사체 발사, 맞은 적은 Dead.</summary>
    public class WeaponTests
    {
        readonly InGameScene scene = new();
        readonly List<(float time, Projectile projectile, Enemy target)> shots = new();

        void OnFired(Projectile p, Enemy e) => shots.Add((Time.time, p, e));

        [UnitySetUp]
        public IEnumerator Load()
        {
            shots.Clear();
            yield return scene.Load();
            Assert.IsNotNull(scene.Spawner, "InGame 씬에 EnemySpawner가 없다");
            Assert.IsNotNull(scene.Weapon, "InGame 씬에 AutoWeapon이 없다");
            scene.StopSpawningAndClear();
            scene.Weapon.Fired += OnFired;
        }

        [TearDown]
        public void Unsubscribe()
        {
            if (scene.Weapon != null)
                scene.Weapon.Fired -= OnFired;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
                yield return null;
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator F10_WeaponData_IsAssigned()
        {
            yield return null;
            var data = scene.Weapon.Data;
            Assert.IsNotNull(data, "WeaponData(SO) 연결 누락");
            Assert.IsNotNull(data.projectilePrefab, "투사체 프리팹 연결 누락");
            Assert.AreEqual(1f, data.cooldown, 0.0001f, "요구사항: 1초마다 발사");
            Assert.Greater(scene.Weapon.Pool.TotalCreated, 0, "투사체도 풀을 미리 채워 두어야 한다");
        }

        [UnityTest]
        public IEnumerator F10_NoEnemyInRange_DoesNotFire()
        {
            scene.SpawnAt(new Vector2(scene.Weapon.Data.range + 2f, 0f), chase: false);
            yield return Wait(1.5f);

            Assert.IsEmpty(shots, "사거리 밖 적에게는 쏘지 않는다");
            Assert.AreEqual(0, scene.Weapon.Pool.CountActive);
        }

        [UnityTest]
        public IEnumerator F10_FiresTowardNearestEnemy()
        {
            var far = scene.SpawnAt(new Vector2(-7f, 0f), chase: false);
            var near = scene.SpawnAt(new Vector2(0f, 4f), chase: false);

            yield return WaitUntil(() => shots.Count > 0, 1.5f);

            Assert.IsNotEmpty(shots, "사거리 안에 적이 있으면 발사해야 한다");
            Assert.AreSame(near, shots[0].target, "가장 가까운 적을 조준해야 한다");
            Assert.Greater(Vector2.Dot(shots[0].projectile.Direction, Vector2.up), 0.99f, "투사체가 가까운 적 방향으로 날아가야 한다");
            Assert.IsTrue(far.IsAlive);
        }

        [UnityTest]
        public IEnumerator F10_FiresOncePerSecond()
        {
            // 한 발에 한 마리씩 처치되므로 여러 마리를 서로 다른 방향에 고정해 둔다.
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                scene.SpawnAt(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5f, chase: false);
            }

            yield return Wait(3.3f);

            Assert.That(shots.Count, Is.InRange(3, 4), $"3.3초 동안 3~4발이어야 한다 (실제 {shots.Count})");
            for (int i = 1; i < shots.Count; i++)
            {
                float interval = shots[i].time - shots[i - 1].time;
                Assert.That(interval, Is.EqualTo(1f).Within(0.1f), $"{i}번째 발사 간격 {interval}");
            }
        }

        [UnityTest]
        public IEnumerator F11_HitEnemy_IsDead_AndReturnedToPool()
        {
            var enemy = scene.SpawnAt(new Vector2(4f, 0f), chase: false);
            Enemy killed = null;
            void OnKilled(Enemy e) => killed = e;
            Enemy.Killed += OnKilled;
            try
            {
                yield return WaitUntil(() => killed != null, 2f);

                Assert.AreSame(enemy, killed, "투사체에 맞은 적이 처치되어야 한다");
                Assert.IsFalse(enemy.IsAlive);
                Assert.IsFalse(enemy.gameObject.activeSelf, "Dead 상태의 적은 화면에서 사라진다");
                Assert.IsFalse(scene.Spawner.Pool.IsActive(enemy), "Dead 상태의 적은 풀로 반환");

                yield return new WaitForFixedUpdate();
                Assert.AreEqual(0, scene.Weapon.Pool.CountActive, "명중한 투사체도 풀로 반환");
                Assert.AreEqual(1, shots.Count, "한 발로 처치되어야 한다");
            }
            finally
            {
                Enemy.Killed -= OnKilled;
            }
        }

        [UnityTest]
        public IEnumerator F11_ProjectileHitsOnlyOneEnemy()
        {
            // 일직선으로 두 마리를 세워도 투사체 하나는 한 마리만 맞힌다 (관통은 무기 강화 단계에서).
            var first = scene.SpawnAt(new Vector2(3f, 0f), chase: false);
            var second = scene.SpawnAt(new Vector2(4.2f, 0f), chase: false);

            yield return WaitUntil(() => !first.IsAlive, 1.5f);
            yield return Wait(0.3f);

            Assert.IsFalse(first.IsAlive);
            Assert.IsTrue(second.IsAlive, "두 번째 적은 다음 발사 전까지 살아 있어야 한다");
        }

        [UnityTest]
        public IEnumerator F11_PartialDamage_DoesNotKill()
        {
            scene.Weapon.enabled = false;
            var enemy = scene.SpawnAt(new Vector2(4f, 0f), chase: false);
            yield return null;

            enemy.TakeDamage(enemy.Data.maxHp * 0.5f);
            Assert.IsTrue(enemy.IsAlive);
            enemy.TakeDamage(enemy.Data.maxHp * 0.5f);
            Assert.IsFalse(enemy.IsAlive);
        }

        [UnityTest]
        public IEnumerator F11_MissedProjectile_ExpiresAndReturnsToPool()
        {
            var enemy = scene.SpawnAt(new Vector2(4f, 0f), chase: false);
            yield return WaitUntil(() => shots.Count > 0, 1.5f);
            Assert.IsNotEmpty(shots);

            // 발사 직후 적을 멀리 치워 빗나가게 만든다.
            var away = new Vector2(0f, 50f);
            enemy.GetComponent<Rigidbody2D>().position = away;
            enemy.transform.position = away;
            scene.Weapon.enabled = false;

            yield return Wait(scene.Weapon.Data.projectileLifetime + 0.2f);

            Assert.IsFalse(shots[0].projectile.IsLive);
            Assert.AreEqual(0, scene.Weapon.Pool.CountActive, "수명이 다한 투사체는 풀로 반환");
            Assert.IsTrue(enemy.IsAlive);
        }
    }
}
