using System.Collections.Generic;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[매화보 7-H절] 이동속도 배율(합연산), 카드 문구.</summary>
    public class MovementPassiveDataTests
    {
        MovementPassiveData data;

        [SetUp]
        public void Create()
        {
            data = ScriptableObject.CreateInstance<MovementPassiveData>();
            data.maxLevel = 3;
            data.speedBonusPerLevel = new[] { 0.05f, 0.1f, 0.15f };
            data.boostBonus = 0.1f;
            data.maxLevelEffect = "최대 레벨: 3초 동안 안 맞으면 이동속도 +10%";
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(data);

        [Test]
        public void Multiplier_PerLevel_AndBoostAddsUp()
        {
            Assert.AreEqual(1.05f, data.GetSpeedMultiplier(1, false), 0.0001f);
            Assert.AreEqual(1.10f, data.GetSpeedMultiplier(2, false), 0.0001f);
            Assert.AreEqual(1.15f, data.GetSpeedMultiplier(3, false), 0.0001f);
            Assert.AreEqual(1.25f, data.GetSpeedMultiplier(3, true), 0.0001f, "합연산 최대 1.25배");
        }

        [Test]
        public void UpgradeCard_ShowsSpeedPercentPoint_AndMaxLevelLine()
        {
            var lines = new List<string>();
            data.DescribeUpgrade(1, lines);
            CollectionAssert.AreEqual(new[] { "이동속도 +5%p" }, lines);

            data.DescribeUpgrade(2, lines);
            CollectionAssert.AreEqual(new[] { "이동속도 +5%p", data.maxLevelEffect }, lines);
        }
    }
}
