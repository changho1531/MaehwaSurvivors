using System.Collections.Generic;
using Game.Enemies;
using Game.Pooling;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 적 처치 → 경험치 구슬 생성 (설계서 6-A절 규칙 1~2, 8).
    /// Enemy.Killed 이벤트만 구독하므로 적·무기 코드는 XP 시스템의 존재를 모른다 (Observer).
    /// 생성은 0.3초(게임 시간) 뒤에 하므로 일시정지 중에는 대기 타이머도 멈춘다.
    /// </summary>
    public class XpDropper : MonoBehaviour
    {
        struct PendingDrop
        {
            public float Remaining;
            public Vector3 Position;
            public int Value;
            public Color Color;
        }

        [SerializeField] XpSettings settings;

        readonly List<PendingDrop> pending = new();
        ComponentPool<XpOrb> pool;

        public XpSettings Settings => settings;
        public ComponentPool<XpOrb> Pool => pool;
        public int PendingCount => pending.Count;

        /// <summary>성장 완료 후에는 더 이상 구슬을 만들지 않는다.</summary>
        public bool DropsStopped { get; private set; }

        void Awake()
        {
            var root = new GameObject("[Pool] XpOrbs").transform;
            // 상한 없음: 구슬은 습득 전까지 사라지지 않으므로 부족하면 늘리기만 한다 (6-A절 풀 정책).
            pool = new ComponentPool<XpOrb>(settings.orbPrefab, root, settings.prewarmCount);
        }

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
        }

        void OnEnemyKilled(Enemy enemy)
        {
            var data = enemy.Data;
            if (DropsStopped || data == null || data.xpAmount <= 0)
                return;

            // 위치는 지금 기록한다. 적은 곧 풀로 돌아가 다른 곳에서 재사용되기 때문.
            pending.Add(new PendingDrop
            {
                Remaining = settings.spawnDelay,
                Position = enemy.transform.position + Vector3.up * settings.spawnOffsetY,
                Value = data.xpAmount,
                Color = data.xpOrbColor
            });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || pending.Count == 0)
                return;

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var drop = pending[i];
                drop.Remaining -= dt;
                if (drop.Remaining > 0f)
                {
                    pending[i] = drop;
                    continue;
                }

                SpawnOrb(drop.Position, drop.Value, drop.Color);
                pending.RemoveAt(i);
            }
        }

        public XpOrb SpawnOrb(Vector3 position, int value, Color color)
        {
            var orb = pool.Get(position);
            orb.Init(value, color, pool.Release);
            return orb;
        }

        /// <summary>성장 완료: 대기 중인 생성 취소 + 맵의 구슬 전부 풀 반환 + 이후 드롭 중지 (6-A절 규칙 8).</summary>
        public void StopDropsAndClear()
        {
            DropsStopped = true;
            pending.Clear();
            pool.ReleaseAll();
        }
    }
}
