namespace Game.Weapons
{
    /// <summary>
    /// HUD 무기 슬롯 한 칸에 그릴 내용 (설계서 7-J절). 무기가 자기 상태를 이 값으로 알려 주고, UI는 이 값만 그린다.
    /// </summary>
    public readonly struct WeaponSlotState
    {
        /// <summary>쿨타임 진행률 0 → 1 (1 = 준비 완료)</summary>
        public readonly float CooldownProgress;
        /// <summary>막 · 콤보처럼 켜져 있는 동안</summary>
        public readonly bool IsActive;
        /// <summary>지속 중일 때 남은 비율 1 → 0</summary>
        public readonly float ActiveRemaining;
        /// <summary>강조 (매화보 최대 레벨 가속)</summary>
        public readonly bool IsHighlighted;

        public WeaponSlotState(float cooldownProgress, bool isActive, float activeRemaining, bool isHighlighted)
        {
            CooldownProgress = cooldownProgress;
            IsActive = isActive;
            ActiveRemaining = activeRemaining;
            IsHighlighted = isHighlighted;
        }

        /// <summary>쿨타임 덮개 비율 = 1 − 진행률 (규칙 2). 지속 중에는 덮개 대신 테두리 게이지를 보인다.</summary>
        public float CooldownCover => IsActive ? 0f : 1f - CooldownProgress;
    }
}
