using System;
using Game.Core;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 레벨업 → LevelUp 상태 → 카드 선택 → Play 복귀 흐름 (설계서 6-A절 규칙 5~7).
    /// 대기 중인 레벨업이 남아 있으면 Play로 나가지 않고 LevelUp 상태에서 다음 카드를 바로 요청한다.
    /// 카드가 뜰 때마다 실시간 기준으로 잠깐 선택을 막아, 연타로 다음 카드를 잘못 고르는 것을 방지한다.
    /// </summary>
    public class LevelUpFlow : MonoBehaviour
    {
        [SerializeField] PlayerLevel playerLevel;
        [SerializeField] XpSettings settings;

        [Tooltip("[임시] 카드 UI가 생기기 전까지 입력 잠금이 풀리면 자동으로 선택을 완료한다")]
        [SerializeField] bool autoChooseForPrototype = true;

        float inputUnlockTime;

        /// <summary>카드를 새로 보여 달라는 요청. 카드 UI가 구독한다.</summary>
        public event Action CardsRequested;

        public bool IsAwaitingChoice { get; private set; }
        public bool CanAcceptChoice => IsAwaitingChoice && Time.unscaledTime >= inputUnlockTime;
        public bool AutoChooseForPrototype { get => autoChooseForPrototype; set => autoChooseForPrototype = value; }

        void OnEnable()
        {
            playerLevel.LeveledUp += OnLeveledUp;
        }

        void OnDisable()
        {
            playerLevel.LeveledUp -= OnLeveledUp;
        }

        void Update()
        {
            if (autoChooseForPrototype && CanAcceptChoice)
                CompleteChoice();
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
            IsAwaitingChoice = true;
            inputUnlockTime = Time.unscaledTime + settings.cardInputLockSeconds;
            CardsRequested?.Invoke();
        }

        /// <summary>카드 하나를 고른 직후 호출. 입력 잠금 중이면 무시하고 false.</summary>
        public bool CompleteChoice()
        {
            if (!CanAcceptChoice)
                return false;

            if (playerLevel.ConsumePending() > 0)
            {
                ShowNextCards(); // LevelUp 상태 유지 (규칙 6)
                return true;
            }

            IsAwaitingChoice = false;
            GameManager.Instance.StateMachine.TryChangeState(GameState.Play);
            return true;
        }
    }
}
