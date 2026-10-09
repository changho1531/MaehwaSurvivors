using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 경과 시간(초) → 스폰 간격(초) 곡선 (설계서 7-D절 2). 시간이 지날수록 간격이 짧아진다. 모든 값은 ※ 예시값.
    /// 조정 목표: 2분 시점에 매화검기 Lv1만으로 화면의 약 1/5(약 35마리)이 적으로 차 있음 — PlayMode 테스트로 측정해 맞춘다.
    /// 곡선을 코드가 아니라 에셋으로 두어 밸런스를 인스펙터에서만 바꾼다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Spawn Curve", fileName = "SpawnCurve")]
    public class SpawnCurve : ScriptableObject
    {
        [Tooltip("가로: 경과 시간(초), 세로: 스폰 간격(초). 마지막 키 이후는 마지막 값 유지")]
        public AnimationCurve intervalBySeconds = new(new Keyframe(0f, 1f), new Keyframe(120f, 0.57f), new Keyframe(300f, 0.25f));

        const float AbsoluteMin = 0.02f; // 곡선을 잘못 그려 0 이하가 되면 한 프레임에 무한 스폰되는 사고 방지

        public float GetInterval(float seconds) => Mathf.Max(AbsoluteMin, intervalBySeconds.Evaluate(seconds));

        /// <summary>곡선의 하한(가장 빠른 간격). 디버그 "스폰 속도 최대"가 쓴다.</summary>
        public float Fastest
        {
            get
            {
                float min = float.PositiveInfinity;
                foreach (var key in intervalBySeconds.keys)
                    min = Mathf.Min(min, key.value);
                return Mathf.Max(AbsoluteMin, float.IsPositiveInfinity(min) ? 1f : min);
            }
        }
    }
}
