using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 개발용 성장 측정 기록 (설계서 7-I절 수치 3): ① 레벨 도달 시각 ② 일정 구간(30초)마다 얻은 경험치.
    /// 필요 경험치 표는 스폰량·처치 속도에 달려 있어 책상에서 정할 수 없으므로, 몇 판 플레이한 기록으로 계산한다.
    /// 규칙만 담은 순수 클래스라 EditMode에서 테스트한다.
    /// </summary>
    public class GrowthRecord
    {
        readonly float bucketSeconds;
        readonly List<(int level, float time)> levels = new();
        readonly List<int> xpPerBucket = new();

        public GrowthRecord(float bucketSeconds = 30f)
        {
            this.bucketSeconds = Mathf.Max(1f, bucketSeconds);
        }

        public IReadOnlyList<(int level, float time)> Levels => levels;
        public IReadOnlyList<int> XpPerBucket => xpPerBucket;
        public float BucketSeconds => bucketSeconds;

        public void RecordLevel(int level, float time) => levels.Add((level, time));

        public void AddXp(int amount, float time)
        {
            if (amount <= 0)
                return;
            int index = Mathf.Max(0, Mathf.FloorToInt(time / bucketSeconds));
            while (xpPerBucket.Count <= index)
                xpPerBucket.Add(0);
            xpPerBucket[index] += amount;
        }

        /// <summary>콘솔에 붙여 넣기 좋은 표 형태</summary>
        public string Format(string ending, float elapsed)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[성장 측정] 종료: {ending}, 경과 {RunStats.Format(elapsed)} ({elapsed:F1}s)");
            sb.AppendLine("레벨\t도달(초)\t간격(초)");
            float previous = 0f;
            foreach (var (level, time) in levels)
            {
                sb.AppendLine($"Lv{level}\t{time:F1}\t{time - previous:F1}");
                previous = time;
            }
            sb.AppendLine($"구간({bucketSeconds:0}초)\t경험치\t분당");
            for (int i = 0; i < xpPerBucket.Count; i++)
                sb.AppendLine($"{RunStats.Format(i * bucketSeconds)}~\t{xpPerBucket[i]}\t{xpPerBucket[i] * 60f / bucketSeconds:F0}");
            return sb.ToString();
        }
    }
}
