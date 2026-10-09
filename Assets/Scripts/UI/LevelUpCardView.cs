using System.Collections.Generic;
using Game.Progression;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 레벨업 선택 화면 (MU-B-003). LevelUpFlow의 이벤트만 구독해 카드를 그리고, 클릭을 Choose(번호)로 전달한다.
    /// 카드가 1장뿐이면 남는 슬롯은 숨긴다 (가로 레이아웃이라 남은 1장이 가운데로 온다).
    /// </summary>
    public class LevelUpCardView : MonoBehaviour
    {
        [SerializeField] LevelUpFlow flow;
        [SerializeField] GameObject panel;
        [SerializeField] LevelUpCardSlot[] slots;

        public GameObject Panel => panel;
        public IReadOnlyList<LevelUpCardSlot> Slots => slots;

        void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; // 클로저가 반복 변수를 공유하지 않도록 복사
                slots[i].Button.onClick.AddListener(() => flow.Choose(index));
            }
            panel.SetActive(false);
        }

        void OnEnable()
        {
            flow.CardsShown += OnCardsShown;
            flow.ChoiceClosed += OnChoiceClosed;
        }

        void OnDisable()
        {
            flow.CardsShown -= OnCardsShown;
            flow.ChoiceClosed -= OnChoiceClosed;
        }

        void OnCardsShown(IReadOnlyList<LevelUpCard> cards)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (i < cards.Count)
                    slots[i].Show(cards[i]);
                else
                    slots[i].Hide();
            }
            panel.SetActive(true);
        }

        void OnChoiceClosed()
        {
            panel.SetActive(false);
        }
    }
}
