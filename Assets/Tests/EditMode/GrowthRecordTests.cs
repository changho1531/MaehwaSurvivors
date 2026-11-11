using Game.Progression;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>[7-I절 측정 기록] 레벨 도달 시각을 순서대로, 경험치를 30초 구간별로 모은다.</summary>
    public class GrowthRecordTests
    {
        [Test]
        public void Levels_KeptInOrder()
        {
            var record = new GrowthRecord();
            record.RecordLevel(2, 20f);
            record.RecordLevel(3, 41f);
            Assert.AreEqual(2, record.Levels.Count);
            Assert.AreEqual((2, 20f), record.Levels[0]);
            Assert.AreEqual((3, 41f), record.Levels[1]);
        }

        [Test]
        public void Xp_GroupedBy30Seconds_GapsAreZero()
        {
            var record = new GrowthRecord(30f);
            record.AddXp(5, 0f);
            record.AddXp(5, 29.9f);
            record.AddXp(7, 95f); // 3번째 구간(90~120)
            record.AddXp(0, 40f); // 무시
            CollectionAssert.AreEqual(new[] { 10, 0, 0, 7 }, record.XpPerBucket);
        }

        [Test]
        public void Format_ContainsEveryLevelLine()
        {
            var record = new GrowthRecord();
            record.RecordLevel(2, 20f);
            record.AddXp(25, 20f);
            string text = record.Format("GameOver", 30f);
            StringAssert.Contains("Lv2\t20.0", text);
            StringAssert.Contains("GameOver", text);
        }
    }
}
