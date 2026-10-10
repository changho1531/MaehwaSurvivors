using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.Progression;
using Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 시연용 디버그 키 (설계서 7-D절 5). 발표 중 레벨업·최대 레벨 같은 핵심 장면을 기다리지 않고 바로 보여 주기 위함.
    /// 에디터·Development Build에서만 동작하고(Debug.isDebugBuild), 릴리스 빌드에서는 스스로 꺼진다.
    /// 켜져 있으면 화면 구석에 "DEBUG"와 켜진 토글을 표시해, 시연 중 실수로 켜 둔 상태를 알 수 있게 한다.
    /// 각 기능은 기존 시스템의 공개 진입점만 호출한다 — 디버그 전용 우회 경로를 만들지 않는다.
    /// </summary>
    public class DebugKeys : MonoBehaviour
    {
        [SerializeField] PlayerLevel playerLevel;
        [SerializeField] PlayerHealth playerHealth;
        [SerializeField] WeaponInventory inventory;
        [SerializeField] EnemySpawner spawner;
        [Tooltip("F3으로 즉시 획득할 무기 (화산십이검)")]
        [SerializeField] WeaponData quickAcquireWeapon;
        [SerializeField] Text label;

        void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                if (label != null)
                    label.gameObject.SetActive(false);
                enabled = false;
                return;
            }
            RefreshLabel();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            // 카드 선택·일시정지·결과 화면 중에는 상태가 꼬이지 않게 플레이 중에만 받는다.
            if (keyboard == null || !GameManager.IsIn(GameState.Play))
                return;

            if (keyboard.f1Key.wasPressedThisFrame) LevelUpNow();
            if (keyboard.f2Key.wasPressedThisFrame) ToggleInvincible();
            if (keyboard.f3Key.wasPressedThisFrame) AcquireQuickWeapon();
            if (keyboard.f4Key.wasPressedThisFrame) MaxOwnedWeapons();
            if (keyboard.f5Key.wasPressedThisFrame) ToggleFastestSpawn();
        }

        /// <summary>F1: 레벨업 1회분 경험치 → 평소와 같은 레벨업 흐름(카드 표시)을 탄다.</summary>
        public void LevelUpNow() => playerLevel.AddXp(playerLevel.RequiredXp);

        /// <summary>F2: 무적 켜기/끄기.</summary>
        public void ToggleInvincible()
        {
            playerHealth.Invincible = !playerHealth.Invincible;
            RefreshLabel();
        }

        /// <summary>F3: 지정 무기 즉시 획득 (이미 있으면 무시).</summary>
        public void AcquireQuickWeapon() => inventory.Acquire(quickAcquireWeapon);

        /// <summary>F4: 보유 무기 전부 최대 레벨 (특수 효과 시연용). 강화 이벤트·성장 완료 판정도 평소처럼 일어난다.</summary>
        public void MaxOwnedWeapons()
        {
            foreach (var weapon in inventory.Owned)
                while (inventory.Upgrade(weapon.Data)) { }
        }

        /// <summary>F5: 스폰 속도 최대(곡선의 하한) 켜기/끄기.</summary>
        public void ToggleFastestSpawn()
        {
            spawner.ForceFastest = !spawner.ForceFastest;
            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (label == null)
                return;
            label.text = "DEBUG"
                + (playerHealth != null && playerHealth.Invincible ? "  무적" : "")
                + (spawner != null && spawner.ForceFastest ? "  스폰 최대" : "");
        }
    }
}
