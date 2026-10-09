using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 화면 전환 연출 공용 설정 (설계서 7-C 규칙 1: 일시정지 화면은 결과 화면과 같은 페이드 값).
    /// 한 에셋을 함께 참조해 연출을 하나로 통일한다. 수치는 ※ 예시값.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/UI Settings", fileName = "UiSettings")]
    public class UiSettings : ScriptableObject
    {
        [Tooltip("결과·일시정지 화면 페이드인 시간(실시간 초)")]
        [Min(0.01f)] public float fadeSeconds = 0.5f;
    }
}
