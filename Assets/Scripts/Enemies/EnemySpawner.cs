using System.Collections.Generic;
using Game.Pooling;
using Game.Player;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 일정 주기마다 플레이어 주변 화면 밖 링 위의 무작위 위치에 적을 풀에서 꺼내 배치한다.
    /// 링 반경은 카메라 시야(화면 비율 포함)에 맞춰 계산하므로 해상도가 바뀌어도 화면 안에서 튀어나오지 않는다.
    /// 언제 스폰할지는 경과 시간(SpawnCurve), 무엇을·몇 마리씩·얼마나 빠르게는 현재 웨이브(WaveDirector)가 정한다 (9-B절).
    /// 웨이브·성장·보스가 모두 시간 기준이라, 레벨 기준이던 적 구성(8-A절)을 웨이브로 대체해 축을 하나로 맞췄다.
    /// 적 종류마다 풀을 따로 두고, 고른 종류의 풀에서 꺼낸다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour, IEnemySource
    {
        [Tooltip("기본 적 (근거리). 웨이브 구성 비율에 없는 나머지를 채운다")]
        [SerializeField] Enemy enemyPrefab;
        [Tooltip("현재 웨이브의 구성·묶음·속도. 없으면 기본 적 1마리씩, 기본 속도")]
        [SerializeField] WaveDirector waves;
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
        [Tooltip("묶음 스폰 시 같은 방향 근처로 퍼지는 폭(월드 단위, 링을 따라)")]
        [SerializeField, Min(0f)] float groupSpread = 1.5f;

        readonly Dictionary<Enemy, ComponentPool<Enemy>> pools = new();
        readonly List<Enemy> prefabBuffer = new();
        Transform poolRoot;
        ComponentPool<Enemy> pool;
        PlayerMovement targetMovement;
        float timer;
        WaveTable.Settings specialsFor;
        float[] specialTimers = System.Array.Empty<float>();

        /// <summary>기본 적(근거리) 풀.</summary>
        public ComponentPool<Enemy> Pool => pool;
        public Enemy BasePrefab => enemyPrefab;
        public WaveDirector Waves => waves;
        /// <summary>게임 시간 기준 경과 초 (일시정지 중 멈춤). 곡선 조회용.</summary>
        public float Elapsed { get; private set; }
        /// <summary>켜면 곡선 대신 곡선의 하한(가장 빠른 간격)으로 스폰 (디버그 F5).</summary>
        public bool ForceFastest { get; set; }

        public float CurrentInterval
        {
            get
            {
                if (spawnCurve == null)
                    return 1f;
                if (ForceFastest)
                    return spawnCurve.Fastest;
                // 보스전 중에는 웨이브 2 구간 간격으로 고정 (9-B 공통 설정)
                if (waves != null && waves.IsBossPhase && waves.Table != null)
                    return spawnCurve.GetInterval(waves.Table.bossIntervalAtSeconds);
                return spawnCurve.GetInterval(Elapsed);
            }
        }

        WaveTable.Settings Settings => waves != null ? waves.CurrentSettings : null;
        public int CurrentGroupSize => Settings != null ? Mathf.Max(1, Settings.groupSize) : 1;
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
            if (target != null)
                targetMovement = target.GetComponent<PlayerMovement>();
            if (waves != null && waves.Table != null)
            {
                waves.Table.CollectPrefabs(prefabBuffer);
                foreach (var prefab in prefabBuffer)
                    PoolOf(prefab); // 처음 등장할 때 생성 비용이 튀지 않게 미리 채움
            }
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
            AdvanceSpecials(Time.deltaTime);
            timer += Time.deltaTime;
            float interval = CurrentInterval;
            while (timer >= interval)
            {
                timer -= interval;
                SpawnGroup();
            }
        }

        /// <summary>
        /// 스폰 1회분: 현재 웨이브의 묶음 수만큼, 같은 방향 근처(링을 따라 groupSpread 폭)에 모아 낸다.
        /// 동시 활성 상한에 닿으면 들어가는 만큼만 생성한다. 생성한 수를 돌려준다.
        /// </summary>
        public int SpawnGroup()
        {
            int size = CurrentGroupSize;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            // 반경은 화면 밖 링 안에서만 고르고 퍼짐은 링을 따라서만 준다 → 묶음 일부가 화면 안에 생기지 않는다
            float halfSpreadAngle = size > 1 ? groupSpread * 0.5f / Mathf.Max(0.01f, MinSpawnRadius) : 0f;
            int spawned = 0;
            for (int i = 0; i < size; i++)
                if (SpawnAt(PickPrefab(), angle + Random.Range(-halfSpreadAngle, halfSpreadAngle)) != null)
                    spawned++;
            return spawned;
        }

        /// <summary>현재 웨이브 구성 비율로 종류를 골라 1마리 스폰한다.</summary>
        public Enemy Spawn() => Spawn(PickPrefab());

        public Enemy Spawn(Enemy prefab) => SpawnAt(prefab, Random.Range(0f, Mathf.PI * 2f));

        Enemy PickPrefab()
        {
            var picked = Settings?.Pick(Random.value);
            return picked != null ? picked : enemyPrefab;
        }

        /// <summary>이동속도 = min(적 기본 속도 × 웨이브 배율, 플레이어 기본 이동속도 × 상한 비율). 생성될 때 정해진다.</summary>
        public float GetSpawnSpeed(Enemy prefab)
        {
            float baseSpeed = prefab.Data != null ? prefab.Data.moveSpeed : 0f;
            if (waves == null || waves.Table == null || Settings == null)
                return baseSpeed;
            float playerBase = targetMovement != null ? targetMovement.MoveSpeed : float.PositiveInfinity;
            return waves.Table.GetEnemySpeed(baseSpeed, Settings.speedMultiplier, playerBase);
        }

        /// <summary>
        /// 특수몹(저주술사) 등장 (8-B 규칙 7). 구성 비율과 따로, 웨이브 표의 간격마다 1마리 — 동시 최대 수에 닿아 있으면 건너뛴다.
        /// 웨이브가 바뀌면(설정이 바뀌면) 그 웨이브의 첫 1마리를 바로 낸다. 보스전 설정에는 특수몹이 없어 새로 나오지 않는다.
        /// 낸 수를 돌려준다. 테스트에서 시간을 건너뛸 때도 쓴다.
        /// </summary>
        public int AdvanceSpecials(float dt)
        {
            var settings = Settings;
            if (settings == null || dt <= 0f)
                return 0;
            var specials = settings.specials ?? System.Array.Empty<WaveTable.Special>();
            if (settings != specialsFor)
            {
                specialsFor = settings;
                specialTimers = new float[specials.Length];
                for (int i = 0; i < specials.Length; i++)
                    specialTimers[i] = specials[i].intervalSeconds; // 웨이브 시작 순간 첫 1마리
            }

            int spawned = 0;
            for (int i = 0; i < specials.Length; i++)
            {
                var special = specials[i];
                specialTimers[i] += dt;
                if (special.prefab == null || specialTimers[i] < special.intervalSeconds)
                    continue;
                specialTimers[i] = 0f;
                if (PoolOf(special.prefab).CountActive >= special.maxAlive)
                    continue;
                if (SpawnAt(special.prefab, Random.Range(0f, Mathf.PI * 2f)) != null)
                    spawned++;
            }
            return spawned;
        }

        /// <summary>적이 부르는 소환 (8-B 규칙 6). 일반 스폰과 같은 풀 · 상한 · 웨이브 속도를 쓴다.</summary>
        public Enemy SpawnNear(Enemy prefab, Vector2 center, float radius)
        {
            if (target == null || prefab == null)
                return null;
            if (maxActive > 0 && CountActive >= maxActive)
                return null; // 상한에 닿으면 들어가는 만큼만

            var typePool = PoolOf(prefab);
            var enemy = typePool.Get(center + Random.insideUnitCircle * radius);
            enemy.SetSource(this);
            enemy.Init(target, typePool.Release, GetSpawnSpeed(prefab));
            return enemy;
        }

        Enemy SpawnAt(Enemy prefab, float angle)
        {
            if (target == null || prefab == null)
                return null;
            // 상한에 닿으면 대기열 없이 그 회차를 건너뛴다 (12절, 확정 2026-10-10)
            if (maxActive > 0 && CountActive >= maxActive)
                return null;

            float minRadius = MinSpawnRadius;
            float radius = Random.Range(minRadius, minRadius + ringThickness);
            var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            var typePool = PoolOf(prefab);
            var enemy = typePool.Get(target.position + offset);
            enemy.SetSource(this);
            enemy.Init(target, typePool.Release, GetSpawnSpeed(prefab));
            return enemy;
        }
    }
}
