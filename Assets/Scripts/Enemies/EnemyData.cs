using UnityEngine;

namespace Game.Enemies
{
    /// <summary>적 스탯 데이터. 모든 수치는 ※ 예시값이며 에셋에서 조정한다.</summary>
    [CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Min(1f)] public float maxHp = 10f;
        [Min(0f)] public float moveSpeed = 2f;
    }
}
