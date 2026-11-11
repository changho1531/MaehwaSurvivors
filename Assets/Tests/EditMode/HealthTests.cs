using Game.Player;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>[7-B절] 플레이어 HP: 무적 시간 없음, 0에서 멈춤, 사망 판정 1회.</summary>
    public class HealthTests
    {
        [Test]
        public void StartsAtMax_DamageReduces()
        {
            var hp = new Health(100f);
            Assert.AreEqual(100f, hp.Current);
            Assert.IsTrue(hp.TryDamage(10f, out _));
            Assert.AreEqual(90f, hp.Current);
        }

        [Test]
        public void NoInvulnerability_ConsecutiveHitsAllApply()
        {
            var hp = new Health(100f);
            for (int i = 0; i < 5; i++)
                Assert.IsTrue(hp.TryDamage(3f, out _), "맞은 직후에도 다음 데미지가 그대로 들어간다");
            Assert.AreEqual(85f, hp.Current, 0.0001f);
        }

        [Test]
        public void ReachesZero_JustDiedOnlyOnce_IgnoresFurtherDamage()
        {
            var hp = new Health(15f);
            hp.TryDamage(10f, out bool died1);
            Assert.IsFalse(died1);
            hp.TryDamage(10f, out bool died2);
            Assert.IsTrue(died2, "처음 0이 되는 순간");
            Assert.AreEqual(0f, hp.Current, "0 아래로 내려가지 않음");
            Assert.IsTrue(hp.IsDead);
            Assert.IsFalse(hp.TryDamage(10f, out bool died3), "사망 후 데미지 무시");
            Assert.IsFalse(died3, "같은 프레임 여러 적의 마지막 데미지에도 사망 처리는 1회");
        }

        [Test]
        public void NonPositiveDamage_IsIgnored()
        {
            var hp = new Health(100f);
            Assert.IsFalse(hp.TryDamage(0f, out _));
            Assert.IsFalse(hp.TryDamage(-5f, out _));
            Assert.AreEqual(100f, hp.Current);
        }

        [Test]
        public void Heal_CapsAtMax_ReturnsActualAmount()
        {
            var hp = new Health(100f);
            hp.TryDamage(30f, out _);
            Assert.AreEqual(10f, hp.Heal(10f), 0.0001f);
            Assert.AreEqual(80f, hp.Current, 0.0001f);
            Assert.AreEqual(20f, hp.Heal(50f), 0.0001f, "최대 HP를 넘지 않는다");
            Assert.AreEqual(100f, hp.Current);
            Assert.AreEqual(0f, hp.Heal(10f), "가득이면 0");
        }

        [Test]
        public void Heal_DoesNotReviveDead()
        {
            var hp = new Health(10f);
            hp.TryDamage(10f, out _);
            Assert.AreEqual(0f, hp.Heal(5f));
            Assert.IsTrue(hp.IsDead);
        }
    }
}
