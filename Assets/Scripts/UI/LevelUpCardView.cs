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
        [Header("등장 연출 ※")]
        [SerializeField, Range(0.1f, 1f)] float popFrom = 0.7f;
        [SerializeField, Min(0f)] float popSeconds = 0.25f;
        [SerializeField, Min(0f)] float popStagger = 0.06f;

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
            // 카드가 차례로 튀어나온다. 카드 중에는 게임 시간이 멈춰 있어 실시간 기준 (입력 잠금 0.2초와 비슷한 길이)
            for (int i = 0; i < cards.Count && i < slots.Length; i++)
                UiTween.PopIn(slots[i].transform, popFrom, popSeconds, i * popStagger);
        }

        void OnChoiceClosed()
        {
            panel.SetActive(false);
        }
    }
}
