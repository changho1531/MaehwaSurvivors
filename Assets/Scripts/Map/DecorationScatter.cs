using System.Collections.Generic;
using Game.Pooling;
using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// 바닥 장식 흩뿌리기 (설계서 9-A절 규칙 3~7). 맵을 일정 크기 구역으로 나누고, 카메라 주변 구역에만 장식을 둔다.
    /// 구역 좌표를 시드로 배치를 정하므로 되돌아오면 같은 장식이 같은 자리에 있다.
    /// 장식은 풀에서 꺼내고, 범위를 벗어난 구역의 장식은 풀로 돌려 재사용한다. 충돌체가 없어 이동을 막지 않는다.
    /// </summary>
    public class DecorationScatter : MonoBehaviour
    {
        public struct Placement
        {
            public Vector2 Position;
            public int SpriteIndex;
            public bool FlipX;
        }

        [Tooltip("따라갈 대상 (카메라)")]
        [SerializeField] Transform follow;
        [Tooltip("납작한 장식만 (높은 장식은 9-A 규칙 5에 따라 제외)")]
        [SerializeField] Sprite[] sprites;
        [Tooltip("구역 한 변 (칸) ※")]
        [SerializeField, Min(1f)] float chunkSize = 6f;
        [Tooltip("카메라 구역에서 몇 구역까지 채우나. 2 = 5×5 구역 = 30칸, 화면 가로 약 18칸을 덮는다")]
        [SerializeField, Min(1)] int radiusChunks = 2;
        [Tooltip("구역당 장식 수 (최소~최대) ※")]
        [SerializeField] Vector2Int perChunk = new(1, 3);
        [Tooltip("바닥(-100) < 장식 < 경험치 구슬(3) < 적·플레이어")]
        [SerializeField] int sortingOrder = -50;
        [SerializeField] int seed = 1;

        readonly Dictionary<Vector2Int, List<SpriteRenderer>> live = new();
        readonly List<Vector2Int> releaseBuffer = new();
        readonly List<Placement> layoutBuffer = new();
        ComponentPool<SpriteRenderer> pool;
        Vector2Int? currentCenter;

        public int ActiveCount => pool?.CountActive ?? 0;
        public int TotalCreated => pool?.TotalCreated ?? 0;

        void Awake()
        {
            // 장식 하나 = SpriteRenderer만 있는 오브젝트. 프리팹 에셋 대신 비활성 견본을 풀 원본으로 쓴다.
            var template = new GameObject("DecorationTemplate").AddComponent<SpriteRenderer>();
            template.sortingOrder = sortingOrder;
            template.transform.SetParent(transform, false);
            template.gameObject.SetActive(false);
            int span = radiusChunks * 2 + 1;
            pool = new ComponentPool<SpriteRenderer>(template, transform, span * span * perChunk.y);
        }

        void LateUpdate()
        {
            if (follow != null)
                Refresh(follow.position);
        }

        /// <summary>center 주변 구역을 채우고 멀어진 구역을 회수한다. 구역이 바뀔 때만 일한다.</summary>
        public void Refresh(Vector2 center)
        {
            var c = ChunkOf(center, chunkSize);
            if (currentCenter == c || sprites == null || sprites.Length == 0)
                return;
            currentCenter = c;

            releaseBuffer.Clear();
            foreach (var chunk in live.Keys)
                if (Mathf.Abs(chunk.x - c.x) > radiusChunks || Mathf.Abs(chunk.y - c.y) > radiusChunks)
                    releaseBuffer.Add(chunk);
            foreach (var chunk in releaseBuffer)
            {
                foreach (var deco in live[chunk])
                    pool.Release(deco);
                live.Remove(chunk);
            }

            for (int x = c.x - radiusChunks; x <= c.x + radiusChunks; x++)
                for (int y = c.y - radiusChunks; y <= c.y + radiusChunks; y++)
                {
                    var chunk = new Vector2Int(x, y);
                    if (!live.ContainsKey(chunk))
                        live[chunk] = Spawn(chunk);
                }
        }

        List<SpriteRenderer> Spawn(Vector2Int chunk)
        {
            Layout(chunk, seed, chunkSize, perChunk, sprites.Length, layoutBuffer);
            var list = new List<SpriteRenderer>(layoutBuffer.Count);
            foreach (var p in layoutBuffer)
            {
                var deco = pool.Get(p.Position);
                deco.sprite = sprites[p.SpriteIndex];
                deco.flipX = p.FlipX;
                list.Add(deco);
            }
            return list;
        }

        public static Vector2Int ChunkOf(Vector2 position, float chunkSize) =>
            new(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.y / chunkSize));

        /// <summary>구역 하나의 장식 배치. 같은 (구역, 시드)면 항상 같은 결과 — 되돌아와도 풍경이 그대로인 이유.</summary>
        public static void Layout(Vector2Int chunk, int seed, float chunkSize, Vector2Int perChunk, int spriteCount, List<Placement> result)
        {
            result.Clear();
            // UnityEngine.Random은 전역 상태라 다른 시스템(스폰 등)의 난수를 흐트러뜨리므로 구역 전용 난수를 쓴다
            var rng = new System.Random(unchecked(chunk.x * 73856093 ^ chunk.y * 19349663 ^ seed * 83492791));
            int count = rng.Next(perChunk.x, perChunk.y + 1);
            var origin = (Vector2)chunk * chunkSize;
            for (int i = 0; i < count; i++)
                result.Add(new Placement
                {
                    Position = origin + new Vector2((float)rng.NextDouble(), (float)rng.NextDouble()) * chunkSize,
                    SpriteIndex = rng.Next(spriteCount),
                    FlipX = rng.Next(2) == 0,
                });
        }
    }
}
