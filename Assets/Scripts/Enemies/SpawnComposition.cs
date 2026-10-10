using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 조합 (설계서 8-A절, 9절 적 조합 테이블의 첫 버전): 플레이어 레벨 → 어떤 적이 나올지.
    /// 스폰 간격(시간 기준, SpawnCurve)과 분리된 두 번째 축이다. 항목에 없는 나머지 비율은 기본 적(근거리)이 채운다.
    /// 이후 저주술사·보스는 항목만 추가한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Spawn Composition", fileName = "SpawnComposition")]
    public class SpawnComposition : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public Enemy prefab;
            [Tooltip("이 레벨부터 등장")]
            [Min(1)] public int firstLevel = 2;
            [Tooltip("최대 레벨에서의 비율. 등장 레벨부터 최대 레벨까지 선형으로 늘어난다 ※")]
            [Range(0f, 1f)] public float ratioAtMaxLevel = 0.3f;
        }

        [Tooltip("비율이 최대가 되는 플레이어 레벨 (중간고사 빌드 Lv10) ※")]
        [Min(1)] public int maxLevel = 10;
        public Entry[] entries = Array.Empty<Entry>();

        /// <summary>해당 레벨에서 이 항목이 차지하는 비율 (0~1).</summary>
        public float GetRatio(Entry entry, int level)
        {
            if (level < entry.firstLevel)
                return 0f;
            int steps = Mathf.Max(1, maxLevel - entry.firstLevel + 1);
            int step = Mathf.Min(level, maxLevel) - entry.firstLevel + 1;
            return entry.ratioAtMaxLevel * step / steps;
        }

        /// <summary>roll(0~1 난수)로 고른 적 프리팹. null이면 기본 적.</summary>
        public Enemy Pick(int level, float roll)
        {
            float cumulative = 0f;
            foreach (var entry in entries)
            {
                cumulative += GetRatio(entry, level);
                if (roll < cumulative)
                    return entry.prefab;
            }
            return null;
        }
    }
}
