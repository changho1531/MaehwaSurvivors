using System;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 플레이어의 경험치·레벨 (설계서 6-A절). 계산은 XpProgress에 맡기고, 여기서는 이벤트로 바깥에 알리기만 한다.
    /// HUD는 XpChanged를, 레벨업 흐름(LevelUpFlow)은 LeveledUp을 구독한다 — 서로를 직접 알지 않는다.
    /// </summary>
    public class PlayerLevel : MonoBehaviour
    {
        [SerializeField] LevelCurve curve;

        XpProgress progress;

        /// <summary>(현재 XP, 필요 XP). 성장 완료 시 (필요, 필요)로 꽉 찬 값을 보낸다.</summary>
        public event Action<int, int> XpChanged;

        /// <summary>(이번에 오른 레벨 수)</summary>
        public event Action<int> LeveledUp;

        /// <summary>성장 완료 진입 (HUD의 MAX 표시용).</summary>
        public event Action Maxed;

        public int Level => Progress.Level;
        public int CurrentXp => Progress.CurrentXp;
        public int RequiredXp => Progress.RequiredXp;
        public int PendingLevelUps => Progress.PendingLevelUps;
        public bool IsMaxed => Progress.IsMaxed;

        XpProgress Progress => progress ??= new XpProgress(level => curve != null ? curve.GetRequiredXp(level) : 1);

        public void AddXp(int amount)
        {
            if (IsMaxed || amount <= 0)
                return;

            int gained = Progress.AddXp(amount);
            XpChanged?.Invoke(CurrentXp, RequiredXp);
            if (gained > 0)
                LeveledUp?.Invoke(gained);
        }

        public int ConsumePending() => Progress.ConsumePending();

        public void DiscardPending() => Progress.DiscardPending();

        /// <summary>성장 완료: 남은 대기 폐기, 경험치 바를 꽉 찬 상태로 고정 (6-A절 규칙 8).</summary>
        public void MarkMaxed()
        {
            if (IsMaxed)
                return;
            Progress.MarkMaxed();
            XpChanged?.Invoke(RequiredXp, RequiredXp);
            Maxed?.Invoke();
        }
    }
}
