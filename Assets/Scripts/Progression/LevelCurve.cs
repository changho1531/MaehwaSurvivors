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
        [Tooltip("※ 예시값. 첫 레벨업은 시작 후 1분 안팎")]
        public int[] xpToNextLevel = { 50, 65, 80, 95, 110, 125, 140, 155, 170 };

        public int GetRequiredXp(int level)
        {
            if (xpToNextLevel == null || xpToNextLevel.Length == 0)
                return 1;
            int index = Mathf.Clamp(level - 1, 0, xpToNextLevel.Length - 1);
            return Mathf.Max(1, xpToNextLevel[index]);
        }
    }
}
