using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 내려찍기형 무기 데이터 (설계서 7-E절, 예: 낙매진각). 수치는 모두 ※ 예시값.
    /// 넉백 거리 수치는 따로 없다: 범위 안의 적을 원의 테두리까지 밀므로 범위가 곧 넉백 거리다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapons/Slam Weapon", fileName = "SlamWeaponData")]
    public class SlamWeaponData : WeaponData
    {
        [Tooltip("[0] = Lv1. 범위 반경(칸)")]
        public float[] radiusPerLevel = { 2f };

        [Header("넉백")]
        [Tooltip("플레이어에게 닿아 있던 적이 테두리까지 밀리는 시간(초)")]
        [Min(0.01f)] public float baseKnockbackSeconds = 0.2f;
        [Tooltip("접촉 거리 = 플레이어 판정 반경 + 적 판정 반경")]
        [Min(0f)] public float contactDistance = 0.675f;

        [Header("최대 레벨 장판")]
        [Min(0f)] public float zoneSeconds = 2f;
        [Min(0f)] public float zoneDamagePerSecond = 2f;
        public DamageZone zonePrefab;
        [Min(0)] public int zonePrewarmCount = 2;

        public BurstEffect effectPrefab;

        public float GetRadius(int level) => LevelTable.Get(radiusPerLevel, level);

        // 범위가 커지면 속도도 함께 커져, 닿아 있던 적은 어느 레벨에서든 기준 시간만큼 밀린다 (7-E 규칙 5).
        public float GetKnockbackSpeed(int level) => Mathf.Max(0.01f, GetRadius(level) - contactDistance) / baseKnockbackSeconds;

        // 즉발형이라 쿨타임 0이면 매 프레임 발동되는 사고가 난다
        public override float GetCooldown(int level) => Mathf.Max(0.05f, base.GetCooldown(level));

        protected override void AppendStatChanges(int from, int to, List<string> lines)
        {
            StatChange.Append(lines, "범위", LevelTable.Get(radiusPerLevel, from), LevelTable.Get(radiusPerLevel, to), "칸");
            base.AppendStatChanges(from, to, lines);
        }

        public override Weapon AddRuntimeTo(GameObject owner) => owner.AddComponent<SlamWeapon>();
    }
}
