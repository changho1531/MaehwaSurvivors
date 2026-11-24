using Game.Pooling;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 지면 강타 (8-C, 2페이즈): 웅크렸다가 내려찍어 충격파 고리를 퍼뜨린다. 고리가 지나가는 순간에만 맞으므로
    /// 고리보다 빨리 달아나야 한다. 고리가 다 퍼질 때까지 보스는 그 자리에 서 있다 ※. ※ 수치 예시
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss Pattern/Ground Slam", fileName = "BossPattern_GroundSlam")]
    public class GroundSlamPatternData : BossPatternData
    {
        [Min(0.1f)] public float maxRadius = 6f;
        [Min(0.1f)] public float expandSeconds = 1f;
        [Min(0.05f)] public float thickness = 0.5f;
        [Tooltip("경고: 발밑 붉은 원 반경")]
        [Min(0.1f)] public float warningRadius = 1.5f;
        public ShockwaveRing ringPrefab;

        void Reset()
        {
            displayName = "지면 강타";
            minPhase = 2;
            maxDistance = 4f;
            cooldownPerPhase = new[] { 7f };
            warningSeconds = 1f;
            damage = 15f;
        }

        public override BossPattern CreateRuntime() => new GroundSlamPattern(this);
    }

    public class GroundSlamPattern : BossPattern<GroundSlamPatternData>
    {
        float elapsed;

        public GroundSlamPattern(GroundSlamPatternData data) : base(data) { }

        public ShockwaveRing LastRing { get; private set; }

        protected override void OnWarningStart(BossContext ctx) => ctx.Telegraph.ShowDisk(Data.warningRadius);

        protected override void OnWarningTick(BossContext ctx, float progress) => ctx.Art.Crouch(progress);

        protected override void OnExecuteStart(BossContext ctx)
        {
            elapsed = 0f;
            LastRing = null;
            ctx.PlayEffect(Data.effectPrefab, ctx.Position, Vector2.right, Data.warningRadius); // 내려찍는 그림의 땅 원 = 경고 원
            if (Data.ringPrefab == null)
                return;
            var pool = PrefabPools.For(Data.ringPrefab, 2);
            LastRing = pool.Get(ctx.Position);
            LastRing.Begin(Data.maxRadius, Data.expandSeconds, Data.thickness, Data.damage, ctx.PlayerHealth, pool.Release);
        }

        protected override bool OnExecuteTick(BossContext ctx, float dt)
        {
            elapsed += dt;
            return elapsed >= Data.expandSeconds;
        }
    }
}
