using System.Collections.Generic;
using Game.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[기능 8] 오브젝트 풀링 — Instantiate/Destroy 대신 재사용.</summary>
    public class ComponentPoolTests
    {
        readonly List<Object> created = new();
        Transform prefab;
        Transform root;

        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("PoolTestPrefab").transform;
            root = new GameObject("PoolTestRoot").transform;
            created.Add(prefab.gameObject);
            created.Add(root.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created)
                if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Prewarm_CreatesInactiveObjectsUnderParent()
        {
            var pool = new ComponentPool<Transform>(prefab, root, prewarmCount: 5);

            Assert.AreEqual(5, pool.TotalCreated);
            Assert.AreEqual(5, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(5, root.childCount);
            foreach (Transform child in root)
                Assert.IsFalse(child.gameObject.activeSelf);
        }

        [Test]
        public void Get_ActivatesAtPosition()
        {
            var pool = new ComponentPool<Transform>(prefab, root);
            var item = pool.Get(new Vector3(3f, -2f, 0f));

            Assert.IsTrue(item.gameObject.activeSelf);
            Assert.AreEqual(new Vector3(3f, -2f, 0f), item.position);
            Assert.AreEqual(1, pool.CountActive);
            Assert.IsTrue(pool.IsActive(item));
        }

        [Test]
        public void Release_ThenGet_ReusesSameInstance()
        {
            var pool = new ComponentPool<Transform>(prefab, root);
            var first = pool.Get(Vector3.zero);
            pool.Release(first);

            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreEqual(1, pool.CountInactive);

            var second = pool.Get(Vector3.one);
            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.TotalCreated, "재사용 시 새로 Instantiate 하면 안 된다");
        }

        [Test]
        public void ManyCycles_DoNotGrowPool()
        {
            var pool = new ComponentPool<Transform>(prefab, root, prewarmCount: 3);
            for (int i = 0; i < 100; i++)
            {
                var a = pool.Get(Vector3.zero);
                var b = pool.Get(Vector3.zero);
                pool.Release(a);
                pool.Release(b);
            }

            Assert.AreEqual(3, pool.TotalCreated);
        }

        [Test]
        public void DoubleRelease_IsIgnored()
        {
            var pool = new ComponentPool<Transform>(prefab, root);
            var item = pool.Get(Vector3.zero);
            pool.Release(item);
            pool.Release(item);

            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreNotSame(pool.Get(Vector3.zero), pool.Get(Vector3.zero), "같은 오브젝트가 두 번 나가면 안 된다");
        }

        [Test]
        public void MaxActive_LimitsConcurrentObjects()
        {
            var pool = new ComponentPool<Transform>(prefab, root, maxActive: 2);

            Assert.IsTrue(pool.TryGet(Vector3.zero, out var a));
            Assert.IsTrue(pool.TryGet(Vector3.zero, out _));
            Assert.IsFalse(pool.TryGet(Vector3.zero, out var c));
            Assert.IsNull(c);

            pool.Release(a);
            Assert.IsTrue(pool.TryGet(Vector3.zero, out _));
        }

        [Test]
        public void ReleaseAll_ReturnsEveryActiveObject()
        {
            var pool = new ComponentPool<Transform>(prefab, root);
            var items = new List<Transform>();
            for (int i = 0; i < 4; i++)
                items.Add(pool.Get(Vector3.zero));

            pool.ReleaseAll();

            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(4, pool.CountInactive);
            foreach (var item in items)
                Assert.IsFalse(item.gameObject.activeSelf);
            pool.ReleaseAll(); // 빈 상태에서 다시 불러도 문제없음
            Assert.AreEqual(4, pool.CountInactive);
        }

        [Test]
        public void Unlimited_GrowsBeyondPrewarm()
        {
            var pool = new ComponentPool<Transform>(prefab, root, prewarmCount: 2);
            for (int i = 0; i < 5; i++)
                pool.Get(Vector3.zero);

            Assert.AreEqual(5, pool.CountActive, "상한 0 = 부족하면 자동 확장");
            Assert.AreEqual(5, pool.TotalCreated);
        }
    }
}
