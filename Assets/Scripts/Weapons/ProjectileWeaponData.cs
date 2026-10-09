using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 투사체형 무기 데이터 (설계서 7절, 예: 매화검기). 수치는 모두 ※ 예시값.
    /// 신규 투사체형 무기는 이 에셋을 하나 더 만드는 것으로 추가한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapons/Projectile Weapon", fileName = "ProjectileWeaponData")]
    public class ProjectileWeaponData : WeaponData
    {
        [Tooltip("[0] = Lv1")]
        public float[] projectileSpeedPerLevel = { 12f };
        [Tooltip("[0] = Lv1. 관통 수: 0 = 첫 적에서 소멸, -1 = 비행 거리 끝까지 무제한 (같은 적은 1회만)")]
        public int[] piercePerLevel = { 0 };
        [Tooltip("이 거리를 날아가면 맞지 않아도 사라진다")]
        [Min(0.1f)] public float maxTravelDistance = 12f;
        public Projectile projectilePrefab;
        [Min(0)] public int prewarmCount = 10;

        public float GetProjectileSpeed(int level) => Mathf.Max(0.1f, LevelTable.Get(projectileSpeedPerLevel, level));
        public int GetPierce(int level) => piercePerLevel == null || piercePerLevel.Length == 0 ? 0 : piercePerLevel[Mathf.Clamp(level - 1, 0, piercePerLevel.Length - 1)];

        // 즉발형이라 쿨타임 0이면 매 프레임 발사되는 사고가 난다
        public override float GetCooldown(int level) => Mathf.Max(0.05f, base.GetCooldown(level));

        protected override void AppendStatChanges(int from, int to, List<string> lines)
        {
            base.AppendStatChanges(from, to, lines);
            StatChange.Append(lines, "탄속", LevelTable.Get(projectileSpeedPerLevel, from), LevelTable.Get(projectileSpeedPerLevel, to));
        }

        public override Weapon AddRuntimeTo(GameObject owner) => owner.AddComponent<ProjectileWeapon>();
    }
}
