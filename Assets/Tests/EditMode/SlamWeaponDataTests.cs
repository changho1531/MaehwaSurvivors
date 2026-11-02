using System.Collections.Generic;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[낙매진각 7-E절] 넉백 목표 지점·속도 계산, 카드 문구.</summary>
    public class SlamWeaponDataTests
    {
        SlamWeaponData data;

        [SetUp]
        public void Create()
        {
            data = ScriptableObject.CreateInstance<SlamWeaponData>();
            data.maxLevel = 3;
            data.radiusPerLevel = new[] { 2f, 2.5f, 3f };
            data.cooldownPerLevel = new[] { 6f, 5f, 4f };
            data.damagePerLevel = new[] { 5f, 5f, 5f };
            data.maxLevelEffect = "최대 레벨: 내려찍은 자리에 꽃잎 장판";
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(data);

        [Test]
        public void KnockbackTarget_IsOnCircleEdge_Outward()
        {
            var target = SlamWeapon.KnockbackTarget(new Vector2(1f, 1f), new Vector2(1f, 2f), 3f, Vector2.right);
            Assert.That(Vector2.Distance(target, new Vector2(1f, 4f)), Is.LessThan(0.001f));
        }

        [Test]
        public void KnockbackTarget_OverlappingEnemy_UsesFallbackDirection()
        {
            var target = SlamWeapon.KnockbackTarget(Vector2.zero, Vector2.zero, 2f, Vector2.left);
            Assert.That(Vector2.Distance(target, new Vector2(-2f, 0f)), Is.LessThan(0.001f));
        }

        [Test]
        public void KnockbackSpeed_KeepsContactEnemyTimeSameAtEveryLevel()
        {
            Assert.AreEqual(6.625f, data.GetKnockbackSpeed(1), 0.001f);
            Assert.AreEqual(11.625f, data.GetKnockbackSpeed(3), 0.001f);
            for (int level = 1; level <= 3; level++)
                Assert.AreEqual(data.baseKnockbackSeconds, (data.GetRadius(level) - data.contactDistance) / data.GetKnockbackSpeed(level), 0.001f);
        }

        [Test]
        public void CardText_ComesFromTable()
        {
            var lines = new List<string>();
            data.DescribeUpgrade(1, lines);
            CollectionAssert.AreEqual(new[] { "범위 +0.5칸", "쿨타임 -1초" }, lines, "데미지는 같아서 생략");

            data.DescribeUpgrade(2, lines);
            CollectionAssert.AreEqual(new[] { "범위 +0.5칸", "쿨타임 -1초", "최대 레벨: 내려찍은 자리에 꽃잎 장판" }, lines);
        }
    }
}
