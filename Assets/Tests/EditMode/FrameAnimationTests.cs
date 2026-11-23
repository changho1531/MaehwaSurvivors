using Game.Visuals;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>프레임 순서: 1회 재생은 마지막에 멈춤, 반복은 loopFrom부터 (자하호신강 = 펼침 1회 + 유지 반복).</summary>
    public class FrameAnimationTests
    {
        [TestCase(0, false, 0, 0)]
        [TestCase(9, false, 0, 9)]
        [TestCase(25, false, 0, 9)]
        [TestCase(10, true, 0, 0)]
        [TestCase(13, true, 0, 3)]
        [TestCase(10, true, 4, 4)]
        [TestCase(15, true, 4, 9)]
        [TestCase(16, true, 4, 4)]
        public void FrameIndex(int elapsed, bool loop, int loopFrom, int expected)
        {
            Assert.AreEqual(expected, FrameAnimation.FrameIndex(elapsed, 10, loop, loopFrom));
        }
    }
}
