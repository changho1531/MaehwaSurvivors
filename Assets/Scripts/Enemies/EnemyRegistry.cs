using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>현재 살아 있는 적 목록. 무기의 타겟 탐색이 씬 전체 검색 없이 이 목록만 본다.</summary>
    public static class EnemyRegistry
    {
        static readonly List<Enemy> active = new();

        public static IReadOnlyList<Enemy> Active => active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            active.Clear();
        }

        public static void Register(Enemy enemy)
        {
            if (!active.Contains(enemy))
                active.Add(enemy);
        }

        public static void Unregister(Enemy enemy)
        {
            active.Remove(enemy);
        }

        /// <summary>from 기준 maxRange 이내에서 가장 가까운 적. 없으면 null.</summary>
        public static Enemy FindNearest(Vector2 from, float maxRange)
        {
            Enemy nearest = null;
            float bestSqr = maxRange * maxRange;

            foreach (var enemy in active)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                float sqr = ((Vector2)enemy.transform.position - from).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }
}
