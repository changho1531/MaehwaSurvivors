using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 모든 무기 데이터의 공통 베이스 (설계서 7절). 무기 유형별 수치는 하위 클래스(ProjectileWeaponData 등)에 둔다.
    /// 수치는 모두 ※ 예시값.
    /// </summary>
    public abstract class WeaponData : ScriptableObject
    {
        public string displayName = "무기";
        [Min(0f)] public float damage = 10f;
        [Min(0.05f)] public float cooldown = 1f;
    }
}
