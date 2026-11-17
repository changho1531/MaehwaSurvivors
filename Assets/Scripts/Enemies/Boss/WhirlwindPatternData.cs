using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 혈풍선 (8-C, 2페이즈): 회전하며 플레이어 쪽으로 밀고 들어온다. 주변 원 안은 계속 데미지, 끝나면 어지러움(큰 반격 기회).
    /// ※ 수치 예시
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss Pattern/Whirlwind", fileName = "BossPattern_Whirlwind")]
    public class WhirlwindPatternData : BossPatternData
    {
        [Min(0.1f)] public float spinSeconds = 2f;
        [Min(0f)] public float moveSpeed = 3f;
        [Min(0.1f)] public float radius = 1.5f;
        [Tooltip("끝나고 멈춰 있는 시간")]
        [Min(0f)] public float dizzySeconds = 1f;
        [Min(0f)] public float spinDegreesPerSecond = 900f;

        void Reset()
        {
            displayName = "혈풍선";
            minPhase = 2;
            cooldownPerPhase = new[] { 10f };
            warningSeconds = 0.8f;
            damage = 20f; // 초당
        }

        public override BossPattern CreateRuntime() => new WhirlwindPattern(this);
    }

    public class WhirlwindPattern : BossPattern<WhirlwindPatternData>
    {
        float elapsed;

        public WhirlwindPattern(WhirlwindPatternData data) : base(data) { }

        public bool IsSpinning => !InWarning && elapsed < Data.spinSeconds;
        public bool IsDizzy => !InWarning && elapsed >= Data.spinSeconds;

        protected override void OnWarningStart(BossContext ctx) => elapsed = 0f;

        protected override void OnWarningTick(BossContext ctx, float progress)
        {
            ctx.Art.Crouch(progress); // 웅크림 + 붉은 기운 소용돌이
            ctx.Art.Glow(progress);
        }

        protected override void OnExecuteStart(BossContext ctx)
        {
            elapsed = 0f;
            ctx.Telegraph.ShowDisk(Data.radius); // 도는 동안 위험 범위
        }

        protected override bool OnExecuteTick(BossContext ctx, float dt)
        {
            elapsed += dt;
            if (elapsed < Data.spinSeconds)
            {
                ctx.Body.linearVelocity = ctx.DirectionToPlayer * Data.moveSpeed;
                ctx.Art.Spin(elapsed * Data.spinDegreesPerSecond);
                ctx.Art.Glow(1f);
                if (ctx.Player != null && ctx.Distance <= Data.radius)
                    ctx.HitPlayer(Data.damage * dt); // 초당 데미지
                return false;
            }

            // 어지러움: 멈춘다
            ctx.Stop();
            ctx.Telegraph.Hide();
            ctx.Art.ResetPose();
            return elapsed >= Data.spinSeconds + Data.dizzySeconds;
        }
    }
}
