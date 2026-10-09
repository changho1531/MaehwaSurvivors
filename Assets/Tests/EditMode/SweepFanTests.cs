using Game.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[화산십이검 7절] 부채꼴 판정: 반경·각도, 대각선 방향.</summary>
    public class SweepFanTests
    {
        const float Radius = 3f;
        const float Angle = 120f;

        static bool In(Vector2 dir, Vector2 point) => SweepWeapon.IsInFan(Vector2.zero, dir, Radius, Angle, point);

        [Test]
        public void InFront_WithinRadius_IsHit()
        {
            Assert.IsTrue(In(Vector2.up, new Vector2(0f, 2f)));
            Assert.IsTrue(In(Vector2.up, new Vector2(0f, 3f)), "경계(반경)는 포함");
        }

        [Test]
        public void BeyondRadius_OrBehind_IsNotHit()
        {
            Assert.IsFalse(In(Vector2.up, new Vector2(0f, 3.1f)), "반경 밖");
            Assert.IsFalse(In(Vector2.up, new Vector2(0f, -1f)), "등 뒤");
        }

        [Test]
        public void HalfAngle_IsTheBoundary()
        {
            // 위쪽 기준 ±60°가 경계
            var inside = Quaternion.Euler(0, 0, 55f) * Vector2.up * 2f;
            var outside = Quaternion.Euler(0, 0, 65f) * Vector2.up * 2f;
            Assert.IsTrue(In(Vector2.up, inside));
            Assert.IsFalse(In(Vector2.up, outside));
        }

        [Test]
        public void DiagonalFacing_RotatesTheFan()
        {
            var diag = new Vector2(1f, 1f).normalized;
            Assert.IsTrue(In(diag, new Vector2(1.5f, 1.5f)), "대각선 방향 그대로 사용");
            Assert.IsTrue(In(diag, new Vector2(2f, 0f)), "45° 차이는 반각(60°) 안");
            Assert.IsFalse(In(diag, new Vector2(-1f, 0f)));
        }

        [Test]
        public void OverlappingPlayer_IsHit()
        {
            Assert.IsTrue(In(Vector2.up, Vector2.zero));
        }
    }
}
