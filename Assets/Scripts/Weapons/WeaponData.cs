using System;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>시너지 태그 (설계서 5절). 시너지 배율 계산(6절)에 쓰인다.</summary>
    [Flags]
    public enum SynergyTag
    {
        None = 0,
        Sword = 1 << 0,     // 검
        Ranged = 1 << 1,    // 원거리
        Combo = 1 << 2,     // 연격
        Melee = 1 << 3,     // 근접
        Impact = 1 << 4,    // 타격
        Area = 1 << 5,      // 범위
        Multi = 1 << 6,     // 다중
        Defense = 1 << 7,   // 방어
        Sustain = 1 << 8,   // 지속
        Movement = 1 << 9,  // 이동
        Support = 1 << 10,  // 보조
    }

    /// <summary>
    /// 모든 무기 데이터의 공통 베이스 (설계서 7절). 무기 유형별 수치는 하위 클래스(ProjectileWeaponData 등)에 둔다.
    /// 수치는 증가율 공식이 아니라 레벨별 수치표(배열)로 둔다 — 인스펙터에서 칸 하나만 고치면 된다. 모든 값은 ※ 예시값.
    /// 최대 레벨도 에셋 값이므로 중간고사(Lv5)·최종(Lv3) 빌드 전환에 코드 수정이 필요 없다.
    /// </summary>
    public abstract class WeaponData : ScriptableObject
    {
        public string displayName = "무기";
        [TextArea] public string description;
        public Sprite icon;
        public SynergyTag tags;
        [Min(1)] public int maxLevel = 5;

        [Tooltip("[0] = Lv1")]
        public float[] damagePerLevel = { 10f };
        [Tooltip("[0] = Lv1. 공격이 끝난 뒤부터 세는 쿨타임(초)")]
        public float[] cooldownPerLevel = { 1f };

        public float GetDamage(int level) => LevelTable.Get(damagePerLevel, level);
        public float GetCooldown(int level) => Mathf.Max(0.05f, LevelTable.Get(cooldownPerLevel, level));

        /// <summary>
        /// 이 데이터에 맞는 무기 컴포넌트를 owner에 붙인다. 무기 유형 추가 = 데이터 하위 클래스 + 무기 하위 클래스 한 쌍,
        /// WeaponInventory는 어떤 유형인지 몰라도 된다 (OCP).
        /// </summary>
        public abstract Weapon AddRuntimeTo(GameObject owner);
    }

    /// <summary>레벨별 수치표 조회. 표가 짧으면 마지막 값을 쓴다 (빈 칸 때문에 0이 되는 사고 방지).</summary>
    public static class LevelTable
    {
        public static float Get(float[] table, int level)
        {
            if (table == null || table.Length == 0)
                return 0f;
            return table[Mathf.Clamp(level - 1, 0, table.Length - 1)];
        }
    }
}
