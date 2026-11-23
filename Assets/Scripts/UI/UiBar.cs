using DG.Tweening;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 가로 게이지. 채움 RectTransform의 오른쪽 앵커를 비율만큼 옮긴다 (스프라이트 없이 동작하는 임시 바).
    /// Ratio(값)는 즉시 바뀌고, 그림만 따라간다:
    /// - smoothSeconds &gt; 0 이면 늘어날 때 부드럽게 차오른다 (경험치). 줄어들 때(레벨업으로 0부터)는 즉시
    /// - trail이 있으면 줄어들 때 채움은 즉시, 잔상이 잠깐 남았다가 따라 줄어든다 (HP: 얼마나 깎였는지 보이게)
    /// </summary>
    public class UiBar : MonoBehaviour
    {
        [SerializeField] RectTransform fill;
        [Tooltip("(선택) 줄어든 만큼 잠깐 남는 잔상. 채움 뒤에 둔다")]
        [SerializeField] RectTransform trail;
        [Tooltip("늘어날 때 차오르는 시간. 0이면 즉시")]
        [SerializeField, Min(0f)] float smoothSeconds;
        [SerializeField, Min(0f)] float trailDelay = 0.25f;
        [SerializeField, Min(0f)] float trailSeconds = 0.35f;

        bool initialized;

        public float Ratio { get; private set; } = 1f;
        /// <summary>지금 그려진 채움 / 잔상 끝 (연출 중에는 Ratio와 다를 수 있다)</summary>
        public float ShownFill => fill.anchorMax.x;
        public float ShownTrail => trail != null ? trail.anchorMax.x : ShownFill;

        public void Set(float current, float max)
        {
            float previous = Ratio;
            Ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            bool first = !initialized;
            initialized = true;

            UiTween.BarTo(fill, Ratio, Ratio > previous && !first ? smoothSeconds : 0f);
            if (trail == null)
                return;
            if (Ratio >= trail.anchorMax.x || first)
                UiTween.BarTo(trail, Ratio, 0f); // 회복 · 시작: 잔상 없이 같이
            else if (!DOTween.IsTweening(trail))
                TrailCatchUp(trailDelay);
            // 따라가는 중이면 새로 시작하지 않는다 — 포위 접촉처럼 매 프레임 깎여도 잔상이 멈춰 있지 않게
        }

        void TrailCatchUp(float delay)
        {
            var tween = UiTween.BarTo(trail, Ratio, trailSeconds, delay);
            // 따라가는 동안 더 깎였으면 이어서 따라간다
            tween?.OnComplete(() =>
            {
                if (trail.anchorMax.x > Ratio + 0.001f)
                    TrailCatchUp(0f);
            });
        }
    }
}
