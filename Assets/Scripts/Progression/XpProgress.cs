using System;

namespace Game.Progression
{
    /// <summary>
    /// 경험치·레벨 계산만 담당하는 순수 C# 클래스 (Unity 의존 없음 → EditMode에서 바로 테스트).
    /// 한 번에 여러 레벨이 올라도 초과분을 이월하며 반복 비교하고, 오른 횟수를 "대기 중인 레벨업"으로 쌓는다.
    /// </summary>
    public class XpProgress
    {
        readonly Func<int, int> requiredXpForLevel;

        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public int RequiredXp => requiredXpForLevel(Level);
        public int PendingLevelUps { get; private set; }

        /// <summary>성장 완료(모든 무기 최대 레벨). 이후 경험치를 받지 않는다 (6-A절 규칙 8).</summary>
        public bool IsMaxed { get; private set; }

        public XpProgress(Func<int, int> requiredXpForLevel)
        {
            this.requiredXpForLevel = requiredXpForLevel;
        }

        /// <summary>경험치를 더하고 이번에 오른 레벨 수를 돌려준다.</summary>
        public int AddXp(int amount)
        {
            if (IsMaxed || amount <= 0)
                return 0;

            CurrentXp += amount;
            int gained = 0;
            while (CurrentXp >= RequiredXp)
            {
                CurrentXp -= RequiredXp;
                Level++;
                gained++;
            }

            PendingLevelUps += gained;
            return gained;
        }

        /// <summary>카드 선택 1회 = 대기 1회 소모. 남은 대기 횟수를 돌려준다.</summary>
        public int ConsumePending()
        {
            if (PendingLevelUps > 0)
                PendingLevelUps--;
            return PendingLevelUps;
        }

        /// <summary>고를 카드가 더 없을 때 남은 대기 횟수를 버린다.</summary>
        public void DiscardPending()
        {
            PendingLevelUps = 0;
        }

        public void MarkMaxed()
        {
            IsMaxed = true;
            PendingLevelUps = 0;
        }
    }
}
