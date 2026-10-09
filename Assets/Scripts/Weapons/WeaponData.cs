using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 무기 데이터 (설계서 7절). 수치는 모두 ※ 예시값.
    /// 신규 투사체형 무기는 이 에셋을 하나 더 만드는 것으로 추가한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string displayName = "임시 탄환";
        [Min(0f)] public float damage = 10f;
        [Min(0.05f)] public float cooldown = 1f;
        [Tooltip("이 거리 안에 적이 있어야 발사한다")]
        [Min(0f)] public float range = 9f;
        [Min(0f)] public float projectileSpeed = 12f;
        [Min(0.05f)] public float projectileLifetime = 2f;
        public Projectile projectilePrefab;
        [Min(0)] public int prewarmCount = 10;
    }
}
