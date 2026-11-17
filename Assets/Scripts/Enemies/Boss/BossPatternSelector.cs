using System.Collections.Generic;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 패턴 고르기 규칙 (설계서 8-C 패턴 선택 규칙 3 · 4). Unity 의존 없는 순수 규칙 → EditMode 테스트.
    /// 후보 = 쿨타임이 끝났고 거리 · 페이즈 조건에 맞는 패턴. 후보 중 직전 패턴은 빼고 무작위 1개,
    /// 직전 패턴만 남았으면 그것도 허용. 후보가 없으면 null (보스는 추적하며 잠시 뒤 다시 확인).
    /// </summary>
    public static class BossPatternSelector
    {
        public static BossPattern Choose(IReadOnlyList<BossPattern> patterns, int phase, float distance, BossPattern last, float roll, List<BossPattern> buffer)
        {
            buffer.Clear();
            foreach (var pattern in patterns)
                if (pattern.CanUse(phase, distance))
                    buffer.Add(pattern);
            if (buffer.Count == 0)
                return null;
            if (buffer.Count > 1 && last != null)
                buffer.Remove(last); // 같은 패턴 연속 금지 (대안이 있을 때만)

            int index = (int)(roll * buffer.Count);
            if (index >= buffer.Count)
                index = buffer.Count - 1;
            return buffer[index];
        }
    }
}
