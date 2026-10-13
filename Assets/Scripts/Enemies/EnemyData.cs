using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Enemies
{
    /// <summary>적 스탯 데이터. 모든 수치는 ※ 예시값이며 에셋에서 조정한다.</summary>
    [CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Min(1f)] public float maxHp = 10f;
        [Min(0f)] public float moveSpeed = 2f;
        [Tooltip("플레이어와 닿아 있는 동안 초당 데미지 (7-B절). 여러 마리면 마리 수만큼 합산")]
        [FormerlySerializedAs("contactDamage"), Min(0f)] public float contactDps = 10f;

        [Header("분리 힘 (7-B 규칙 1-A, 적끼리는 충돌하지 않음)")]
        [Tooltip("이 거리 안의 다른 적에게서 밀려난다. 가까울수록 세게")]
        [Min(0f)] public float separationRadius = 0.6f;
        [Tooltip("완전히 겹쳤을 때 밀려나는 속도. 너무 강하면 서로 밀려 플레이어에 못 닿음")]
        [Min(0f)] public float separationStrength = 1f;

        [Header("경험치 구슬 (설계서 6-A절)")]
        [Tooltip("많은 순: 보스 > 저주술사 > 궁수 > 근거리 적")]
        [Min(0)] public int xpAmount = 1;
        [Tooltip("구슬 모양은 공통, 색만 적 종류별")]
        public Color xpOrbColor = new(0.788f, 0.635f, 0.294f); // GOLD #C9A24B
    }
}
