using System.Collections.Generic;
using DG.Tweening;
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
        [Header("선택 연출 ※")]
        [SerializeField, Min(0f)] float chosenGrowSeconds = 0.12f;
        [SerializeField, Min(0f)] float chosenFadeSeconds = 0.28f;
        [SerializeField, Min(1f)] float chosenEndScale = 1.3f;

        GameObject chosenGhost;

        public GameObject Panel => panel;
        public IReadOnlyList<LevelUpCardSlot> Slots => slots;
        /// <summary>선택 연출 중인 고른 카드의 잔상 (없으면 null)</summary>
        public GameObject ChosenGhost => chosenGhost;

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
            flow.CardChosen += OnCardChosen;
            flow.ChoiceClosed += OnChoiceClosed;
        }

        void OnDisable()
        {
            flow.CardsShown -= OnCardsShown;
            flow.CardChosen -= OnCardChosen;
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

        // 흐름은 고르는 즉시 다음 카드 / Play로 넘어가므로(로직 지연 없음), 고른 카드의 잔상 사본을 화면 위에 남겨 커졌다 흩어지게 한다.
        // 카드 화면 밖(캔버스 바로 아래)에 두어 패널이 닫혀도, 다음 카드가 떠도 끝까지 재생된다. 실시간 기준.
        void OnCardChosen(int index)
        {
            if (index < 0 || index >= slots.Length)
                return;
            if (chosenGhost != null)
                Destroy(chosenGhost);

            var source = (RectTransform)slots[index].transform;
            var root = source.GetComponentInParent<Canvas>().rootCanvas.transform;
            chosenGhost = Instantiate(source.gameObject, root, true);
            chosenGhost.name = "ChosenCard";
            Destroy(chosenGhost.GetComponent<LevelUpCardSlot>());
            var rect = (RectTransform)chosenGhost.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = source.rect.size;
            rect.position = source.position;
            rect.localScale = Vector3.one;
            var group = chosenGhost.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; // 잔상이 다음 카드 클릭을 가리지 않게
            group.interactable = false;

            var ghost = chosenGhost;
            DOTween.Sequence()
                .Append(rect.DOScale(1.12f, chosenGrowSeconds).SetEase(Ease.OutQuad))
                .Append(rect.DOScale(chosenEndScale, chosenFadeSeconds).SetEase(Ease.InQuad))
                .Join(group.DOFade(0f, chosenFadeSeconds).SetEase(Ease.InQuad))
                .SetUpdate(true).SetLink(ghost, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => Destroy(ghost));
        }

        void OnChoiceClosed()
        {
            panel.SetActive(false);
        }
    }
}
