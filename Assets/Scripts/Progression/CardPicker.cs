using System.Collections.Generic;

namespace Game.Progression
{
    /// <summary>후보에서 서로 다른 항목을 최대 count개 무작위로 뽑는다 (설계서 6절: 2개 추출, 중복 방지, 부족하면 있는 만큼).</summary>
    public static class CardPicker
    {
        public static void Pick<T>(IReadOnlyList<T> candidates, int count, System.Random random, List<T> result)
        {
            result.Clear();
            result.AddRange(candidates);

            // 부분 Fisher-Yates: 앞 count칸만 섞으면 중복 없이 무작위 count개가 된다.
            int take = System.Math.Min(count, result.Count);
            for (int i = 0; i < take; i++)
            {
                int j = random.Next(i, result.Count);
                (result[i], result[j]) = (result[j], result[i]);
            }
            result.RemoveRange(take, result.Count - take);
        }
    }
}
