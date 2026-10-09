using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 보유 무기와 레벨 (설계서 7절). 획득·강화, 레벨업 카드 후보 조회, 성장 완료(모든 무기 최대 레벨) 판정을 맡는다.
    /// 무기 컴포넌트 생성은 각 WeaponData에 맡기므로 여기에는 무기 유형별 분기가 없다.
    /// </summary>
    public class WeaponInventory : MonoBehaviour
    {
        [SerializeField] WeaponCatalog catalog;
        [Tooltip("게임 시작 시 Lv1로 지급 (매화검기)")]
        [SerializeField] WeaponData startingWeapon;

        readonly List<Weapon> owned = new();

        public event Action<Weapon> WeaponAcquired;

        /// <summary>(무기, 새 레벨)</summary>
        public event Action<Weapon, int> WeaponUpgraded;

        /// <summary>카탈로그의 모든 무기가 최대 레벨이 된 순간 한 번 (6-A절 규칙 8 성장 완료).</summary>
        public event Action AllWeaponsMaxed;

        public WeaponCatalog Catalog => catalog;
        public IReadOnlyList<Weapon> Owned => owned;
        public bool AllMaxed { get; private set; }

        void Awake()
        {
            if (startingWeapon != null)
                Acquire(startingWeapon);
        }

        public Weapon Find(WeaponData data)
        {
            foreach (var weapon in owned)
                if (weapon.Data == data)
                    return weapon;
            return null;
        }

        public int GetLevel(WeaponData data) => Find(data) is { } weapon ? weapon.Level : 0;

        public bool IsMaxed(WeaponData data) => Find(data) is { } weapon && weapon.Level >= data.maxLevel;

        public bool Acquire(WeaponData data)
        {
            if (data == null || Find(data) != null)
                return false;

            var weapon = data.AddRuntimeTo(gameObject);
            weapon.Init(data, 1);
            owned.Add(weapon);
            WeaponAcquired?.Invoke(weapon);
            CheckAllMaxed();
            return true;
        }

        public bool Upgrade(WeaponData data)
        {
            var weapon = Find(data);
            if (weapon == null || weapon.Level >= data.maxLevel)
                return false;

            weapon.SetLevel(weapon.Level + 1);
            WeaponUpgraded?.Invoke(weapon, weapon.Level);
            CheckAllMaxed();
            return true;
        }

        /// <summary>카드 선택 반영: 미보유면 신규 획득, 보유 중이면 강화.</summary>
        public bool Apply(WeaponData data) => Find(data) == null ? Acquire(data) : Upgrade(data);

        /// <summary>카드 후보 = 미보유 무기 + 보유 중이고 최대 레벨 미만인 무기 (설계서 6절).</summary>
        public void GetCandidates(List<WeaponData> result)
        {
            result.Clear();
            if (catalog == null)
                return;
            foreach (var data in catalog.weapons)
                if (data != null && !IsMaxed(data))
                    result.Add(data);
        }

        void CheckAllMaxed()
        {
            if (AllMaxed || catalog == null || catalog.weapons.Count == 0)
                return;
            foreach (var data in catalog.weapons)
                if (data != null && !IsMaxed(data))
                    return;

            AllMaxed = true;
            AllWeaponsMaxed?.Invoke();
        }
    }
}
