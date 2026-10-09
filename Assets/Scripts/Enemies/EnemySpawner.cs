using Game.Pooling;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 일정 주기마다 플레이어 주변 화면 밖 링 위의 무작위 위치에 적을 풀에서 꺼내 배치한다.
    /// 링 반경은 카메라 시야(화면 비율 포함)에 맞춰 계산하므로 해상도가 바뀌어도 화면 안에서 튀어나오지 않는다.
    /// 웨이브/적 조합 테이블은 아직 없음 — 단일 적 프리팹만 스폰한다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] Enemy enemyPrefab;
        [SerializeField] Transform target;
        [Tooltip("지정하면 이 카메라 시야 바로 바깥을 스폰 링으로 쓴다")]
        [SerializeField] Camera viewCamera;

        [Header("※ 예시값")]
        [SerializeField, Min(0.05f)] float spawnInterval = 1f;
        [Tooltip("카메라가 없을 때 쓰는 최소 반경")]
        [SerializeField, Min(0f)] float fallbackSpawnRadius = 14f;
        [Tooltip("화면 대각선 밖으로 얼마나 더 떨어져 스폰할지")]
        [SerializeField, Min(0f)] float spawnMargin = 1f;
        [SerializeField, Min(0f)] float ringThickness = 2f;
        [SerializeField, Min(0)] int prewarmCount = 30;
        [Tooltip("동시 활성 상한. 0이면 무제한")]
        [SerializeField, Min(0)] int maxActive = 300;

        ComponentPool<Enemy> pool;
        float timer;

        public ComponentPool<Enemy> Pool => pool;
        public Transform Target => target;
        public float MinSpawnRadius => viewCamera != null && viewCamera.orthographic ? HalfViewDiagonal(viewCamera) + spawnMargin : fallbackSpawnRadius;
        public float MaxSpawnRadius => MinSpawnRadius + ringThickness;

        static float HalfViewDiagonal(Camera cam)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            return Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
        }

        void Awake()
        {
            var root = new GameObject("[Pool] Enemies").transform;
            pool = new ComponentPool<Enemy>(enemyPrefab, root, prewarmCount, maxActive);
        }

        void Update()
        {
            timer += Time.deltaTime;
            while (timer >= spawnInterval)
            {
                timer -= spawnInterval;
                Spawn();
            }
        }

        public Enemy Spawn()
        {
            if (target == null)
                return null;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float minRadius = MinSpawnRadius;
            float radius = Random.Range(minRadius, minRadius + ringThickness);
            var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            // TODO: 설계서 12절 — 동시 활성 상한 초과 시 대기열 처리. 현재는 해당 회차를 건너뛴다.
            if (!pool.TryGet(target.position + offset, out var enemy))
                return null;

            enemy.Init(target, pool.Release);
            return enemy;
        }
    }
}
