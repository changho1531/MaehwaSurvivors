using System;
using System.Collections.Generic;
using Game.Player;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 보스 혈각마존의 행동 = 패턴 선택기 (설계서 8-C절). 등장 → 틈(추적) → 후보 고르기 → 패턴(경고 → 실행) → 틈 … 을 반복한다.
    /// 패턴이 무엇을 하는지는 모르고 BossPattern만 다루므로, 패턴 추가에 이 클래스를 고치지 않는다 (무기 구조와 같은 OCP).
    /// HP · 피격 · 접촉 데미지는 공통 Enemy가, 생성 · 처치 후 처리는 BossSpawner가 맡는다.
    /// 물리 스텝(게임 시간)으로만 진행하므로 일시정지 · 레벨업 카드 중에는 패턴 · 쿨타임 · 연출이 모두 멈춘다.
    /// </summary>
    public class BossBehaviour : EnemyBehaviour
    {
        [Tooltip("경고 동작(떨림 · 웅크림 · 붉은 기운 · 회전)에 쓸 그림")]
        [SerializeField] SpriteRenderer artRenderer;

        readonly List<BossPattern> patterns = new();
        readonly List<BossPattern> buffer = new();
        BossEnemyData data;
        BossContext ctx;
        BossPattern current;
        BossPattern last;
        BossPattern opening;
        float gapRemaining;
        float retryRemaining;
        float transitionRemaining;
        bool pendingPhase2;
        bool phase2Triggered; // 전환은 한 판에 한 번 (전환 연출 중에 다시 걸리지 않게)
        bool forceOpening;
        bool defeated;

        /// <summary>(새 페이즈) — 전환 연출이 끝나고 2페이즈가 시작될 때</summary>
        public event Action<int> PhaseChanged;
        /// <summary>페이즈 전환 연출 시작 (화면 가장자리 번쩍임)</summary>
        public event Action PhaseTransitionStarted;
        /// <summary>(패턴 이름) — 경고가 시작되는 순간</summary>
        public event Action<string> PatternStarted;

        public BossEnemyData Data => data;
        public int Phase => ctx?.Phase ?? 1;
        public BossPattern Current => current;
        public BossPattern Last => last;
        public IReadOnlyList<BossPattern> Patterns => patterns;
        public bool IsInGap => current == null && gapRemaining > 0f;
        public bool IsTransitioning => transitionRemaining > 0f;
        public bool IsDefeated => defeated;
        public BossTelegraph Telegraph => ctx?.Telegraph;
        /// <summary>테스트 · 시연용: 무작위 대신 이 값으로 고른다 (0~1). 음수면 무작위.</summary>
        public float FixedRoll { get; set; } = -1f;

        BossPattern requested;

        /// <summary>개발자 모드: 다음 패턴을 이것으로 (쿨타임 · 페이즈 · 거리 무시, 틈 생략). 진행 중인 패턴 · 전환은 끝난 뒤.</summary>
        public void RequestPattern(BossPattern pattern)
        {
            requested = pattern;
            gapRemaining = 0f;
            retryRemaining = 0f;
        }

        protected override void Awake()
        {
            base.Awake();
            data = Owner.Data as BossEnemyData;
            ctx = new BossContext
            {
                Owner = Owner,
                Body = Body,
                Telegraph = BossTelegraph.Create(transform),
                Art = new BossArt(artRenderer),
            };
            if (data == null)
                return;
            foreach (var patternData in data.patterns)
            {
                if (patternData == null)
                    continue;
                var pattern = patternData.CreateRuntime();
                patterns.Add(pattern);
                if (patternData == data.phase2OpeningPattern)
                    opening = pattern;
            }
        }

        public override void ResetState()
        {
            if (current != null)
                current.Cancel(ctx);
            current = null;
            last = null;
            ctx.Phase = 1;
            gapRemaining = 0f;
            retryRemaining = 0f;
            transitionRemaining = 0f;
            pendingPhase2 = false;
            phase2Triggered = false;
            forceOpening = false;
            requested = null;
            defeated = false;
            foreach (var pattern in patterns)
                pattern.ResetCooldown();
            ctx.Art.ResetPose();
            ctx.Telegraph.Hide();
        }

        /// <summary>HP 0 → 진행 중 패턴 즉시 중단 (8-C 처치 연출). 이후 아무것도 하지 않는다.</summary>
        public void OnDefeated()
        {
            defeated = true;
            if (current != null)
                current.Cancel(ctx);
            current = null;
            transitionRemaining = 0f;
            ctx.Stop();
        }

        public override void Tick(Transform target, float dt)
        {
            if (data == null || defeated)
            {
                Body.linearVelocity = Vector2.zero;
                return;
            }
            if (ctx.Player != target)
            {
                ctx.Player = target;
                ctx.PlayerHealth = target.GetComponent<PlayerHealth>();
                ctx.PlayerCollider = target.GetComponent<Collider2D>();
                ctx.BodyCollider = GetComponent<Collider2D>();
            }

            // 쿨타임은 패턴이 끝난 뒤부터 센다 → 진행 중인 패턴의 쿨타임은 멈춰 있다
            foreach (var pattern in patterns)
                if (pattern != current)
                    pattern.TickCooldown(dt);

            if (!phase2Triggered && Owner.CurrentHp <= data.maxHp * data.phase2HpRatio)
            {
                phase2Triggered = true;
                pendingPhase2 = true;
            } // 진행 중인 패턴은 끝까지 마친 뒤 전환

            if (transitionRemaining > 0f)
            {
                ctx.Stop();
                transitionRemaining -= dt;
                ctx.Art.Glow(0.5f + 0.5f * Mathf.Abs(Mathf.Sin(transitionRemaining * 12f))); // 붉게 번쩍임
                if (transitionRemaining <= 0f)
                    EnterPhase2();
                return;
            }

            if (current != null)
            {
                if (!current.Tick(ctx, dt))
                    return;
                current.StartCooldown(ctx.Phase);
                last = current;
                current = null;
                gapRemaining = data.GetGap(ctx.Phase);
            }

            if (pendingPhase2)
            {
                StartTransition();
                return;
            }

            ctx.Chase(); // 틈 · 후보 없음: 추적만
            if (gapRemaining > 0f)
            {
                gapRemaining -= dt;
                return;
            }

            retryRemaining -= dt;
            if (retryRemaining > 0f)
                return;
            retryRemaining = data.retrySeconds;

            // 화면 밖(등장 직후 걸어 들어오는 중 등)에서는 패턴 없음 — 보이지 않는 곳에서 맞지 않게
            if (!ScreenCheck.IsVisible(Body.position))
                return;

            var next = requested
                ?? (forceOpening && opening != null
                    ? opening // 2페이즈 첫 패턴은 거리와 상관없이 지면 강타 (페이즈가 바뀐 걸 바로 보여 줌)
                    : BossPatternSelector.Choose(patterns, ctx.Phase, ctx.Distance, last, FixedRoll >= 0f ? FixedRoll : UnityEngine.Random.value, buffer));
            if (next == null)
                return;
            if (next == requested)
                requested = null;
            else
                forceOpening = false;
            StartPattern(next);
        }

        void StartPattern(BossPattern pattern)
        {
            current = pattern;
            pattern.Begin(ctx);
            PatternStarted?.Invoke(pattern.Name);
        }

        void StartTransition()
        {
            pendingPhase2 = false;
            transitionRemaining = Mathf.Max(0.0001f, data.phaseTransitionSeconds);
            ctx.Stop();
            PhaseTransitionStarted?.Invoke();
        }

        void EnterPhase2()
        {
            ctx.Phase = 2;
            ctx.Art.ResetPose();
            foreach (var pattern in patterns)
                pattern.ResetCooldown(); // 곧바로 쓸 수 있게
            gapRemaining = 0f;
            retryRemaining = 0f;
            forceOpening = true;
            PhaseChanged?.Invoke(2);
        }
    }
}
