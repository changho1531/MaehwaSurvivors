using Game.Player;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>[플레이어 HP] 데미지·피격 후 무적 시간·사망.</summary>
    public class HealthTests
    {
        [Test]
        public void StartsAtMax_DamageReduces()
        {
            var hp = new Health(100f, 0.5f);
            Assert.AreEqual(100f, hp.Current);
            Assert.IsTrue(hp.TryDamage(10f, 0f));
            Assert.AreEqual(90f, hp.Current);
        }

        [Test]
        public void Invulnerable_AfterHit_UntilTimePasses()
        {
            var hp = new Health(100f, 0.5f);
            hp.TryDamage(10f, 1f);
            Assert.IsTrue(hp.IsInvulnerable(1.2f));
            Assert.IsFalse(hp.TryDamage(10f, 1.4f), "무적 시간 중 무시");
            Assert.AreEqual(90f, hp.Current);
            Assert.IsTrue(hp.TryDamage(10f, 1.5f), "무적 종료 후 다시 맞음");
            Assert.AreEqual(80f, hp.Current);
        }

        [Test]
        public void ReachesZero_IsDead_IgnoresFurtherDamage()
        {
            var hp = new Health(15f, 0f);
            hp.TryDamage(10f, 0f);
            Assert.IsFalse(hp.IsDead);
            hp.TryDamage(10f, 1f);
            Assert.AreEqual(0f, hp.Current, "0 아래로 내려가지 않음");
            Assert.IsTrue(hp.IsDead);
            Assert.IsFalse(hp.TryDamage(10f, 2f), "사망 후 데미지 무시");
        }

        [Test]
        public void NonPositiveDamage_IsIgnored_AndDoesNotStartInvulnerability()
        {
            var hp = new Health(100f, 0.5f);
            Assert.IsFalse(hp.TryDamage(0f, 0f));
            Assert.IsFalse(hp.TryDamage(-5f, 0f));
            Assert.IsFalse(hp.IsInvulnerable(0.1f));
            Assert.AreEqual(100f, hp.Current);
        }
    }
}
