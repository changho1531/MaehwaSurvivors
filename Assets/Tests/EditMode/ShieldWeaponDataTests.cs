using System.Collections.Generic;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[자하호신강 7-G절] 감소율·지속, 최대 레벨 회복량, 카드 문구.</summary>
    public class ShieldWeaponDataTests
    {
        ShieldWeaponData data;

        [SetUp]
        public void Create()
        {
            data = ScriptableObject.CreateInstance<ShieldWeaponData>();
            data.maxLevel = 3;
            data.damagePerLevel = new[] { 0f };
            data.reductionPerLevel = new[] { 0.2f, 0.25f, 0.3f };
            data.durationPerLevel = new[] { 3f, 3.5f, 4f };
            data.cooldownPerLevel = new[] { 7f, 6f, 5f };
            data.maxLevelHealRatio = 0.2f;
            data.maxLevelEffect = "최대 레벨: 막이 꺼질 때 잃은 체력의 20% 회복";
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(data);

        [Test]
        public void Lv1_Cycle_IsDurationPlusCooldown_10Seconds()
        {
            Assert.AreEqual(0.2f, data.GetReduction(1), 0.0001f);
            Assert.AreEqual(10f, data.GetDuration(1) + data.GetCooldown(1), 0.0001f);
        }

        [Test]
        public void HealAmount_IsLostHpTimesRatio_OnlyAtMaxLevel()
        {
            Assert.AreEqual(14f, data.GetHealAmount(3, 30f, 100f), 0.0001f, "잃은 체력 70 × 20%");
            Assert.AreEqual(0f, data.GetHealAmount(3, 100f, 100f), "체력 가득이면 0");
            Assert.AreEqual(0f, data.GetHealAmount(2, 30f, 100f), "최대 레벨 전에는 회복 없음");
        }

        [Test]
        public void UpgradeCard_ShowsReductionDurationCooldown_AndMaxLevelLine()
        {
            var lines = new List<string>();
            data.DescribeUpgrade(1, lines);
            CollectionAssert.AreEqual(new[] { "받는 피해 -5%p", "지속 +0.5초", "쿨타임 -1초" }, lines);

            data.DescribeUpgrade(2, lines);
            CollectionAssert.AreEqual(new[] { "받는 피해 -5%p", "지속 +0.5초", "쿨타임 -1초", data.maxLevelEffect }, lines);
        }

        [Test]
        public void ZeroCooldown_HasFloor()
        {
            data.cooldownPerLevel = new[] { 0f };
            Assert.Greater(data.GetCooldown(1), 0f);
        }
    }
}
