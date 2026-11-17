using Game.Enemies;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 이번 런의 기록 (설계서 7-B절): 생존 시간, 처치 수. HUD와 결과 화면이 읽는다.
    /// 생존 시간은 게임 시간(deltaTime)으로 누적하므로 Pause·LevelUp·GameOver 중에는 자동으로 멈춘다 (규칙 9).
    /// 처치 수는 Enemy.Killed 1회 = 1 (규칙 10, 적 종류 구분 없음). 적은 이 클래스를 모른다 (Observer).
    /// 씬과 함께 생성·파괴되므로 재도전 시 씬 재로드로 자연히 0부터 시작한다.
    /// </summary>
    public class RunStats : MonoBehaviour
    {
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }

        /// <summary>보스를 쓰러뜨렸는지 (결과 화면 제목 · 색, 11절 MU-C-002)</summary>
        public bool Cleared { get; private set; }
        /// <summary>[보스 소환] 버튼으로 조기 소환한 판인지</summary>
        public bool BossSummonedEarly { get; private set; }
        /// <summary>보스가 나온 시각 (경과 시간)</summary>
        public float BossSummonedAt { get; private set; }

        /// <summary>보스 처치 시 BossSpawner가 기록한다 (8-C 이벤트 · 연결).</summary>
        public void RecordClear(bool summonedEarly, float summonedAt)
        {
            Cleared = true;
            BossSummonedEarly = summonedEarly;
            BossSummonedAt = summonedAt;
        }

        void OnEnable() => Enemy.Killed += OnEnemyKilled;
        void OnDisable() => Enemy.Killed -= OnEnemyKilled;

        void Update()
        {
            Elapsed += Time.deltaTime;
        }

        void OnEnemyKilled(Enemy enemy) => Kills++;

        /// <summary>"mm:ss" (60분 이상이면 분이 그대로 늘어난다).</summary>
        public static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
