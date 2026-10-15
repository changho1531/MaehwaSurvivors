using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// 끝없는 바닥 (설계서 9-A절 규칙 1·2). 이음새 없는 바닥 그림을 SpriteRenderer의 Tiled 모드로 넓게 깔고,
    /// 카메라를 따라 "바닥 1장 크기 단위로" 순간 이동시킨다. 1장 단위로만 움직이므로 무늬가 미끄러지지 않고
    /// 플레이어 눈에는 바닥이 제자리에 끝없이 이어진 것처럼 보인다. 오브젝트 1개라 생성·파괴가 없다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class InfiniteFloor : MonoBehaviour
    {
        [Tooltip("따라갈 대상 (카메라)")]
        [SerializeField] Transform follow;
        [Tooltip("가로·세로로 깔 바닥 장수. 화면 크기 + 1장(순간 이동 여유)을 덮어야 한다. 바닥 6칸 × 5장 = 30칸 > 화면 가로 약 18칸")]
        [SerializeField, Min(1)] int tilesAcross = 5;

        SpriteRenderer floor;
        Vector2 tileSize;

        void Awake()
        {
            floor = GetComponent<SpriteRenderer>();
            // Tiled 모드는 그림의 Mesh Type이 Full Rect여야 반복된다 (임포트 설정)
            floor.drawMode = SpriteDrawMode.Tiled;
            tileSize = floor.sprite.bounds.size;
            floor.size = tileSize * tilesAcross;
        }

        void LateUpdate()
        {
            if (follow == null)
                return;
            var p = Snap(follow.position, tileSize);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }

        /// <summary>가장 가까운 바닥 1장 경계로 맞춘다. 이 격자 위에서만 움직이면 무늬가 화면에서 움직이지 않는다.</summary>
        public static Vector2 Snap(Vector2 position, Vector2 tileSize) =>
            new(Mathf.Round(position.x / tileSize.x) * tileSize.x, Mathf.Round(position.y / tileSize.y) * tileSize.y);
    }
}
