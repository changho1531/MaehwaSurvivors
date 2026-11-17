using System.Collections.Generic;
using Game.Enemies.Boss;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[보스 8-C절] 패턴 사용 조건(거리 · 페이즈), 고르기 규칙(직전 패턴 제외 · 쿨타임), 데이터 에셋 수치.</summary>
    public class BossPatternTests
    {
        const string Folder = "Assets/Data/Enemies/Boss/";
        readonly List<BossPattern> buffer = new();
        readonly List<Object> created = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in created)
                Object.DestroyImmediate(o);
            created.Clear();
        }

        T Make<T>(string name, int minPhase, float minDistance, float maxDistance) where T : BossPatternData
        {
            var data = ScriptableObject.CreateInstance<T>();
            data.displayName = name;
            data.minPhase = minPhase;
            data.minDistance = minDistance;
            data.maxDistance = maxDistance;
            data.cooldownPerPhase = new[] { 3f, 2f };
            created.Add(data);
            return data;
        }

        [Test]
        public void IsAllowed_DistanceAndPhase()
        {
            var swing = Make<SwingPatternData>("휘두르기", 1, 0f, 2.5f);
            var charge = Make<ChargePatternData>("돌진", 1, 4f, 999f);
            var slam = Make<GroundSlamPatternData>("지면 강타", 2, 0f, 4f);

            Assert.IsTrue(swing.IsAllowed(1, 2.5f));
            Assert.IsFalse(swing.IsAllowed(1, 2.6f), "2.5칸 이하에서만 휘두르기");
            Assert.IsFalse(charge.IsAllowed(1, 3.9f));
            Assert.IsTrue(charge.IsAllowed(2, 4f), "4칸 이상");
            Assert.IsFalse(slam.IsAllowed(1, 1f), "지면 강타는 2페이즈에서만");
            Assert.IsTrue(slam.IsAllowed(2, 1f));
        }

        [Test]
        public void Choose_ExcludesLast_UnlessOnlyCandidate()
        {
            var a = Make<ChargePatternData>("돌진", 1, 4f, 999f).CreateRuntime();
            var b = Make<BloodOrbPatternData>("혈기탄", 1, 4f, 999f).CreateRuntime();
            var near = Make<SwingPatternData>("휘두르기", 1, 0f, 2.5f).CreateRuntime();
            var list = new List<BossPattern> { a, b, near };

            for (int i = 0; i < 10; i++)
                Assert.AreSame(b, BossPatternSelector.Choose(list, 1, 6f, a, i / 10f, buffer), "직전 패턴(돌진) 제외");

            b.StartCooldown(1);
            Assert.AreSame(a, BossPatternSelector.Choose(list, 1, 6f, a, 0.5f, buffer), "직전 패턴만 남으면 허용");

            a.StartCooldown(1);
            Assert.IsNull(BossPatternSelector.Choose(list, 1, 6f, null, 0.5f, buffer), "후보 없음 → null (추적하며 다시 확인)");
            Assert.IsNull(BossPatternSelector.Choose(list, 1, 3f, null, 0.5f, buffer), "중간 거리(2.5~4)는 후보 없음");
        }

        [Test]
        public void Cooldown_PerPhase_CountsDown()
        {
            var p = Make<SwingPatternData>("휘두르기", 1, 0f, 2.5f).CreateRuntime();
            p.StartCooldown(1);
            Assert.AreEqual(3f, p.CooldownRemaining, 0.0001f);
            Assert.IsFalse(p.CanUse(1, 1f));
            p.TickCooldown(3f);
            Assert.IsTrue(p.CanUse(1, 1f));
            p.StartCooldown(2);
            Assert.AreEqual(2f, p.CooldownRemaining, 0.0001f, "2페이즈 쿨타임");
        }

        [Test]
        public void Assets_MatchSpecTable()
        {
            var boss = AssetDatabase.LoadAssetAtPath<BossEnemyData>(Folder + "EnemyData_Boss.asset");
            Assert.IsNotNull(boss);
            Assert.AreEqual("혈각마존", boss.displayName);
            Assert.AreEqual(1.8f, boss.moveSpeed, 0.001f);
            Assert.AreEqual(15f, boss.contactDps);
            Assert.IsTrue(boss.knockbackImmune);
            Assert.AreEqual(0.5f, boss.phase2HpRatio);
            Assert.AreEqual(0.8f, boss.GetGap(1), 0.001f);
            Assert.AreEqual(0.6f, boss.GetGap(2), 0.001f);
            Assert.AreEqual(5, boss.patterns.Length);
            Assert.IsInstanceOf<GroundSlamPatternData>(boss.phase2OpeningPattern);

            var swing = AssetDatabase.LoadAssetAtPath<SwingPatternData>(Folder + "BossPattern_Swing.asset");
            Assert.AreEqual(2.5f, swing.maxDistance);
            Assert.AreEqual(15f, swing.damage);
            Assert.AreEqual(3f, swing.GetCooldown(1), 0.001f);
            Assert.AreEqual(2.1f, swing.GetCooldown(2), 0.001f);

            var charge = AssetDatabase.LoadAssetAtPath<ChargePatternData>(Folder + "BossPattern_Charge.asset");
            Assert.AreEqual(4f, charge.minDistance);
            Assert.AreEqual(8f, charge.distance);
            Assert.AreEqual(12f, charge.speed);
            Assert.AreEqual(0.5f, charge.restSeconds);
            Assert.AreEqual(20f, charge.damage);

            var orb = AssetDatabase.LoadAssetAtPath<BloodOrbPatternData>(Folder + "BossPattern_BloodOrb.asset");
            Assert.AreEqual(5, orb.count);
            Assert.AreEqual(60f, orb.spreadAngle);
            Assert.AreEqual(4.5f, orb.speed);
            Assert.IsNotNull(orb.orbPrefab);

            var slam = AssetDatabase.LoadAssetAtPath<GroundSlamPatternData>(Folder + "BossPattern_GroundSlam.asset");
            Assert.AreEqual(2, slam.minPhase);
            Assert.AreEqual(6f, slam.maxRadius);
            Assert.AreEqual(7f, slam.GetCooldown(2), 0.001f);
            Assert.IsNotNull(slam.ringPrefab);

            var whirl = AssetDatabase.LoadAssetAtPath<WhirlwindPatternData>(Folder + "BossPattern_Whirlwind.asset");
            Assert.AreEqual(2, whirl.minPhase);
            Assert.AreEqual(2f, whirl.spinSeconds);
            Assert.AreEqual(1f, whirl.dizzySeconds);
            Assert.AreEqual(10f, whirl.GetCooldown(2), 0.001f);
        }
    }
}
