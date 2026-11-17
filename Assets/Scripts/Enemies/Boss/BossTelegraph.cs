using Game.Weapons;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 보스 공격 경고 표시 (8-C 연출): 모두 붉은 반투명 + 테두리 — 저주술사(어두운 보라)와 색으로 구분.
    /// 앞쪽 반원(휘두르기) · 직선(돌진) · 원(지면 강타 · 혈풍선) 세 모양을 코드로 만든 그림으로 그린다.
    /// 보스 자식이지만 보스 크기(2배)의 영향을 받지 않게 월드 크기로 맞춘다.
    /// </summary>
    public class BossTelegraph : MonoBehaviour
    {
        static readonly Color FillColor = new(0.9f, 0.08f, 0.08f, 0.28f);
        static readonly Color StrongFillColor = new(1f, 0.2f, 0.15f, 0.55f);
        static readonly Color EdgeColor = new(1f, 0.2f, 0.18f, 0.9f);

        Transform fan;
        SpriteRenderer fanFill;
        Transform line;
        SpriteRenderer lineFill;
        Transform disk;
        SpriteRenderer diskFill;

        public bool IsShowing => fan.gameObject.activeSelf || line.gameObject.activeSelf || disk.gameObject.activeSelf;
        public bool IsFanShown => fan.gameObject.activeSelf;
        public bool IsLineShown => line.gameObject.activeSelf;
        public bool IsDiskShown => disk.gameObject.activeSelf;
        /// <summary>현재 표시 중인 반원 · 직선의 방향</summary>
        public Vector2 Direction { get; private set; }

        public static BossTelegraph Create(Transform owner)
        {
            var go = new GameObject("Telegraph");
            go.transform.SetParent(owner, false);
            var s = owner.lossyScale;
            go.transform.localScale = new Vector3(1f / Mathf.Max(0.01f, s.x), 1f / Mathf.Max(0.01f, s.y), 1f);
            var telegraph = go.AddComponent<BossTelegraph>();
            telegraph.Build();
            return telegraph;
        }

        void Build()
        {
            fan = CreateShape("Fan", TelegraphSprites.HalfDisk, TelegraphSprites.HalfDiskEdge, out fanFill);
            line = CreateShape("Line", TelegraphSprites.Rect, TelegraphSprites.RectEdge, out lineFill);
            disk = CreateShape("Disk", ProceduralCircle.Disk, ProceduralCircle.Ring, out diskFill);
            Hide();
        }

        Transform CreateShape(string name, Sprite fill, Sprite edge, out SpriteRenderer fillRenderer)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            fillRenderer = CreateRenderer(root, "Fill", fill, FillColor, 2);
            CreateRenderer(root, "Edge", edge, EdgeColor, 3);
            return root;
        }

        static SpriteRenderer CreateRenderer(Transform parent, string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        /// <summary>앞쪽 반원 (휘두르기). strong = 실제로 휘두르는 순간</summary>
        public void ShowFan(Vector2 direction, float radius, bool strong = false)
        {
            Hide();
            Direction = direction;
            fan.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            fan.localScale = Vector3.one * radius; // 그림 반경 1
            fanFill.color = strong ? StrongFillColor : FillColor;
            fan.gameObject.SetActive(true);
        }

        /// <summary>진행 방향 직선 (돌진)</summary>
        public void ShowLine(Vector2 direction, float length, float width)
        {
            Hide();
            Direction = direction;
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            line.localScale = new Vector3(length, width, 1f); // 그림 1×1, 왼쪽 가운데가 기준점
            line.gameObject.SetActive(true);
        }

        /// <summary>발밑 원 (지면 강타) · 주변 원 (혈풍선)</summary>
        public void ShowDisk(float radius)
        {
            Hide();
            disk.localScale = Vector3.one * (radius * 2f); // 원 그림 지름 1
            disk.gameObject.SetActive(true);
        }

        public void Hide()
        {
            fan.gameObject.SetActive(false);
            line.gameObject.SetActive(false);
            disk.gameObject.SetActive(false);
        }
    }

    /// <summary>경고 표시용 코드 그림 (흰색, 색은 렌더러에서).</summary>
    public static class TelegraphSprites
    {
        const int Size = 128;
        const float Edge = 0.06f;
        static Sprite halfDisk, halfDiskEdge, rect, rectEdge;

        /// <summary>오른쪽(+x)을 향한 반원. 기준점 = 원의 중심, 반경 1</summary>
        public static Sprite HalfDisk => halfDisk ??= Create("TelegraphHalfDisk", new Vector2(0.5f, 0.5f), Size / 2f, (u, v) => u >= 0f && u * u + v * v <= 1f);

        public static Sprite HalfDiskEdge => halfDiskEdge ??= Create("TelegraphHalfDiskEdge", new Vector2(0.5f, 0.5f), Size / 2f, (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            return u >= -Edge && d <= 1f && (d >= 1f - Edge || Mathf.Abs(u) <= Edge);
        });

        /// <summary>1×1, 기준점 = 왼쪽 가운데 (길이 방향으로 늘린다)</summary>
        public static Sprite Rect => rect ??= Create("TelegraphRect", new Vector2(0f, 0.5f), Size, (u, v) => true);

        public static Sprite RectEdge => rectEdge ??= Create("TelegraphRectEdge", new Vector2(0f, 0.5f), Size, (u, v) =>
            Mathf.Abs(u) >= 1f - Edge * 0.5f || Mathf.Abs(v) >= 1f - Edge * 2f);

        /// <summary>inside(u, v): u, v는 텍스처를 −1 ~ 1로 본 좌표</summary>
        static Sprite Create(string name, Vector2 pivot, float pixelsPerUnit, System.Func<float, float, bool> inside)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size * 2f - 1f;
                float v = (y + 0.5f) / Size * 2f - 1f;
                pixels[y * Size + x] = inside(u, v) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), pivot, pixelsPerUnit);
            sprite.name = name;
            return sprite;
        }
    }
}
