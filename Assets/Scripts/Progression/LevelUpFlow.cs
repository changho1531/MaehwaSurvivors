using System;
using System.Collections.Generic;
using Game.Core;
using Game.Weapons;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>레벨업 카드 1장의 내용. 보유 중이면 강화, 미보유면 신규 획득.</summary>
    public readonly struct LevelUpCard
    {
        public readonly WeaponData Weapon;
        public readonly bool IsNew;
        public readonly int NextLevel;

        public LevelUpCard(WeaponData weapon, bool isNew, int nextLevel)
        {
            Weapon = weapon;
            IsNew = isNew;
            NextLevel = nextLevel;
        }
    }

    /// <summary>
    /// 레벨업 → LevelUp 상태 → 카드 선택 → Play 복귀 흐름 (설계서 6절, 6-A절 규칙 5~7).
    /// 대기 중인 레벨업이 남아 있으면 Play로 나가지 않고 LevelUp 상태에서 다음 카드를 바로 뽑는다.
    /// 카드가 뜰 때마다 실시간 기준으로 잠깐 선택을 막아, 연타로 다음 카드를 잘못 고르는 것을 방지한다.
    /// 화면 표시는 카드 UI가 CardsShown/ChoiceClosed 이벤트를 구독해 처리한다 (흐름과 UI 분리).
    /// </summary>
    public class LevelUpFlow : MonoBehaviour
    {
        [SerializeField] PlayerLevel playerLevel;
        [SerializeField] WeaponInventory inventory;
        [SerializeField] XpSettings settings;
        [Tooltip("※ 예시값. 한 번에 보여 줄 카드 수")]
        [SerializeField, Min(1)] int cardsPerLevelUp = 2;

        readonly List<WeaponData> candidates = new();
        readonly List<WeaponData> picked = new();
        readonly List<LevelUpCard> cards = new();
        readonly System.Random random = new();
        float inputUnlockTime;

        /// <summary>새 카드가 뽑혔다. 카드 UI가 구독한다.</summary>
        public event Action<IReadOnlyList<LevelUpCard>> CardsShown;

        /// <summary>카드 선택이 모두 끝나 Play로 돌아간다.</summary>
        public event Action ChoiceClosed;

        public IReadOnlyList<LevelUpCard> CurrentCards => cards;
        public bool IsAwaitingChoice { get; private set; }
        public bool CanAcceptChoice => IsAwaitingChoice && Time.unscaledTime >= inputUnlockTime;

        void OnEnable()
        {
            playerLevel.LeveledUp += OnLeveledUp;
        }

        void OnDisable()
        {
            playerLevel.LeveledUp -= OnLeveledUp;
        }

        void OnLeveledUp(int gained)
        {
            if (IsAwaitingChoice)
                return; // 이미 카드 표시 중이면 대기 횟수만 늘어난 것이므로 그대로 이어서 처리된다.

            if (!GameManager.Instance.StateMachine.TryChangeState(GameState.LevelUp))
                return;
            ShowNextCards();
        }

        void ShowNextCards()
        {
            inventory.GetCandidates(candidates);
            if (candidates.Count == 0)
            {
                // 고를 카드가 없으면 남은 대기 횟수를 버리고 복귀 (6-A절 예외)
                playerLevel.DiscardPending();
                Close();
                return;
            }

            CardPicker.Pick(candidates, cardsPerLevelUp, random, picked);
            cards.Clear();
            foreach (var weapon in picked)
            {
                int level = inventory.GetLevel(weapon);
                cards.Add(new LevelUpCard(weapon, level == 0, level + 1));
            }

            IsAwaitingChoice = true;
            inputUnlockTime = Time.unscaledTime + settings.cardInputLockSeconds;
            CardsShown?.Invoke(cards);
        }

        /// <summary>카드 선택. 입력 잠금 중이거나 잘못된 번호면 무시하고 false.</summary>
        public bool Choose(int index)
        {
            if (!CanAcceptChoice || index < 0 || index >= cards.Count)
                return false;

            inventory.Apply(cards[index].Weapon);

            if (playerLevel.ConsumePending() > 0)
            {
                ShowNextCards(); // LevelUp 상태 유지 (규칙 6)
                return true;
            }

            Close();
            return true;
        }

        void Close()
        {
            IsAwaitingChoice = false;
            cards.Clear();
            ChoiceClosed?.Invoke();
            GameManager.Instance.StateMachine.TryChangeState(GameState.Play);
        }
    }
}
