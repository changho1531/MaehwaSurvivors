using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 레벨별 필요 경험치 표 (설계서 6-A절). 공식 대신 표로 두어 인스펙터에서 칸 하나만 고치면 되게 한다.
    /// xpToNextLevel[0] = Lv1 → Lv2 에 필요한 XP. 표를 넘어선 레벨은 마지막 값을 계속 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Level Curve", fileName = "LevelCurve")]
    public class LevelCurve : ScriptableObject
    {
        [Tooltip("※ 예시값. 10-10 플레이 테스트에서 레벨업이 느려 기존 표의 절반(홀수는 올림)으로 줄임")]
        public int[] xpToNextLevel = { 25, 33, 40, 48, 55, 63, 70, 78, 85 };

        public int GetRequiredXp(int level)
        {
            if (xpToNextLevel == null || xpToNextLevel.Length == 0)
                return 1;
            int index = Mathf.Clamp(level - 1, 0, xpToNextLevel.Length - 1);
            return Mathf.Max(1, xpToNextLevel[index]);
        }
    }
}
