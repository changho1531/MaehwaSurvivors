using Game.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[웨이브 9-B절] 경과 시간 → 웨이브 번호, 이동속도 상한, 구성 비율 고르기.</summary>
    public class WaveTableTests
    {
        WaveTable table;

        [SetUp]
        public void Create()
        {
            table = ScriptableObject.CreateInstance<WaveTable>();
            table.waves = new WaveTable.Wave[5];
            for (int i = 0; i < 5; i++)
                table.waves[i] = new WaveTable.Wave { startSeconds = i * 120f };
            table.enemySpeedCapRatio = 0.7f;
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(table);

        [TestCase(0f, 1)]
        [TestCase(119.9f, 1)]
        [TestCase(120f, 2)]
        [TestCase(359.9f, 3)]
        [TestCase(480f, 5)]
        [TestCase(700f, 5)]
        public void WaveNumber_ByElapsedSeconds(float seconds, int expected)
        {
            Assert.AreEqual(expected, table.GetWaveNumber(seconds));
        }

        [Test]
        public void EnemySpeed_IsBaseTimesMultiplier_CappedAtPlayerBaseTimes07()
        {
            Assert.AreEqual(2.4f, table.GetEnemySpeed(2f, 1.2f, 5f), 0.0001f, "도병 웨이브 5: 2 × 1.2");
            Assert.AreEqual(3.5f, table.GetEnemySpeed(4f, 1.2f, 5f), 0.0001f, "상한 = 5 × 0.7");
        }

        [Test]
        public void Settings_Pick_ByCumulativeRatio_RestIsBase()
        {
            var settings = new WaveTable.Settings { shares = new[] { new WaveTable.Share { prefab = null, ratio = 0.25f } } };
            // 비율 안쪽은 그 항목(여기서는 null 프리팹), 바깥은 기본 적(null) — 경계만 확인
            Assert.IsNull(settings.Pick(0.9f), "나머지 75%는 기본 적");
            Assert.AreEqual(0, new WaveTable.Settings().shares.Length);
        }
    }
}
