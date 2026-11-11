using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 웨이브 진행 (설계서 9-B절). 경과 시간(게임 시간)으로 현재 웨이브를 판단해 바뀌면 WaveChanged를 내고,
    /// 보스 등장 시각(또는 조기 소환)에 보스 단계로 넘어가 BossPhaseStarted를 낸다. 스포너는 CurrentSettings만 읽는다.
    /// 배너·버튼·(이후) 보스 생성·AI 대사는 이벤트를 구독하므로 이 클래스는 그들을 모른다 (Observer).
    /// 일시정지·레벨업 카드 중에는 deltaTime이 0이라 웨이브 시간도 멈춘다.
    /// </summary>
    public class WaveDirector : MonoBehaviour
    {
        [SerializeField] WaveTable table;

        /// <summary>(새 웨이브 번호, 1부터)</summary>
        public event Action<int> WaveChanged;
        /// <summary>(조기 소환 여부, 그때의 경과 시간)</summary>
        public event Action<bool, float> BossPhaseStarted;

        public WaveTable Table => table;
        public float Elapsed { get; private set; }
        public int CurrentWave { get; private set; } = 1;
        public bool IsBossPhase { get; private set; }
        public bool BossSummonedEarly { get; private set; }
        public float BossPhaseStartedAt { get; private set; }

        /// <summary>[보스 소환] 버튼 표시 조건: 조기 소환 가능 시각 이후 + 아직 보스 단계 전</summary>
        public bool CanSummonEarly => table != null && !IsBossPhase && Elapsed >= table.earlySummonSeconds;

        public WaveTable.Settings CurrentSettings =>
            table == null ? null : IsBossPhase ? table.bossPhase : table.GetSettings(CurrentWave);

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            Advance(Elapsed + dt);
        }

        /// <summary>경과 시간을 옮기고 그 사이 일어날 전환을 처리한다. 테스트·디버그에서 특정 시각으로 건너뛸 때도 쓴다.</summary>
        public void Advance(float seconds)
        {
            Elapsed = Mathf.Max(Elapsed, seconds);
            if (table == null || IsBossPhase)
                return;

            if (Elapsed >= table.bossSeconds)
            {
                StartBossPhase(early: false);
                return;
            }

            int wave = table.GetWaveNumber(Elapsed);
            if (wave != CurrentWave)
            {
                // 쉬는 시간 없이 바로 다음 구성으로. 이미 나와 있는 적은 그대로 (생성 때 속도 유지)
                CurrentWave = wave;
                WaveChanged?.Invoke(wave);
            }
        }

        /// <summary>[보스 소환] 버튼. 확인창 없이 바로, 되돌릴 수 없음 (9-B 규칙 7).</summary>
        public bool SummonBossEarly()
        {
            if (!CanSummonEarly || Time.timeScale <= 0f)
                return false;
            StartBossPhase(early: true);
            return true;
        }

        void StartBossPhase(bool early)
        {
            IsBossPhase = true;
            BossSummonedEarly = early;
            BossPhaseStartedAt = Elapsed;
            // 남은 웨이브는 건너뛴다. 보스 생성·등장 연출은 보스 명세에서 이 이벤트를 구독해 붙인다
            BossPhaseStarted?.Invoke(early, Elapsed);
        }
    }
}
