using Game.Visuals;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>돌진 (8-C): 떨어져 있을 때 붉은 선을 긋고 직선으로 달린다. 끝나면 숨 고르기(반격 시간). ※ 수치 예시</summary>
    [CreateAssetMenu(menuName = "Game/Boss Pattern/Charge", fileName = "BossPattern_Charge")]
    public class ChargePatternData : BossPatternData
    {
        [Min(0.1f)] public float distance = 8f;
        [Min(0.1f)] public float speed = 12f;
        [Tooltip("끝나고 멈춰 있는 시간")]
        [Min(0f)] public float restSeconds = 0.5f;
        [Tooltip("몸 중심과 플레이어 사이 이 거리 안이면 명중 (보스 몸 크기)")]
        [Min(0.1f)] public float hitRadius = 1f;
        [Tooltip("경고 선 굵기")]
        [Min(0.1f)] public float lineWidth = 1.4f;
        [Min(0f)] public float shakeAmplitude = 0.06f;
        [Min(0f)] public float shakeFrequency = 25f;
        [Tooltip("돌진 불꽃 꼬리 높이(발 기준, 칸) — 몸 가운데에서 뒤로 뻗게 ※")]
        public float effectHeight = 0.9f;

        void Reset()
        {
            displayName = "돌진";
            minDistance = 4f;
            cooldownPerPhase = new[] { 6f, 4.2f };
            warningSeconds = 0.8f;
            damage = 20f;
        }

        public override BossPattern CreateRuntime() => new ChargePattern(this);
    }

    public class ChargePattern : BossPattern<ChargePatternData>
    {
        Vector2 direction;
        float traveled;
        float rested;
        bool hit;
        BurstEffect trail;

        public ChargePattern(ChargePatternData data) : base(data) { }

        public Vector2 LockedDirection => direction;
        public bool HasHit => hit;
        public bool IsResting => traveled >= Data.distance;

        protected override void OnWarningStart(BossContext ctx)
        {
            direction = ctx.DirectionToPlayer; // 경고 순간 고정 — 플레이어가 피해도 그대로 끝까지 (예외)
            ctx.Telegraph.ShowLine(direction, Data.distance, Data.lineWidth);
        }

        protected override void OnWarningTick(BossContext ctx, float progress) =>
            ctx.Art.Shake(Data.shakeAmplitude, Data.shakeFrequency, Time.fixedDeltaTime);

        protected override void OnExecuteStart(BossContext ctx)
        {
            traveled = 0f;
            rested = 0f;
            hit = false;
            ctx.PassThroughPlayer(true);
            ctx.Body.linearVelocity = direction * Data.speed;
            trail = ctx.PlayEffect(Data.effectPrefab, TrailPosition(ctx), direction, Data.hitRadius);
        }

        Vector2 TrailPosition(BossContext ctx) => ctx.Position + Vector2.up * Data.effectHeight;

        protected override void OnCancel(BossContext ctx) => ctx.PassThroughPlayer(false);

        protected override bool OnExecuteTick(BossContext ctx, float dt)
        {
            if (traveled < Data.distance)
            {
                traveled += Data.speed * dt;
                bool moving = traveled < Data.distance;
                ctx.Body.linearVelocity = moving ? direction * Data.speed : Vector2.zero;
                if (!moving)
                    ctx.PassThroughPlayer(false);
                else if (trail != null && trail.isActiveAndEnabled)
                    trail.transform.position = TrailPosition(ctx); // 풀 오브젝트라 보스 자식으로 두지 않고 따라가게만
                if (!hit && ctx.Player != null && ctx.Distance <= Data.hitRadius)
                {
                    hit = true; // 몸에 닿으면 1회
                    ctx.HitPlayer(Data.damage);
                }
                return false;
            }

            ctx.Stop(); // 숨 고르기
            rested += dt;
            return rested >= Data.restSeconds;
        }
    }
}
