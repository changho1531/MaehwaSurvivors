using System.Collections;
using System.Collections.Generic;
using Game.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[기능 7~9] 임시 근거리 적, 프리팹 + 풀링, 1초 주기 랜덤 스폰, 플레이어 추적.</summary>
    public class EnemyTests
    {
        readonly InGameScene scene = new();

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            Assert.IsNotNull(scene.Spawner, "InGame 씬에 EnemySpawner가 없다");
            if (scene.Weapon != null)
                scene.Weapon.enabled = false; // 무기가 적을 처치하면 카운트가 흔들리므로 끈다.
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator F7_EnemyObject_IsSetUp()
        {
            scene.StopSpawningAndClear();
            var enemy = scene.Spawner.Spawn();
            yield return null;

            Assert.IsNotNull(enemy.Data, "EnemyData(SO) 연결 누락");
            Assert.IsNotNull(enemy.GetComponent<SpriteRenderer>()?.sprite);
            Assert.AreEqual(RigidbodyType2D.Dynamic, enemy.GetComponent<Rigidbody2D>().bodyType);
            Assert.AreEqual(0f, enemy.GetComponent<Rigidbody2D>().gravityScale, "탑다운이라 중력 0");
            var collider = enemy.GetComponent<Collider2D>();
            Assert.IsNotNull(collider);
            Assert.IsFalse(collider.isTrigger, "적끼리/플레이어와 물리적으로 겹치지 않아야 한다");
            Assert.AreEqual(enemy.Data.maxHp, enemy.CurrentHp);
            Assert.IsTrue(enemy.IsAlive);
        }

        [UnityTest]
        public IEnumerator F8_Spawning_UsesPrewarmedPool_NotInstantiate()
        {
            int createdBefore = scene.Spawner.Pool.TotalCreated;
            Assert.Greater(createdBefore, 0, "풀을 미리 채워 두어야 한다");

            yield return Wait(3.5f);

            Assert.Greater(scene.Spawner.Pool.CountActive, 0);
            Assert.AreEqual(createdBefore, scene.Spawner.Pool.TotalCreated, "프리웜 개수 이내에서는 새로 생성하지 않아야 한다");
            foreach (var enemy in EnemyRegistry.Active)
                Assert.AreEqual("[Pool] Enemies", enemy.transform.parent?.name, "풀에서 나온 적이어야 한다");
        }

        [UnityTest]
        public IEnumerator F8_KilledEnemy_ReturnsToPool_AndIsReused()
        {
            scene.StopSpawningAndClear();
            int created = scene.Spawner.Pool.TotalCreated;

            Enemy killed = null;
            void OnKilled(Enemy e) => killed = e;
            Enemy.Killed += OnKilled;
            try
            {
                var enemy = scene.Spawner.Spawn();
                yield return null;
                enemy.TakeDamage(enemy.Data.maxHp);
                yield return null;

                Assert.AreSame(enemy, killed, "Killed 이벤트가 발행되어야 한다");
                Assert.IsFalse(enemy.gameObject.activeSelf, "처치된 적은 비활성화");
                Assert.IsFalse(scene.Spawner.Pool.IsActive(enemy), "처치된 적은 풀로 반환");
                Assert.IsFalse(((IList<Enemy>)new List<Enemy>(EnemyRegistry.Active)).Contains(enemy));

                // 같은 인스턴스를 다시 꺼내면 HP가 초기화되어야 한다.
                var reused = scene.Spawner.Spawn();
                Assert.AreEqual(created, scene.Spawner.Pool.TotalCreated);
                Assert.AreEqual(reused.Data.maxHp, reused.CurrentHp);
            }
            finally
            {
                Enemy.Killed -= OnKilled;
            }
        }

        [UnityTest]
        public IEnumerator F9_SpawnsAboutOncePerSecond()
        {
            scene.StopSpawningAndClear();
            scene.Spawner.enabled = true;

            yield return Wait(3.5f);

            int count = scene.Spawner.Pool.CountActive;
            Assert.That(count, Is.InRange(3, 4), $"1초 주기라면 3.5초 동안 3~4마리여야 한다 (실제 {count})");
        }

        [UnityTest]
        public IEnumerator F9_SpawnPositions_AreRandomOnRingOutsidePlayer()
        {
            scene.StopSpawningAndClear();
            var center = (Vector2)scene.Player.transform.position;
            var angles = new HashSet<int>();

            for (int i = 0; i < 20; i++)
            {
                var enemy = scene.Spawner.Spawn();
                var offset = (Vector2)enemy.transform.position - center;
                Assert.That(offset.magnitude, Is.InRange(scene.Spawner.MinSpawnRadius - 0.01f, scene.Spawner.MaxSpawnRadius + 0.01f));
                angles.Add(Mathf.RoundToInt(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg / 30f));
            }

            Assert.GreaterOrEqual(angles.Count, 5, "스폰 방향이 무작위로 퍼져야 한다");

            // 스폰 지점은 화면 밖이어야 한다.
            float halfHeight = scene.Camera.orthographicSize;
            float halfWidth = halfHeight * scene.Camera.aspect;
            Assert.Greater(scene.Spawner.MinSpawnRadius, Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight), "스폰 반경이 화면 대각선보다 커야 한다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator F9_Enemy_ChasesPlayer()
        {
            scene.StopSpawningAndClear();
            var enemy = scene.SpawnAt(new Vector2(8f, 0f), chase: true);
            float before = Vector2.Distance(enemy.transform.position, scene.Player.transform.position);

            yield return Wait(1f);

            float after = Vector2.Distance(enemy.transform.position, scene.Player.transform.position);
            float expected = enemy.Data.moveSpeed * 1f;
            Assert.That(before - after, Is.EqualTo(expected).Within(expected * 0.25f), $"1초에 {expected}만큼 다가와야 한다 (실제 {before - after})");
        }

        [UnityTest]
        public IEnumerator F9_Enemy_RetargetsWhenPlayerMoves()
        {
            scene.StopSpawningAndClear();
            var enemy = scene.SpawnAt(new Vector2(6f, 0f), chase: true);
            yield return Wait(0.2f);

            // 플레이어를 적의 위쪽으로 순간 이동 → 적의 진행 방향도 위로 바뀌어야 한다.
            var playerBody = scene.Player.GetComponent<Rigidbody2D>();
            var newPos = (Vector2)enemy.transform.position + new Vector2(0f, 6f);
            playerBody.position = newPos;
            scene.Player.transform.position = newPos;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var velocity = enemy.GetComponent<Rigidbody2D>().linearVelocity;
            Assert.Greater(Vector2.Dot(velocity.normalized, Vector2.up), 0.9f, $"적이 플레이어 쪽(위)으로 방향을 바꿔야 한다 (velocity {velocity})");
        }

        [UnityTest]
        public IEnumerator F9_Enemies_StopAtPlayer_WithoutPushingThrough()
        {
            scene.StopSpawningAndClear();
            scene.SpawnAt(new Vector2(3f, 0f), chase: true);
            scene.SpawnAt(new Vector2(-3f, 0f), chase: true);

            yield return Wait(2.5f);

            Assert.That(Vector2.Distance(scene.Player.transform.position, Vector2.zero), Is.LessThan(0.01f), "적에게 밀려 플레이어가 움직이면 안 된다");
            foreach (var enemy in EnemyRegistry.Active)
                Assert.Greater(Vector2.Distance(enemy.transform.position, scene.Player.transform.position), 0.5f, "적이 플레이어를 뚫고 지나가면 안 된다");
        }
    }
}
