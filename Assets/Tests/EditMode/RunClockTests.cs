using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>[HUD] 생존 시간 표기 mm:ss.</summary>
    public class RunClockTests
    {
        [TestCase(0f, "00:00")]
        [TestCase(5.9f, "00:05")]
        [TestCase(65f, "01:05")]
        [TestCase(599.99f, "09:59")]
        [TestCase(3725f, "62:05")]
        [TestCase(-3f, "00:00")]
        public void Format(float seconds, string expected)
        {
            Assert.AreEqual(expected, RunClock.Format(seconds));
        }
    }
}
