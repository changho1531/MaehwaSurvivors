using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 주기형 피해 감소막 데이터 (설계서 7-G절, 예: 자하호신강). 수치는 모두 ※ 예시값.
    /// 공격하지 않으므로 베이스의 데미지 표는 쓰지 않는다(0). 쿨타임 표는 "막이 꺼진 뒤부터" 세는 시간이다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapons/Shield Weapon", fileName = "ShieldWeaponData")]
    public class ShieldWeaponData : WeaponData
    {
        [Tooltip("[0] = Lv1. 받는 피해 감소율 (0.2 = -20%)")]
        public float[] reductionPerLevel = { 0.2f };
        [Tooltip("[0] = Lv1. 막 지속시간(초)")]
        public float[] durationPerLevel = { 3f };

        [Header("최대 레벨")]
        [Tooltip("막이 꺼질 때 잃은 체력(최대 HP − 현재 HP)의 이 비율만큼 회복")]
        [Range(0f, 1f)] public float maxLevelHealRatio = 0.2f;

        public ShieldEffect effectPrefab;

        public float GetReduction(int level) => Mathf.Clamp01(LevelTable.Get(reductionPerLevel, level));
        public float GetDuration(int level) => Mathf.Max(0.05f, LevelTable.Get(durationPerLevel, level));

        // 잃은 체력 기준: 위험할수록 많이 찬다 (7-G절 설계 의도)
        public float GetHealAmount(int level, float currentHp, float maxHp) =>
            level >= maxLevel ? Mathf.Max(0f, maxHp - currentHp) * maxLevelHealRatio : 0f;

        // 막 지속시간이 0이 될 수 없어 매 프레임 켜지는 일은 없지만, 쿨타임 0은 "꺼지자마자 다시 켜짐"이 되므로 하한을 둔다
        public override float GetCooldown(int level) => Mathf.Max(0.05f, base.GetCooldown(level));

        protected override void AppendStatChanges(int from, int to, List<string> lines)
        {
            // 감소율은 %p로 표시 ("받는 피해 -5%p"). 감소율이 커질수록 받는 피해가 줄어드므로 부호를 뒤집는다
            float from100 = GetReduction(from) * 100f, to100 = GetReduction(to) * 100f;
            StatChange.Append(lines, "받는 피해", -from100, -to100, "%p");
            StatChange.Append(lines, "지속", GetDuration(from), GetDuration(to), "초");
            StatChange.Append(lines, "쿨타임", LevelTable.Get(cooldownPerLevel, from), LevelTable.Get(cooldownPerLevel, to), "초");
        }

        public override Weapon AddRuntimeTo(GameObject owner) => owner.AddComponent<ShieldWeapon>();
    }
}
