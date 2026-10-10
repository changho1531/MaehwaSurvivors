using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 원거리 적(혈영궁수) 데이터 (설계서 8-A절). 공통 스탯은 EnemyData, 원거리 전용 값만 여기에 — 무기 데이터와 같은 상속 구조.
    /// 모든 수치는 ※ 예시값.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Ranged Enemy Data", fileName = "RangedEnemyData")]
    public class RangedEnemyData : EnemyData
    {
        [Header("원거리 공격 (8-A절)")]
        [Tooltip("이 거리 안이면 멈춰서 쏜다. 화면 안(위아래 5칸)에 항상 들어오도록 3칸")]
        [Min(0.1f)] public float attackRange = 3f;
        [Tooltip("쏘기 직전 떨림(신호) 시간")]
        [Min(0f)] public float windupSeconds = 0.5f;
        [Tooltip("발사 간격 = 떨림 + 대기")]
        [Min(0.1f)] public float fireInterval = 2f;
        [Min(0f)] public float arrowDamage = 8f;
        [Tooltip("화살 속도 = 플레이어 최고 이동 속도 × 이 배수. 보고 피할 수 있게")]
        [Min(0.1f)] public float arrowSpeedMultiplier = 1.3f;
        public Arrow arrowPrefab;

        [Header("떨림 연출")]
        [Min(0f)] public float shakeAmplitude = 0.06f;
        [Min(0f)] public float shakeFrequency = 30f;

        /// <summary>화살 비행 거리 = 공격 사거리 (빗나가면 사거리 끝에서 사라짐).</summary>
        public float ArrowTravelDistance => attackRange;
    }
}
