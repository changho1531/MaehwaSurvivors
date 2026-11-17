using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 혈기탄 (8-C): 떨어져 있을 때 플레이어 방향 부채꼴로 구체를 날린다. 플레이어보다 느려 움직이면 피할 수 있다.
    /// 구체는 궁수 화살(Arrow)을 그대로 쓴다 — 직선 비행 · 1회 명중 · 사거리 끝에서 사라짐이 같다. ※ 수치 예시
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss Pattern/Blood Orb", fileName = "BossPattern_BloodOrb")]
    public class BloodOrbPatternData : BossPatternData
    {
        [Min(1)] public int count = 5;
        [Tooltip("부채꼴 전체 각도")]
        [Range(0f, 360f)] public float spreadAngle = 60f;
        [Min(0.1f)] public float speed = 4.5f;
        [Min(0.1f)] public float range = 10f;
        public Arrow orbPrefab;

        void Reset()
        {
            displayName = "혈기탄";
            minDistance = 4f;
            cooldownPerPhase = new[] { 5f, 3.5f };
            warningSeconds = 0.6f;
            damage = 10f;
        }

        public override BossPattern CreateRuntime() => new BloodOrbPattern(this);
    }

    public class BloodOrbPattern : BossPattern<BloodOrbPatternData>
    {
        Vector2 direction;

        public BloodOrbPattern(BloodOrbPatternData data) : base(data) { }

        public Vector2 LockedDirection => direction;
        public int LastFired { get; private set; }

        protected override void OnWarningStart(BossContext ctx) => direction = ctx.DirectionToPlayer; // 경고 순간 고정

        protected override void OnWarningTick(BossContext ctx, float progress) => ctx.Art.Glow(progress); // 손에 붉은 기운

        protected override void OnExecuteStart(BossContext ctx)
        {
            LastFired = 0;
            if (Data.orbPrefab == null)
                return;
            var pool = Arrow.PoolFor(Data.orbPrefab);
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int i = 0; i < Data.count; i++)
            {
                // 부채꼴을 균등하게 나눈다 (1발이면 가운데)
                float t = Data.count > 1 ? (float)i / (Data.count - 1) - 0.5f : 0f;
                float angle = (baseAngle + t * Data.spreadAngle) * Mathf.Deg2Rad;
                var orb = pool.Get(ctx.Position);
                orb.Launch(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), Data.speed, Data.damage, Data.range, ctx.PlayerHealth, pool.Release);
                LastFired++;
            }
        }
    }
}
