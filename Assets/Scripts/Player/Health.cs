using System;

namespace Game.Player
{
    /// <summary>
    /// HP 규칙만 담은 순수 클래스 (Unity 의존 없음 → EditMode 테스트). 설계서 7-B절.
    /// 무적 시간 없음: 맞는 족족 깎인다 (포위당하면 빠르게 무너지는 긴장감이 의도). 회복 수단 없음.
    /// </summary>
    public class Health
    {
        public float Max { get; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        public Health(float max)
        {
            Max = Math.Max(1f, max);
            Current = Max;
        }

        /// <summary>
        /// 데미지 적용. 이미 죽었거나 0 이하 데미지면 무시하고 false.
        /// justDied는 이번 호출로 처음 0이 되었을 때만 true → 같은 프레임에 여러 적이 마지막 데미지를 줘도 사망 처리는 1회.
        /// </summary>
        public bool TryDamage(float amount, out bool justDied)
        {
            justDied = false;
            if (IsDead || amount <= 0f)
                return false;

            Current = Math.Max(0f, Current - amount);
            justDied = IsDead;
            return true;
        }
    }
}
