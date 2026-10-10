using System.Collections.Generic;
using Game.Pooling;
using Game.Progression;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 일정 주기마다 플레이어 주변 화면 밖 링 위의 무작위 위치에 적을 풀에서 꺼내 배치한다.
    /// 링 반경은 카메라 시야(화면 비율 포함)에 맞춰 계산하므로 해상도가 바뀌어도 화면 안에서 튀어나오지 않는다.
    /// 언제 스폰할지는 경과 시간(SpawnCurve), 무엇을 스폰할지는 플레이어 레벨(SpawnComposition)로 정한다 (8-A절, 두 축 분리).
    /// 적 종류마다 풀을 따로 두고, 고른 종류의 풀에서 꺼낸다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("기본 적 (근거리). 조합 항목에 없는 나머지 비율을 채운다")]
        [SerializeField] Enemy enemyPrefab;
        [Tooltip("플레이어 레벨 → 적 종류 비율 (8-A절). 없으면 기본 적만")]
        [SerializeField] SpawnComposition composition;
        [Tooltip("조합 비율을 고를 때 읽는 플레이어 레벨")]
        [SerializeField] PlayerLevel playerLevel;
        [SerializeField] Transform target;
        [Tooltip("지정하면 이 카메라 시야 바로 바깥을 스폰 링으로 쓴다")]
        [SerializeField] Camera viewCamera;

        [Tooltip("경과 시간 → 스폰 간격 곡선 (7-D절). 없으면 1초 고정")]
        [SerializeField] SpawnCurve spawnCurve;

        [Header("※ 예시값")]
        [Tooltip("카메라가 없을 때 쓰는 최소 반경")]
        [SerializeField, Min(0f)] float fallbackSpawnRadius = 14f;
        [Tooltip("화면 대각선 밖으로 얼마나 더 떨어져 스폰할지")]
        [SerializeField, Min(0f)] float spawnMargin = 1f;
        [SerializeField, Min(0f)] float ringThickness = 2f;
        [Tooltip("적 종류별 풀 프리웜 개수")]
        [SerializeField, Min(0)] int prewarmCount = 30;
        [Tooltip("모든 종류를 합친 동시 활성 상한. 0이면 무제한")]
        [SerializeField, Min(0)] int maxActive = 300;

        readonly Dictionary<Enemy, ComponentPool<Enemy>> pools = new();
        Transform poolRoot;
        ComponentPool<Enemy> pool;
        float timer;

        /// <summary>기본 적(근거리) 풀.</summary>
        public ComponentPool<Enemy> Pool => pool;
        public Enemy BasePrefab => enemyPrefab;
        public SpawnComposition Composition => composition;
        /// <summary>조합 비율에 쓰는 플레이어 레벨. 레벨 시스템이 없으면 1.</summary>
        public int Level => playerLevel != null ? playerLevel.Level : 1;
        /// <summary>게임 시간 기준 경과 초 (일시정지 중 멈춤). 곡선 조회용.</summary>
        public float Elapsed { get; private set; }
        /// <summary>켜면 곡선 대신 곡선의 하한(가장 빠른 간격)으로 스폰 (디버그 F5).</summary>
        public bool ForceFastest { get; set; }
        public float CurrentInterval => spawnCurve == null ? 1f : ForceFastest ? spawnCurve.Fastest : spawnCurve.GetInterval(Elapsed);
        public Transform Target => target;
        public float MinSpawnRadius => viewCamera != null && viewCamera.orthographic ? HalfViewDiagonal(viewCamera) + spawnMargin : fallbackSpawnRadius;
        public float MaxSpawnRadius => MinSpawnRadius + ringThickness;

        public int CountActive
        {
            get
            {
                int count = 0;
                foreach (var p in pools.Values)
                    count += p.CountActive;
                return count;
            }
        }

        static float HalfViewDiagonal(Camera cam)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            return Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
        }

        void Awake()
        {
            poolRoot = new GameObject("[Pool] Enemies").transform;
            pool = PoolOf(enemyPrefab);
            if (composition != null)
                foreach (var entry in composition.entries)
                    PoolOf(entry.prefab); // 처음 등장할 때 생성 비용이 튀지 않게 미리 채움
        }

        /// <summary>적 종류별 풀 (없으면 만든다).</summary>
        public ComponentPool<Enemy> PoolOf(Enemy prefab)
        {
            if (!pools.TryGetValue(prefab, out var p))
            {
                p = new ComponentPool<Enemy>(prefab, poolRoot, prewarmCount);
                pools.Add(prefab, p);
            }
            return p;
        }

        /// <summary>모든 종류의 활성 적을 풀로 되돌린다.</summary>
        public void ReleaseAll()
        {
            foreach (var p in pools.Values)
                p.ReleaseAll();
        }

        void Update()
        {
            Elapsed += Time.deltaTime;
            timer += Time.deltaTime;
            float interval = CurrentInterval;
            while (timer >= interval)
            {
                timer -= interval;
                Spawn();
            }
        }

        /// <summary>현재 레벨의 조합 비율로 종류를 골라 스폰한다.</summary>
        public Enemy Spawn()
        {
            var picked = composition != null ? composition.Pick(Level, Random.value) : null;
            return Spawn(picked != null ? picked : enemyPrefab);
        }

        public Enemy Spawn(Enemy prefab)
        {
            if (target == null)
                return null;
            // 상한에 닿으면 대기열 없이 그 회차를 건너뛴다 (12절, 확정 2026-10-10)
            if (maxActive > 0 && CountActive >= maxActive)
                return null;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float minRadius = MinSpawnRadius;
            float radius = Random.Range(minRadius, minRadius + ringThickness);
            var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            var typePool = PoolOf(prefab);
            var enemy = typePool.Get(target.position + offset);
            enemy.Init(target, typePool.Release);
            return enemy;
        }
    }
}
