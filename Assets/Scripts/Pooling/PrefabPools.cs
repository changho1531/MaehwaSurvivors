using System.Collections.Generic;
using UnityEngine;

namespace Game.Pooling
{
    /// <summary>
    /// 프리팹별 공유 풀. 적이 쏘거나 까는 것(궁수 화살, 저주 장판, 보스 혈기탄 · 충격파)은 쏘는 개체가 풀로 돌아가도
    /// 끝까지 남아야 하므로 개체가 아니라 프리팹 단위로 풀을 둔다. 같은 프리팹이면 모든 개체가 한 풀을 나눠 쓴다.
    /// 루트가 씬 오브젝트라 씬이 바뀌면 함께 파괴되고, 그다음 요청 때 새로 만든다.
    /// </summary>
    public static class PrefabPools
    {
        interface IPool
        {
            void ReleaseAll();
        }

        sealed class Entry<T> : IPool where T : Component
        {
            public ComponentPool<T> Pool;
            public void ReleaseAll() => Pool.ReleaseAll();
        }

        static readonly Dictionary<Component, IPool> pools = new();
        static Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pools.Clear();
            root = null;
        }

        public static ComponentPool<T> For<T>(T prefab, int prewarmCount = 0) where T : Component
        {
            if (root == null)
            {
                // 이전 씬의 풀은 오브젝트와 함께 파괴됐으므로 목록도 비운다
                pools.Clear();
                root = new GameObject("[Pool] Shared").transform;
            }
            if (pools.TryGetValue(prefab, out var existing))
                return ((Entry<T>)existing).Pool;

            var entry = new Entry<T> { Pool = new ComponentPool<T>(prefab, root, prewarmCount) };
            pools.Add(prefab, entry);
            return entry.Pool;
        }

        /// <summary>공유 풀의 활성 오브젝트를 모두 되돌린다 (보스 처치 시 화면 정리, 8-C절).</summary>
        public static void ReleaseAll()
        {
            if (root == null)
                return;
            foreach (var pool in pools.Values)
                pool.ReleaseAll();
        }
    }
}
