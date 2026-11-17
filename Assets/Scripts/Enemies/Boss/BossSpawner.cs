using System;
using System.Collections;
using Game.Core;
using Game.Player;
using Game.Pooling;
using Game.Progression;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 보스 등장 · 처치 (설계서 8-C절). WaveDirector.BossPhaseStarted(10분 또는 조기 소환)를 구독해 화면 밖 위쪽에 보스를 1마리 만들고,
    /// 처치되면 짧은 슬로 모션 → 남은 적 · 투사체 · 장판 정리 → GameClear로 넘긴다. HP 바 · 문구 · 대사는 BossSpawned를 구독한다 (Observer).
    /// 보스는 한 판에 1마리라 풀을 쓰지 않고 처음 등장할 때 만든다.
    /// </summary>
    public class BossSpawner : MonoBehaviour
    {
        [SerializeField] WaveDirector waves;
        [SerializeField] EnemySpawner spawner;
        [SerializeField] Enemy bossPrefab;
        [SerializeField] Transform player;
        [SerializeField] RunStats stats;
        [Tooltip("이 카메라 시야 바로 위쪽 바깥에서 생성")]
        [SerializeField] Camera viewCamera;
        [Header("※ 예시값")]
        [SerializeField, Min(0f)] float spawnMargin = 1.5f;
        [Tooltip("처치 연출: 게임 속도 배율과 실시간 길이")]
        [SerializeField, Range(0.05f, 1f)] float defeatTimeScale = 0.3f;
        [SerializeField, Min(0f)] float defeatSeconds = 0.5f;

        PlayerHealth playerHealth;
        float damageDealt;
        float fightStartedAt;
        float fightSeconds;

        /// <summary>(보스) — HP 바, 등장 문구, (이후) AI 대사 ELITE_SEEN</summary>
        public event Action<Enemy> BossSpawned;
        /// <summary>HP 0이 된 순간 (클리어 화면은 연출 뒤)</summary>
        public event Action BossDefeated;

        public Enemy Boss { get; private set; }
        public BossBehaviour Behaviour { get; private set; }
        public bool IsDefeated { get; private set; }
        public float DamageDealt => damageDealt;

        void Awake()
        {
            if (player != null)
                playerHealth = player.GetComponent<PlayerHealth>();
        }

        void OnEnable()
        {
            if (waves != null)
                waves.BossPhaseStarted += OnBossPhaseStarted;
        }

        void OnDisable()
        {
            if (waves != null)
                waves.BossPhaseStarted -= OnBossPhaseStarted;
        }

        void OnBossPhaseStarted(bool early, float at) => SpawnBoss();

        /// <summary>보스 생성. 이미 있으면 그대로 (BossPhaseStarted 한 번에 1마리).</summary>
        public Enemy SpawnBoss()
        {
            if (Boss != null || bossPrefab == null || player == null)
                return Boss;

            // 플레이어 기준 화면 밖 위쪽 → 걸어 들어온다 (들어오는 동안은 패턴 없음)
            float up = viewCamera != null && viewCamera.orthographic ? viewCamera.orthographicSize : 6f;
            var position = player.position + Vector3.up * (up + spawnMargin);
            Boss = Instantiate(bossPrefab, position, Quaternion.identity);
            Boss.name = bossPrefab.name;
            Behaviour = Boss.GetComponent<BossBehaviour>();
            Boss.SetSource(spawner);
            // 이동속도는 웨이브 배율 · 상한을 쓰지 않고 데이터 값 그대로 (8-C 기본 수치)
            Boss.Init(player, OnBossDown, Boss.Data.moveSpeed);
            Boss.Damaged += OnBossDamaged;
            damageDealt = 0f;
            fightStartedAt = stats != null ? stats.Elapsed : Time.time;
            BossSpawned?.Invoke(Boss);
            return Boss;
        }

        void OnBossDamaged(float amount) => damageDealt += amount;

        // Enemy가 HP 0에서 부른다 (풀 반환 대신). 보스는 연출이 끝날 때까지 화면에 남긴다
        void OnBossDown(Enemy boss)
        {
            if (IsDefeated)
                return;
            IsDefeated = true;
            fightSeconds = (stats != null ? stats.Elapsed : Time.time) - fightStartedAt;
            if (playerHealth != null)
                playerHealth.DamageLocked = true; // 처치 순간부터 플레이어는 맞지 않는다
            if (Behaviour != null)
                Behaviour.OnDefeated();
            BossDefeated?.Invoke();

            // 같은 프레임에 플레이어가 먼저 죽었다면 클리어 우선 — 연출 없이 바로 넘긴다
            if (GameManager.IsIn(GameState.GameOver) && playerHealth != null && playerHealth.DiedFrame == Time.frameCount)
            {
                FinishClear();
                return;
            }
            StartCoroutine(DefeatSequence());
        }

        IEnumerator DefeatSequence()
        {
            var art = Boss.GetComponentInChildren<SpriteRenderer>();
            var baseColor = art != null ? art.color : Color.white;
            Time.timeScale = defeatTimeScale;
            float t = 0f;
            while (t < defeatSeconds)
            {
                t += Time.unscaledDeltaTime;
                if (art != null)
                {
                    // 붉게 번쩍이며 사라짐
                    float k = Mathf.Clamp01(t / Mathf.Max(0.01f, defeatSeconds));
                    var c = Color.Lerp(baseColor, new Color(1f, 0.15f, 0.1f), Mathf.Abs(Mathf.Sin(t * 30f)));
                    c.a = 1f - k;
                    art.color = c;
                }
                yield return null;
            }
            // 일시정지 · 카드 선택 중이면 끝날 때까지 기다렸다가 넘긴다
            while (!GameManager.IsIn(GameState.Play) && GameManager.Current != null && !GameManager.IsIn(GameState.GameOver))
                yield return null;
            if (GameManager.IsIn(GameState.GameOver))
                yield break; // 이미 끝난 판은 클리어로 바꾸지 않는다 (같은 프레임만 예외, OnBossDown)
            if (GameManager.IsIn(GameState.Play))
                Time.timeScale = 1f;
            FinishClear();
        }

        void FinishClear()
        {
            Boss.gameObject.SetActive(false);
            // 화면의 남은 적 · 투사체 · 장판을 모두 정리하고 새로 나오지 않게
            if (spawner != null)
            {
                spawner.ReleaseAll();
                spawner.enabled = false;
            }
            PrefabPools.ReleaseAll();
            if (stats != null && waves != null)
                stats.RecordClear(waves.BossSummonedEarly, waves.BossPhaseStartedAt);
            GameManager.TryChangeState(GameState.GameClear);
        }

        /// <summary>개발용 측정 (8-C): 보스에게 준 초당 데미지와 처치 시간 → HP 확정용. GrowthLogger가 함께 출력한다.</summary>
        public string MeasurementLine
        {
            get
            {
                if (Boss == null)
                    return null;
                float seconds = IsDefeated ? fightSeconds : (stats != null ? stats.Elapsed : Time.time) - fightStartedAt;
                float dps = seconds > 0f ? damageDealt / seconds : 0f;
                return $"[보스 측정] {(IsDefeated ? "처치" : "미처치")}  전투 {seconds:0.0}초  준 데미지 {damageDealt:0}  초당 {dps:0.0}  (최대 HP {Boss.Data.maxHp:0})";
            }
        }
    }
}
