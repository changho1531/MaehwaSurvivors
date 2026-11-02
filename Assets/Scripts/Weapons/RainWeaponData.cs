using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 범위 지속 데미지형 무기 데이터 (설계서 7-F절, 예: 만천화우). 수치는 모두 ※ 예시값.
    /// 베이스의 데미지 표는 초당 데미지로 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapons/Rain Weapon", fileName = "RainWeaponData")]
    public class RainWeaponData : WeaponData
    {
        [Tooltip("[0] = Lv1. 범위 반경(칸)")]
        public float[] radiusPerLevel = { 2f };
        [Tooltip("[0] = Lv1. 쏟아지는 시간(초)")]
        public float[] durationPerLevel = { 2f };
        [Tooltip("최대 레벨에서 쏟아짐이 끝날 때 원 안 적 전부에 주는 데미지")]
        [Min(0f)] public float explosionDamage = 10f;
        public DamageZone zonePrefab;
        [Min(0)] public int zonePrewarmCount = 2;

        public float GetRadius(int level) => LevelTable.Get(radiusPerLevel, level);
        public float GetDuration(int level) => LevelTable.Get(durationPerLevel, level);

        // 즉발형이라 쿨타임 0이면 매 프레임 발동되는 사고가 난다
        public override float GetCooldown(int level) => Mathf.Max(0.05f, base.GetCooldown(level));

        protected override void AppendStatChanges(int from, int to, List<string> lines)
        {
            StatChange.Append(lines, "범위", LevelTable.Get(radiusPerLevel, from), LevelTable.Get(radiusPerLevel, to), "칸");
            base.AppendStatChanges(from, to, lines);
        }

        public override Weapon AddRuntimeTo(GameObject owner) => owner.AddComponent<RainWeapon>();
    }
}
