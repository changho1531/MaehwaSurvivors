using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>이 빌드에서 등장 가능한 무기 목록 (설계서 7절). 무기 추가 = 목록에 에셋 추가.</summary>
    [CreateAssetMenu(menuName = "Game/Weapons/Weapon Catalog", fileName = "WeaponCatalog")]
    public class WeaponCatalog : ScriptableObject
    {
        public List<WeaponData> weapons = new();
    }
}
