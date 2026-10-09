using System.Collections.Generic;
using Game.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[기능 10] 가장 가까운 적 탐색.</summary>
    public class EnemyRegistryTests
    {
        readonly List<GameObject> created = new();

        [SetUp]
        public void SetUp() => EnemyRegistry.Clear();

        [TearDown]
        public void TearDown()
        {
            EnemyRegistry.Clear();
            foreach (var go in created)
                if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        Enemy MakeEnemy(Vector2 position, float hp = 10f)
        {
            var go = new GameObject("TestEnemy");
            go.transform.position = position;
            created.Add(go);
            var enemy = go.AddComponent<Enemy>();
            // 에디트 모드에서는 OnEnable이 불리지 않으므로 HP를 직접 채워 살아 있는 상태로 만든다.
            typeof(Enemy).GetProperty(nameof(Enemy.CurrentHp))!.SetValue(enemy, hp);
            EnemyRegistry.Register(enemy);
            return enemy;
        }

        [Test]
        public void Empty_ReturnsNull()
        {
            Assert.IsNull(EnemyRegistry.FindNearest(Vector2.zero, 100f));
        }

        [Test]
        public void ReturnsClosestWithinRange()
        {
            MakeEnemy(new Vector2(5f, 0f));
            var near = MakeEnemy(new Vector2(-2f, 1f));
            MakeEnemy(new Vector2(0f, 7f));

            Assert.AreSame(near, EnemyRegistry.FindNearest(Vector2.zero, 100f));
        }

        [Test]
        public void IgnoresEnemiesOutOfRange()
        {
            MakeEnemy(new Vector2(10f, 0f));
            Assert.IsNull(EnemyRegistry.FindNearest(Vector2.zero, 9f));
        }

        [Test]
        public void IgnoresDeadEnemies()
        {
            MakeEnemy(new Vector2(1f, 0f), hp: 0f);
            var alive = MakeEnemy(new Vector2(4f, 0f));

            Assert.AreSame(alive, EnemyRegistry.FindNearest(Vector2.zero, 100f));
        }

        [Test]
        public void UnregisteredEnemy_IsNotFound()
        {
            var e = MakeEnemy(new Vector2(1f, 0f));
            EnemyRegistry.Unregister(e);
            Assert.IsNull(EnemyRegistry.FindNearest(Vector2.zero, 100f));
        }
    }
}
