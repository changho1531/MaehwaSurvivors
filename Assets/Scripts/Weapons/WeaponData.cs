using System;
using System.Collections.Generic;
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
        [Tooltip("신규 획득 카드에 쓰는 한 줄 설명")]
        [TextArea] public string description;
        [Tooltip("최대 레벨이 되는 강화 카드에 덧붙는 한 줄 (예: \"최대 레벨: 관통 무제한\"). 비우면 생략")]
        [TextArea] public string maxLevelEffect;
        public Sprite icon;
        public SynergyTag tags;
        [Min(1)] public int maxLevel = 5;

        [Tooltip("[0] = Lv1")]
        public float[] damagePerLevel = { 10f };
        [Tooltip("[0] = Lv1. 공격이 끝난 뒤부터 세는 쿨타임(초)")]
        public float[] cooldownPerLevel = { 1f };

        public float GetDamage(int level) => LevelTable.Get(damagePerLevel, level);
        // 쿨타임 0 = 공격이 끝나자마자 다음 공격 (화산십이검 최대 레벨 연속 발동, 7절 규칙 6).
        // 즉발형 무기는 0이면 매 프레임 공격하게 되므로 하위 클래스에서 하한을 둔다.
        public virtual float GetCooldown(int level) => Mathf.Max(0f, LevelTable.Get(cooldownPerLevel, level));

        /// <summary>
        /// 강화 카드 문구 (7-D절): from → from+1 레벨에서 값이 바뀌는 항목만, 최대 레벨이 되면 특수 효과 한 줄을 덧붙인다.
        /// 문구를 손으로 쓰지 않고 수치표에서 계산하므로, 표를 고치면 카드도 따라 바뀐다.
        /// </summary>
        public void DescribeUpgrade(int from, List<string> lines)
        {
            lines.Clear();
            AppendStatChanges(from, from + 1, lines);
            if (from + 1 >= maxLevel && !string.IsNullOrWhiteSpace(maxLevelEffect))
                lines.Add(maxLevelEffect);
        }

        /// <summary>유형별 수치가 있는 하위 클래스는 override해서 항목을 덧붙인다.</summary>
        protected virtual void AppendStatChanges(int from, int to, List<string> lines)
        {
            StatChange.Append(lines, "공격력", LevelTable.Get(damagePerLevel, from), LevelTable.Get(damagePerLevel, to));
            StatChange.Append(lines, "쿨타임", LevelTable.Get(cooldownPerLevel, from), LevelTable.Get(cooldownPerLevel, to), "초");
        }

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

    /// <summary>수치 변화 한 줄 ("공격력 +2", "쿨타임 -0.05초"). 값이 같으면 줄을 만들지 않는다.</summary>
    public static class StatChange
    {
        public static void Append(List<string> lines, string label, float from, float to, string unit = "")
        {
            float delta = to - from;
            if (Mathf.Abs(delta) < 0.0001f)
                return;
            lines.Add($"{label} {(delta > 0f ? "+" : "-")}{Mathf.Abs(delta):0.##}{unit}");
        }
    }
}
