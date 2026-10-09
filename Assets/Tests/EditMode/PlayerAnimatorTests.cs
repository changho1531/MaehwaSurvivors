using Game.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[7-A절] 이동 여부 + 바라보는 방향 → 재생 클립·좌우 반전.</summary>
    public class PlayerAnimatorTests
    {
        [Test]
        public void Stopped_IsIdle_RegardlessOfFacing()
        {
            foreach (var dir in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right, new Vector2(1, 1).normalized })
            {
                var (state, flip) = PlayerAnimator.Resolve(false, dir);
                Assert.AreEqual(PlayerAnimator.IdleState, state, $"정지 시 방향 {dir}");
                Assert.IsFalse(flip, "정지(정면)는 반전 없음");
            }
        }

        [Test]
        public void Moving_FourDirections()
        {
            Assert.AreEqual((PlayerAnimator.WalkBackState, false), PlayerAnimator.Resolve(true, Vector2.up), "위 = 뒷모습");
            Assert.AreEqual((PlayerAnimator.WalkFrontState, false), PlayerAnimator.Resolve(true, Vector2.down), "아래 = 앞모습");
            Assert.AreEqual((PlayerAnimator.WalkRightState, false), PlayerAnimator.Resolve(true, Vector2.right));
            Assert.AreEqual((PlayerAnimator.WalkRightState, true), PlayerAnimator.Resolve(true, Vector2.left), "왼쪽 = 오른쪽 반전");
        }

        [Test]
        public void Diagonal_UsesNearestFourWay()
        {
            Assert.AreEqual(PlayerSpriteDirection.Back, PlayerAnimator.ToFourWay(new Vector2(0.3f, 1f)), "위쪽에 가까움");
            Assert.AreEqual(PlayerSpriteDirection.Left, PlayerAnimator.ToFourWay(new Vector2(-1f, -0.4f)), "왼쪽에 가까움");
            Assert.AreEqual(PlayerSpriteDirection.Right, PlayerAnimator.ToFourWay(new Vector2(1f, 1f).normalized), "정확한 45°는 좌우 그림");
            Assert.AreEqual(PlayerSpriteDirection.Left, PlayerAnimator.ToFourWay(new Vector2(-1f, -1f).normalized));
        }
    }
}
