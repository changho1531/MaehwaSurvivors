using Game.Core;
using Game.Progression;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[XP·레벨] 경험치 계산, 레벨 곡선, 상태별 시간 배율 (설계서 6-A절).</summary>
    public class XpProgressTests
    {
        // Lv1→2: 10, Lv2→3: 20, Lv3→4: 30 ...
        static XpProgress Make() => new(level => level * 10);

        [Test]
        public void AddXp_BelowRequired_DoesNotLevel()
        {
            var p = Make();
            Assert.AreEqual(0, p.AddXp(9));
            Assert.AreEqual(1, p.Level);
            Assert.AreEqual(9, p.CurrentXp);
            Assert.AreEqual(0, p.PendingLevelUps);
        }

        [Test]
        public void AddXp_ReachingRequired_LevelsUp_AndCarriesOverflow()
        {
            var p = Make();
            Assert.AreEqual(1, p.AddXp(13));
            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(3, p.CurrentXp, "초과분 이월");
            Assert.AreEqual(20, p.RequiredXp);
            Assert.AreEqual(1, p.PendingLevelUps);
        }

        [Test]
        public void AddXp_Large_GainsSeveralLevels_AndQueuesThem()
        {
            var p = Make();
            // 10 + 20 + 30 = 60 → Lv4, 5 남음
            Assert.AreEqual(3, p.AddXp(65));
            Assert.AreEqual(4, p.Level);
            Assert.AreEqual(5, p.CurrentXp);
            Assert.AreEqual(3, p.PendingLevelUps);
        }

        [Test]
        public void ConsumePending_DecrementsUntilZero()
        {
            var p = Make();
            p.AddXp(30); // Lv3, 대기 2
            Assert.AreEqual(1, p.ConsumePending());
            Assert.AreEqual(0, p.ConsumePending());
            Assert.AreEqual(0, p.ConsumePending(), "0 아래로 내려가지 않음");
        }

        [Test]
        public void DiscardPending_ClearsQueue()
        {
            var p = Make();
            p.AddXp(30);
            p.DiscardPending();
            Assert.AreEqual(0, p.PendingLevelUps);
        }

        [Test]
        public void MarkMaxed_DiscardsPending_AndIgnoresFurtherXp()
        {
            var p = Make();
            p.AddXp(30);
            p.MarkMaxed();
            Assert.IsTrue(p.IsMaxed);
            Assert.AreEqual(0, p.PendingLevelUps);

            int level = p.Level, xp = p.CurrentXp;
            Assert.AreEqual(0, p.AddXp(1000));
            Assert.AreEqual(level, p.Level);
            Assert.AreEqual(xp, p.CurrentXp);
        }

        [Test]
        public void AddXp_ZeroOrNegative_IsIgnored()
        {
            var p = Make();
            p.AddXp(0);
            p.AddXp(-5);
            Assert.AreEqual(0, p.CurrentXp);
        }

        [Test]
        public void LevelCurve_UsesTable_AndClampsPastEnd()
        {
            var curve = ScriptableObject.CreateInstance<LevelCurve>();
            try
            {
                curve.xpToNextLevel = new[] { 5, 8, 12 };
                Assert.AreEqual(5, curve.GetRequiredXp(1));
                Assert.AreEqual(8, curve.GetRequiredXp(2));
                Assert.AreEqual(12, curve.GetRequiredXp(3));
                Assert.AreEqual(12, curve.GetRequiredXp(10), "표를 넘으면 마지막 값");

                curve.xpToNextLevel = new int[0];
                Assert.AreEqual(1, curve.GetRequiredXp(1), "빈 표여도 0으로 나누기/무한 레벨업이 없어야 한다");
            }
            finally
            {
                Object.DestroyImmediate(curve);
            }
        }

        [Test]
        public void LevelCurve_DefaultTable_IsIncreasing()
        {
            var curve = ScriptableObject.CreateInstance<LevelCurve>();
            try
            {
                for (int i = 1; i < curve.xpToNextLevel.Length; i++)
                    Assert.Greater(curve.xpToNextLevel[i], curve.xpToNextLevel[i - 1], "레벨이 오를수록 필요 XP 증가");
            }
            finally
            {
                Object.DestroyImmediate(curve);
            }
        }

        [Test]
        public void TimeScalePolicy_StopsTime_OnlyWhenGameIsSuspended()
        {
            Assert.AreEqual(1f, TimeScalePolicy.For(GameState.Play));
            Assert.AreEqual(1f, TimeScalePolicy.For(GameState.Title));
            Assert.AreEqual(0f, TimeScalePolicy.For(GameState.Pause));
            Assert.AreEqual(0f, TimeScalePolicy.For(GameState.LevelUp));
            Assert.AreEqual(0f, TimeScalePolicy.For(GameState.GameOver));
            Assert.AreEqual(0f, TimeScalePolicy.For(GameState.GameClear));
        }
    }
}
