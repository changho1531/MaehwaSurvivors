using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 부채꼴 연속 베기형 무기 데이터 (설계서 7절, 예: 화산십이검). 수치는 모두 ※ 예시값.
    /// 레벨이 오르면 데미지·쿨타임만 바뀐다 (베이스의 레벨별 표). 판정 범위는 고정.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapons/Sweep Weapon", fileName = "SweepWeaponData")]
    public class SweepWeaponData : WeaponData
    {
        [Tooltip("콤보 한 번의 타격 횟수")]
        [Min(1)] public int hitCount = 12;
        [Tooltip("콤보 지속 시간(초). 타격은 이 시간 동안 균등 간격")]
        [Min(0.05f)] public float comboDuration = 1.5f;
        [Min(0.1f)] public float radius = 3f;
        [Tooltip("부채꼴 전체 각도(도)")]
        [Range(1f, 360f)] public float angle = 120f;
        public SweepEffect effectPrefab;

        public override Weapon AddRuntimeTo(GameObject owner) => owner.AddComponent<SweepWeapon>();
    }
}
