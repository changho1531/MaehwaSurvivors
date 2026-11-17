using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 코드로 만든 UI용 기본 스프라이트. Image의 Filled(원형 채우기)는 스프라이트가 있어야 동작하므로,
    /// 그림 에셋 없이 쿨타임 덮개 · 테두리 게이지를 그리기 위해 쓴다. 한 번 만들어 공유한다.
    /// </summary>
    public static class UiSprites
    {
        const int Size = 64;
        static Sprite solid;
        static Sprite outline;

        /// <summary>흰색 꽉 찬 사각형</summary>
        public static Sprite Solid => solid != null ? solid : solid = Create("UiSolid", (x, y) => true);

        /// <summary>흰색 사각 테두리 (두께 = 크기의 1/16)</summary>
        public static Sprite Outline => outline != null ? outline : outline = Create("UiOutline", (x, y) =>
        {
            const int t = Size / 16;
            return x < t || y < t || x >= Size - t || y >= Size - t;
        });

        static Sprite edgeGlow;

        /// <summary>가장자리만 진하고 가운데는 투명 (화면 가장자리 번쩍임, 8-C 연출)</summary>
        public static Sprite EdgeGlow
        {
            get
            {
                if (edgeGlow != null)
                    return edgeGlow;
                const int n = 128;
                var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "UiEdgeGlow", wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // 가장자리까지의 거리 0(끝) ~ 0.5(가운데) → 가장자리 22% 안쪽에서 사라진다
                    float edge = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y)) / (float)n;
                    float a = Mathf.Clamp01(1f - edge / 0.22f);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                edgeGlow = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
                edgeGlow.name = "UiEdgeGlow";
                return edgeGlow;
            }
        }

        static Sprite Create(string name, System.Func<int, int, bool> filled)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = filled(x, y) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }
    }
}
