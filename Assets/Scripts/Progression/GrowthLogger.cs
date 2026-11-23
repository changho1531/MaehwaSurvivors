using Game.Core;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 개발용 성장 측정 (설계서 7-I절 수치 3). PlayerLevel 이벤트만 구독해 GrowthRecord에 쌓고, 런이 끝나면(GameOver·GameClear,
    /// 또는 씬을 떠날 때) 콘솔에 한 번 출력한다. 시각은 RunStats의 게임 시간 기준이라 일시정지·카드 선택 시간은 빠진다.
    /// 에디터·Development Build에서만 동작하고 릴리스 빌드에서는 스스로 꺼진다 (DebugKeys와 같은 기준).
    /// </summary>
    public class GrowthLogger : MonoBehaviour
    {
        [SerializeField] PlayerLevel level;
        [SerializeField] RunStats stats;
        [Tooltip("있으면 보스전 측정(초당 데미지 · 처치 시간)을 함께 출력 (8-C, HP 확정용)")]
        [SerializeField] Game.Enemies.Boss.BossSpawner boss;
        [Tooltip("※ 경험치를 묶어 세는 구간(초)")]
        [SerializeField, Min(1f)] float bucketSeconds = 30f;

        GrowthRecord record;
        bool logged;
        GameStateMachine machine;

        public GrowthRecord Record => record ??= new GrowthRecord(bucketSeconds);
        public bool Logged => logged;

        float Now => stats != null ? stats.Elapsed : Time.timeSinceLevelLoad;

        void Awake()
        {
            if (!Debug.isDebugBuild)
                enabled = false;
        }

        void OnEnable()
        {
            if (level != null)
            {
                level.XpAdded += OnXpAdded;
                level.LeveledUp += OnLeveledUp;
            }
            // 매니저는 씬 진입점(GameBootstrap)이 먼저 만든다
            machine = GameManager.Current != null ? GameManager.Current.StateMachine : null;
            if (machine != null)
                machine.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            if (level != null)
            {
                level.XpAdded -= OnXpAdded;
                level.LeveledUp -= OnLeveledUp;
            }
            if (machine != null)
                machine.StateChanged -= OnStateChanged;
            machine = null;
        }

        void OnDestroy()
        {
            // 일시정지 → 타이틀처럼 결과 화면 없이 끝난 런도 기록을 남긴다
            if (enabled && Record.Levels.Count > 0)
                Flush("중단");
        }

        void OnXpAdded(int amount) => Record.AddXp(amount, Now);

        void OnLeveledUp(int gained)
        {
            // 한 번에 여러 레벨이 올라도 레벨마다 한 줄 (같은 시각)
            for (int l = level.Level - gained + 1; l <= level.Level; l++)
                Record.RecordLevel(l, Now);
        }

        void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.GameOver || next == GameState.GameClear)
                Flush(next.ToString());
        }

        public void Flush(string ending)
        {
            if (logged)
                return;
            logged = true;
            var text = Record.Format(ending, Now);
            var bossLine = boss != null ? boss.MeasurementLine : null;
            Debug.Log(bossLine != null ? text + "\n" + bossLine : text);
        }
    }
}
