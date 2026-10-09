using Game.Weapons;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 성장 완료 연결 (설계서 6-A절 규칙 8): 모든 무기가 최대 레벨이 되는 순간
    /// 구슬 드롭 중지·맵 구슬 제거(XpDropper) + 경험치 바 MAX 고정·대기 폐기(PlayerLevel).
    /// 무기 시스템과 XP 시스템이 서로를 모르도록 둘 사이의 연결만 이 컴포넌트가 맡는다.
    /// </summary>
    public class GrowthCompletion : MonoBehaviour
    {
        [SerializeField] WeaponInventory inventory;
        [SerializeField] XpDropper dropper;
        [SerializeField] PlayerLevel playerLevel;

        void OnEnable()
        {
            inventory.AllWeaponsMaxed += OnAllWeaponsMaxed;
            if (inventory.AllMaxed)
                OnAllWeaponsMaxed();
        }

        void OnDisable()
        {
            inventory.AllWeaponsMaxed -= OnAllWeaponsMaxed;
        }

        void OnAllWeaponsMaxed()
        {
            dropper.StopDropsAndClear();
            playerLevel.MarkMaxed();
        }
    }
}
