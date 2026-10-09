using UnityEngine;

namespace Game.Player
{
    /// <summary>플레이어 스탯 데이터. 모든 수치는 ※ 예시값이며 에셋에서 조정한다.</summary>
    [CreateAssetMenu(menuName = "Game/Player Data", fileName = "PlayerData")]
    public class PlayerData : ScriptableObject
    {
        [Min(1f)] public float maxHp = 100f;
        [Tooltip("피격 후 무적 시간(초). 이 동안 추가 접촉 데미지를 받지 않는다")]
        [Min(0f)] public float invulnerabilitySeconds = 0.5f;
        [Tooltip("피격 시 깜빡임 색")]
        public Color hitFlashColor = new(1f, 0.45f, 0.45f, 1f);
    }
}
