using System;

namespace Game.Player
{
    /// <summary>
    /// HP + 피격 후 무적 시간 규칙만 담은 순수 클래스 (Unity 의존 없음 → EditMode 테스트).
    /// 시간은 호출자가 넘겨 주므로, 일시정지(게임 시간 정지) 중에는 무적 시간도 흐르지 않는다.
    /// </summary>
    public class Health
    {
        float invulnerableUntil = float.NegativeInfinity;

        public float Max { get; }
        public float Current { get; private set; }
        public float InvulnerabilitySeconds { get; }
        public bool IsDead => Current <= 0f;

        public Health(float max, float invulnerabilitySeconds)
        {
            Max = Math.Max(1f, max);
            Current = Max;
            InvulnerabilitySeconds = Math.Max(0f, invulnerabilitySeconds);
        }

        public bool IsInvulnerable(float now) => now < invulnerableUntil;

        /// <summary>
        /// 데미지 적용. 죽었거나 무적 중이면 무시하고 false.
        /// 맞으면 무적 시간 시작 → 적 여러 마리에 둘러싸여도 매 물리 스텝마다 깎이지 않는다.
        /// </summary>
        public bool TryDamage(float amount, float now)
        {
            if (IsDead || amount <= 0f || IsInvulnerable(now))
                return false;

            Current = Math.Max(0f, Current - amount);
            invulnerableUntil = now + InvulnerabilitySeconds;
            return true;
        }
    }
}
