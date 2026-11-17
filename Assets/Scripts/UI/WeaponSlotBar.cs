using System.Collections.Generic;
using Game.Weapons;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// HUD 무기 슬롯 6칸 (설계서 7-J절, 좌측 하단). 표시만 한다 — 무기 로직은 슬롯을 모른다.
    /// WeaponInventory의 WeaponAcquired · WeaponUpgraded를 구독해 얻은 순서대로 왼쪽부터 칸을 채우고 레벨을 갱신하며 (Observer),
    /// 매 프레임 각 무기의 GetSlotState()만 읽어 덮개 · 게이지를 그린다. 무기 종류별 분기가 없어 새 무기가 생겨도 그대로다.
    /// </summary>
    public class WeaponSlotBar : MonoBehaviour
    {
        [SerializeField] WeaponInventory inventory;
        [Header("※ 7-J 배치표 예시값")]
        [SerializeField, Min(1)] int slotCount = 6;
        [SerializeField, Min(8f)] float slotSize = 56f;
        [SerializeField, Min(0f)] float spacing = 6f;
        [SerializeField, Min(0f)] float flashSeconds = 0.3f;
        [Tooltip("비우면 기본 글꼴")]
        [SerializeField] Font font;

        readonly List<WeaponSlotView> views = new();
        readonly Dictionary<Weapon, WeaponSlotView> byWeapon = new();
        int filled;

        public IReadOnlyList<WeaponSlotView> Views => views;

        void Awake()
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < slotCount; i++)
            {
                var go = new GameObject($"Slot{i}", typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; // 왼쪽 아래 기준으로 가로 1줄
                rect.anchoredPosition = new Vector2(i * (slotSize + spacing), 0f);
                var view = go.AddComponent<WeaponSlotView>();
                view.Build(slotSize, font, flashSeconds);
                views.Add(view);
            }
        }

        void OnEnable()
        {
            if (inventory == null)
                return;
            inventory.WeaponAcquired += OnAcquired;
            inventory.WeaponUpgraded += OnUpgraded;
            // 시작 무기는 인벤토리 Awake에서 이미 지급됐을 수 있다 → 지금 가진 것부터 순서대로 채운다
            foreach (var weapon in inventory.Owned)
                OnAcquired(weapon);
        }

        void OnDisable()
        {
            if (inventory == null)
                return;
            inventory.WeaponAcquired -= OnAcquired;
            inventory.WeaponUpgraded -= OnUpgraded;
        }

        void OnAcquired(Weapon weapon)
        {
            if (byWeapon.ContainsKey(weapon) || filled >= views.Count)
                return;
            var view = views[filled++]; // 얻은 순서대로 왼쪽부터 (규칙 1)
            view.Assign(weapon);
            byWeapon.Add(weapon, view);
        }

        void OnUpgraded(Weapon weapon, int level)
        {
            if (!byWeapon.TryGetValue(weapon, out var view))
                return;
            view.RefreshLevel();
            view.Flash(); // 카드를 연속으로 골라도 칸마다 각각 반짝인다
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var pair in byWeapon)
            {
                if (pair.Key == null)
                    continue;
                var state = pair.Key.GetSlotState();
                pair.Value.Refresh(state, dt);
            }
        }
    }
}
