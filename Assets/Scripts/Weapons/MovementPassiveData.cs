using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 상시 이동속도 증가 데이터 (설계서 7-H절, 예: 매화보). 수치는 모두 ※ 예시값.
    /// 공격·쿨타임이 없으므로 베이스의 데미지·쿨타임 표는 쓰지 않는다(0). 카드·레벨 관리만 공통 틀을 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapons/Movement Passive", fileName = "MovementPassiveData")]
    public class MovementPassiveData : WeaponData
    {
        [Tooltip("[0] = Lv1. 이동속도 증가율 (0.05 = +5%)")]
        public float[] speedBonusPerLevel = { 0.05f };

        [Header("최대 레벨 무피격 가속")]
        [Tooltip("이 시간(초, 게임 시간) 동안 안 맞으면 추가 속도")]
        [Min(0f)] public float boostDelaySeconds = 3f;
        [Tooltip("추가 이동속도 (합연산, 0.1 = +10%p)")]
        [Min(0f)] public float boostBonus = 0.1f;

        public PetalTrail petalPrefab;

        public float GetSpeedBonus(int level) => Mathf.Max(0f, LevelTable.Get(speedBonusPerLevel, level));

        /// <summary>합연산: 1 + 레벨 증가율 + (가속 중이면 추가 속도). 최대 1.25배 (7-H 공통 설정)</summary>
        public float GetSpeedMultiplier(int level, bool boosted) => 1f + GetSpeedBonus(level) + (boosted ? boostBonus : 0f);

        protected override void AppendStatChanges(int from, int to, List<string> lines)
        {
            StatChange.Append(lines, "이동속도", GetSpeedBonus(from) * 100f, GetSpeedBonus(to) * 100f, "%p");
        }

        public override Weapon AddRuntimeTo(GameObject owner) => owner.AddComponent<MovementPassive>();
    }
}
