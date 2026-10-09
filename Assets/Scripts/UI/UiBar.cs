using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 가로 게이지. 채움 RectTransform의 오른쪽 앵커를 비율만큼 옮긴다 (스프라이트 없이 동작하는 임시 바).
    /// </summary>
    public class UiBar : MonoBehaviour
    {
        [SerializeField] RectTransform fill;

        public float Ratio { get; private set; } = 1f;

        public void Set(float current, float max)
        {
            Ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Ratio, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }
    }
}
