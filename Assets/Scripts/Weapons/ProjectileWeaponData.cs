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
        [Tooltip("이 거리를 날아가면 맞지 않아도 사라진다")]
        [Min(0.1f)] public float maxTravelDistance = 12f;
        public Projectile projectilePrefab;
        [Min(0)] public int prewarmCount = 10;

        public float GetProjectileSpeed(int level) => Mathf.Max(0.1f, LevelTable.Get(projectileSpeedPerLevel, level));

        public override Weapon AddRuntimeTo(GameObject owner) => owner.AddComponent<ProjectileWeapon>();
    }
}
