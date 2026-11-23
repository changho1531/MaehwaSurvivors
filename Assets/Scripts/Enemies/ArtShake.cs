using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 그림(자식)만 가로로 떠는 경고 동작 — 궁수(활 당김) · 저주술사(장판 시전) · 보스(돌진 직전)가 같이 쓴다.
    /// 세로 위치는 EnemyArtMotion(걷기 통통)의 몫이라 가로(x)만 바꾼다. 몸통(물리 · 판정)은 건드리지 않는다.
    /// 세기 · 빠르기는 각 적의 데이터(SO)에서 받는다.
    /// </summary>
    public sealed class ArtShake
    {
        readonly Transform art;
        float time;

        public ArtShake(Transform art) => this.art = art;

        /// <summary>시간 0부터: 시작 순간은 제자리 (sin 0)</summary>
        public void Restart() => time = 0f;

        public void Tick(float amplitude, float frequency, float dt)
        {
            time += dt;
            SetX(Offset(time, amplitude, frequency));
        }

        /// <summary>떨림을 멈추고 제자리로</summary>
        public void Stop()
        {
            time = 0f;
            SetX(0f);
        }

        public static float Offset(float time, float amplitude, float frequency) =>
            Mathf.Sin(time * frequency * Mathf.PI * 2f) * amplitude;

        void SetX(float x)
        {
            if (art == null)
                return;
            var p = art.localPosition;
            art.localPosition = new Vector3(x, p.y, p.z);
        }
    }
}
