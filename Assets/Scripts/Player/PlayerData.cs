using UnityEngine;

namespace Game.Player
{
    /// <summary>플레이어 스탯 데이터 (설계서 7-B절). 모든 수치는 ※ 예시값이며 에셋에서 조정한다.</summary>
    [CreateAssetMenu(menuName = "Game/Player Data", fileName = "PlayerData")]
    public class PlayerData : ScriptableObject
    {
        [Min(1f)] public float maxHp = 100f;
    }
}
