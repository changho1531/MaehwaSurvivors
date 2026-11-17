using Game.Weapons;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>휘두르기 (8-C): 붙어 있을 때 앞쪽 반원을 한 번 벤다. ※ 수치 예시</summary>
    [CreateAssetMenu(menuName = "Game/Boss Pattern/Swing", fileName = "BossPattern_Swing")]
    public class SwingPatternData : BossPatternData
    {
        [Min(0.1f)] public float radius = 2.5f;
        [Tooltip("벤 자리를 잠깐 진하게 보여 주는 시간")]
        [Min(0f)] public float strikeShowSeconds = 0.15f;

        void Reset()
        {
            displayName = "휘두르기";
            maxDistance = 2.5f;
            cooldownPerPhase = new[] { 3f, 2.1f };
            warningSeconds = 0.6f;
            damage = 15f;
        }

        public override BossPattern CreateRuntime() => new SwingPattern(this);
    }

    public class SwingPattern : BossPattern<SwingPatternData>
    {
        Vector2 direction;
        float shown;

        public SwingPattern(SwingPatternData data) : base(data) { }

        /// <summary>마지막으로 휘두른 결과 (테스트용)</summary>
        public bool LastHit { get; private set; }
        public Vector2 LockedDirection => direction;

        protected override void OnWarningStart(BossContext ctx)
        {
            direction = ctx.DirectionToPlayer; // 경고 순간 고정 (규칙 6)
            ctx.Telegraph.ShowFan(direction, Data.radius);
        }

        protected override void OnExecuteStart(BossContext ctx)
        {
            shown = 0f;
            LastHit = ctx.Player != null && SweepWeapon.IsInFan(ctx.Position, direction, Data.radius, 180f, ctx.Player.position);
            if (LastHit)
                ctx.HitPlayer(Data.damage);
            ctx.Telegraph.ShowFan(direction, Data.radius, strong: true);
        }

        protected override bool OnExecuteTick(BossContext ctx, float dt)
        {
            shown += dt;
            return shown >= Data.strikeShowSeconds;
        }
    }
}
