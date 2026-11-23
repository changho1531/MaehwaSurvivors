using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 보스 그림(자식)만 움직이는 경고 동작 (8-C 연출: 떨림 · 웅크림 · 붉은 기운 · 회전). 몸통(물리 · 판정)은 건드리지 않는다.
    /// 그림이 1장뿐이라 동작은 코드로 흔들기 · 크기 · 색 · 회전으로 표현한다 ※. 걷기 튀기(EnemyArtMotion)는 세로 위치만 쓰므로 겹치지 않는다.
    /// </summary>
    public sealed class BossArt
    {
        static readonly Color Red = new(1f, 0.25f, 0.2f, 1f);

        readonly SpriteRenderer renderer;
        readonly Transform art;
        readonly Vector3 baseScale;
        readonly Color baseColor;
        readonly ArtShake shake;

        public BossArt(SpriteRenderer renderer)
        {
            this.renderer = renderer;
            art = renderer != null ? renderer.transform : null;
            baseScale = art != null ? art.localScale : Vector3.one;
            baseColor = renderer != null ? renderer.color : Color.white;
            shake = new ArtShake(art);
        }

        public Color BaseColor => baseColor;

        public void Shake(float amplitude, float frequency, float dt) => shake.Tick(amplitude, frequency, dt); // 궁수 · 저주술사와 같은 떨림

        /// <summary>amount 0 → 1: 낮아지고 넓어진다</summary>
        public void Crouch(float amount)
        {
            if (art != null)
                art.localScale = new Vector3(baseScale.x * (1f + 0.12f * amount), baseScale.y * (1f - 0.18f * amount), baseScale.z);
        }

        /// <summary>amount 0 → 1: 붉은 기운</summary>
        public void Glow(float amount)
        {
            if (renderer != null)
                renderer.color = Color.Lerp(baseColor, Red, Mathf.Clamp01(amount));
        }

        public void Spin(float degrees)
        {
            if (art != null)
                art.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        public void SetAlpha(float alpha)
        {
            if (renderer == null)
                return;
            var c = renderer.color;
            c.a = alpha;
            renderer.color = c;
        }

        public void ResetPose()
        {
            shake.Stop();
            if (art == null)
                return;
            art.localScale = baseScale;
            art.localRotation = Quaternion.identity;
            renderer.color = baseColor;
        }
    }
}
