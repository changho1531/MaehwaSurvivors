using System.Collections.Generic;
using UnityEngine;

namespace Game.Pooling
{
    /// <summary>
    /// 프리팹 기반 오브젝트 풀. 비활성 오브젝트가 있으면 재사용하고, 없으면 새로 만들어 풀에 등록한다.
    /// maxActive가 0보다 크면 동시 활성 수를 그 값으로 제한한다.
    /// </summary>
    public class ComponentPool<T> where T : Component
    {
        readonly T prefab;
        readonly Transform parent;
        readonly int maxActive;
        readonly Stack<T> inactive = new();
        readonly HashSet<T> active = new();

        public int CountActive => active.Count;
        public int CountInactive => inactive.Count;

        /// <summary>풀이 지금까지 Instantiate 한 총 개수. 재사용이 일어나면 늘지 않는다.</summary>
        public int TotalCreated { get; private set; }

        public ComponentPool(T prefab, Transform parent, int prewarmCount = 0, int maxActive = 0)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxActive = maxActive;

            for (int i = 0; i < prewarmCount; i++)
                inactive.Push(Create());
        }

        public bool IsActive(T item) => active.Contains(item);

        public bool TryGet(Vector3 position, out T item)
        {
            if (maxActive > 0 && active.Count >= maxActive)
            {
                item = null;
                return false;
            }

            item = inactive.Count > 0 ? inactive.Pop() : Create();
            item.transform.SetPositionAndRotation(position, Quaternion.identity);
            active.Add(item);
            item.gameObject.SetActive(true);
            return true;
        }

        public T Get(Vector3 position)
        {
            TryGet(position, out var item);
            return item;
        }

        public void Release(T item)
        {
            // 이중 반환이나 이미 파괴된 오브젝트는 무시한다.
            if (item == null || !active.Remove(item))
                return;

            item.gameObject.SetActive(false);
            inactive.Push(item);
        }

        T Create()
        {
            var item = Object.Instantiate(prefab, parent);
            item.gameObject.SetActive(false);
            TotalCreated++;
            return item;
        }
    }
}
