using Game.Player;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 패턴이 보스를 움직이고 플레이어를 때릴 때 쓰는 손잡이. 패턴은 BossBehaviour를 모르고 이것만 본다.
    /// </summary>
    public sealed class BossContext
    {
        public Enemy Owner;
        public Rigidbody2D Body;
        public Transform Player;
        public PlayerHealth PlayerHealth;
        public BossTelegraph Telegraph;
        public BossArt Art;
        public int Phase = 1;

        public Vector2 Position => Body.position;
        public Vector2 ToPlayer => Player != null ? (Vector2)Player.position - Body.position : Vector2.zero;
        public float Distance => ToPlayer.magnitude;

        /// <summary>플레이어 쪽 단위 방향. 겹쳐 있으면 오른쪽.</summary>
        public Vector2 DirectionToPlayer
        {
            get
            {
                var v = ToPlayer;
                return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.right;
            }
        }

        public Collider2D BodyCollider;
        public Collider2D PlayerCollider;

        /// <summary>
        /// 플레이어와의 물리 충돌을 잠시 끈다 (돌진은 플레이어를 뚫고 8칸 끝까지 간다, 8-C 예외).
        /// 끄지 않으면 움직이지 않는 플레이어 몸에 막혀 그 자리에 멈춘다.
        /// </summary>
        public void PassThroughPlayer(bool on)
        {
            if (BodyCollider != null && PlayerCollider != null)
                Physics2D.IgnoreCollision(BodyCollider, PlayerCollider, on);
        }

        public void Chase() => Body.linearVelocity = DirectionToPlayer * Owner.MoveSpeed;
        public void Stop() => Body.linearVelocity = Vector2.zero;

        /// <summary>모든 보스 데미지는 PlayerHealth 진입점으로 (자하호신강 감소 적용).</summary>
        public void HitPlayer(float amount)
        {
            if (PlayerHealth != null && amount > 0f)
                PlayerHealth.TakeDamage(amount);
        }
    }

    /// <summary>
    /// 보스 패턴의 진행 (설계서 8-C절): 경고 → 실행 → 끝. 쿨타임은 끝난 뒤부터 센다(무기 루프와 같은 원칙).
    /// 경고 동안 보스는 멈추고, 방향 같은 "어디를 때릴지"는 경고 시작 순간에 정해 바꾸지 않는다 — 보고 피할 수 있게.
    /// 하위 클래스는 "무엇을 경고하고 어떻게 때리나"만 구현한다.
    /// </summary>
    public abstract class BossPattern
    {
        float warningRemaining;

        protected BossPattern(BossPatternData data) => Data = data;

        public BossPatternData Data { get; }
        public string Name => Data.displayName;
        public float CooldownRemaining { get; private set; }
        public bool IsReady => CooldownRemaining <= 0f;
        public bool InWarning { get; private set; }

        public void TickCooldown(float dt) => CooldownRemaining = Mathf.Max(0f, CooldownRemaining - dt);
        public void StartCooldown(int phase) => CooldownRemaining = Data.GetCooldown(phase);
        public void ResetCooldown() => CooldownRemaining = 0f;

        public bool CanUse(int phase, float distance) => IsReady && Data.IsAllowed(phase, distance);

        public void Begin(BossContext ctx)
        {
            InWarning = true;
            warningRemaining = Data.warningSeconds;
            ctx.Stop();
            OnWarningStart(ctx);
        }

        /// <summary>dt만큼 진행. 끝났으면 true.</summary>
        public bool Tick(BossContext ctx, float dt)
        {
            if (InWarning)
            {
                ctx.Stop();
                warningRemaining -= dt;
                float progress = Data.warningSeconds > 0f ? Mathf.Clamp01(1f - warningRemaining / Data.warningSeconds) : 1f;
                OnWarningTick(ctx, progress);
                if (warningRemaining > 0f)
                    return false;

                InWarning = false;
                ctx.Telegraph.Hide();
                ctx.Art.ResetPose();
                OnExecuteStart(ctx);
                dt = 0f; // 실행은 다음 스텝부터 시간을 쓴다
            }

            if (!OnExecuteTick(ctx, dt))
                return false;
            Finish(ctx);
            return true;
        }

        /// <summary>즉시 중단 (보스 처치, 8-C 연출).</summary>
        public void Cancel(BossContext ctx)
        {
            InWarning = false;
            OnCancel(ctx);
            Finish(ctx);
        }

        void Finish(BossContext ctx)
        {
            ctx.Stop();
            ctx.Telegraph.Hide();
            ctx.Art.ResetPose();
        }

        protected abstract void OnWarningStart(BossContext ctx);
        protected virtual void OnWarningTick(BossContext ctx, float progress) { }
        protected abstract void OnExecuteStart(BossContext ctx);
        /// <summary>실행 중. 끝났으면 true (즉발형은 바로 true).</summary>
        protected virtual bool OnExecuteTick(BossContext ctx, float dt) => true;
        protected virtual void OnCancel(BossContext ctx) { }
    }

    /// <summary>패턴 하위 클래스가 데이터 타입을 캐스팅하지 않게.</summary>
    public abstract class BossPattern<TData> : BossPattern where TData : BossPatternData
    {
        protected BossPattern(TData data) : base(data) { }
        protected new TData Data => (TData)base.Data;
    }
}
