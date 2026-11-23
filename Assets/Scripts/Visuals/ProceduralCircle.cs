using UnityEngine;

namespace Game.Visuals
{
    /// <summary>그림 없이 쓰는 원/링 스프라이트. 지름 1(월드 단위)이 되도록 만들어 스케일 = 지름으로 쓴다.</summary>
    public static class ProceduralCircle
    {
        const int Size = 128;
        static Sprite disk;
        static Sprite ring;

        public static Sprite Disk => disk != null ? disk : disk = Create("ProceduralDisk", d => Mathf.Clamp01((1f - d) / 0.12f) * Mathf.Lerp(0.55f, 1f, d));
        public static Sprite Ring => ring != null ? ring : ring = Create("ProceduralRing", d => Mathf.Clamp01(1f - Mathf.Abs(d - 0.92f) / 0.08f));

        /// <summary>alpha(d): 중심 0 ~ 가장자리 1까지의 거리 d에 대한 투명도</summary>
        static Sprite Create(string name, System.Func<float, float> alpha)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            float half = (Size - 1) * 0.5f;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a = d > 1f ? 0f : alpha(d);
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.name = name;
            return sprite;
        }
    }
}
