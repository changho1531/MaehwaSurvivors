using Game.Enemies;
using NUnit.Framework;

namespace Game.Tests
{
    public class EnemyArtMotionTests
    {
        [Test]
        public void Bob_StartsOnGround_AndStaysWithinHeight()
        {
            Assert.AreEqual(0f, EnemyArtMotion.BobOffset(0f, 0.06f, 4f), 1e-5f, "걷기 시작 순간은 튀지 않음");
            Assert.AreEqual(0.06f, EnemyArtMotion.BobOffset(0.125f, 0.06f, 4f), 1e-5f, "초당 4번이면 1/8초에 꼭대기");
            for (float t = 0f; t < 2f; t += 0.01f)
            {
                float y = EnemyArtMotion.BobOffset(t, 0.06f, 4f);
                Assert.That(y, Is.InRange(0f, 0.06f + 1e-5f), "땅 아래로 내려가지 않음");
            }
        }

        [TestCase(1f, true, false)]
        [TestCase(-1f, true, true)]
        [TestCase(1f, false, true)]
        [TestCase(-1f, false, false)]
        public void Flip_FollowsMoveDirection(float vx, bool artFacesRight, bool expectFlip)
        {
            Assert.AreEqual(expectFlip, EnemyArtMotion.ShouldFlip(vx, artFacesRight));
        }
    }
}
