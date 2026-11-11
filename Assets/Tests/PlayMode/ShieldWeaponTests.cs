using System.Collections;
using System.Collections.Generic;
using Game.Player;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [자하호신강 7-G절] 켜진 동안 받는 피해 감소(직접·접촉), 꺼진 뒤부터 쿨타임, 켜진 채 다시 켜지지 않음,
    /// 최대 레벨에서만 꺼질 때 회복, 체력 가득이면 회복 0, 일시정지 중 정지, 피격 연출, 카탈로그 등록.
    /// 주기를 짧게 하려고 카탈로그 에셋을 복제해 수치만 바꾼다 (에셋 원본은 건드리지 않는다).
    /// </summary>
    public class ShieldWeaponTests
    {
        readonly InGameScene scene = new();
        readonly List<(float time, bool started)> events = new();
        ShieldWeaponData asset;
        ShieldWeaponData data;
        ShieldWeapon shield;
        PlayerHealth health;

        [UnitySetUp]
        public IEnumerator Load()
        {
            events.Clear();
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            health = scene.Player.GetComponent<PlayerHealth>();

            asset = null;
            foreach (var weapon in scene.Inventory.Catalog.weapons)
                if (weapon is ShieldWeaponData shieldData)
                    asset = shieldData;
            Assert.IsNotNull(asset, "카탈로그에 자하호신강(ShieldWeaponData)이 없다");

            data = Object.Instantiate(asset);
            data.durationPerLevel = new[] { 0.6f, 0.6f, 0.6f };
            data.cooldownPerLevel = new[] { 0.5f, 0.5f, 0.5f };
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Object.Destroy(data);
            yield return null;
        }

        void Acquire(int level = 1)
        {
            Assert.IsTrue(scene.Inventory.Acquire(data));
            for (int i = 1; i < level; i++)
                Assert.IsTrue(scene.Inventory.Upgrade(data));
            shield = (ShieldWeapon)scene.Inventory.Find(data);
            shield.ShieldStarted += (_, _) => events.Add((Time.time, true));
            shield.ShieldEnded += _ => events.Add((Time.time, false));
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
        }

        static IEnumerator WaitFixed(float seconds)
        {
            int steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (int i = 0; i < steps; i++)
                yield return new WaitForFixedUpdate();
        }

        [Test]
        public void J0_CatalogAsset_MatchesTable_AndNewCardText()
        {
            Assert.AreEqual(3, asset.maxLevel);
            Assert.AreEqual(0.2f, asset.GetReduction(1), 0.0001f);
            Assert.AreEqual(0.3f, asset.GetReduction(3), 0.0001f);
            Assert.AreEqual(10f, asset.GetDuration(1) + asset.GetCooldown(1), 0.0001f, "Lv1 한 주기 = 지속 3 + 쿨 7");
            StringAssert.Contains("받는 피해 -20%", asset.description, "획득 카드 문구");
        }

        [UnityTest]
        public IEnumerator J1_WhileOn_DamageReduced_AfterOff_Normal()
        {
            Acquire();
            yield return WaitUntil(() => shield.IsShieldActive, 1f);
            Assert.IsTrue(shield.IsShieldActive, "적이 없어도 켜진다");

            float before = health.CurrentHp;
            health.TakeDamage(10f);
            Assert.AreEqual(8f, before - health.CurrentHp, 0.001f, "켜진 동안 × (1 − 0.2)");

            yield return WaitUntil(() => !shield.IsShieldActive, 2f);
            before = health.CurrentHp;
            health.TakeDamage(10f);
            Assert.AreEqual(10f, before - health.CurrentHp, 0.001f, "꺼진 뒤 원래대로");
        }

        [UnityTest]
        public IEnumerator J2_ContactDamage_AlsoReduced()
        {
            data.durationPerLevel = new[] { 5f };
            Acquire();
            yield return WaitUntil(() => shield.IsShieldActive, 1f);

            var enemy = scene.SpawnAt(Vector2.right * 0.9f, chase: true);
            float dps = enemy.Data.contactDps;
            yield return WaitUntil(() => health.CurrentHp < health.MaxHp, 2f);
            float start = health.CurrentHp;
            yield return WaitFixed(1f);
            Assert.IsTrue(shield.IsShieldActive);
            float expected = dps * (1f - data.GetReduction(1));
            Assert.That(start - health.CurrentHp, Is.EqualTo(expected).Within(expected * 0.2f), "접촉 데미지도 같은 배율");
        }

        [UnityTest]
        public IEnumerator J3_CooldownStartsAfterOff_NeverRestartsWhileOn()
        {
            Acquire();
            yield return WaitUntil(() => events.Count >= 4, 5f);
            Assert.GreaterOrEqual(events.Count, 4, "두 주기");

            for (int i = 0; i < 4; i++)
                Assert.AreEqual(i % 2 == 0, events[i].started, "켜짐 → 꺼짐이 번갈아 (켜진 채 다시 켜지지 않음)");
            Assert.AreEqual(0.6f, events[1].time - events[0].time, 0.1f, "지속시간");
            Assert.AreEqual(0.5f, events[2].time - events[1].time, 0.1f, "꺼진 뒤부터 쿨타임");
        }

        [UnityTest]
        public IEnumerator J4_MaxLevel_HealsLostHpRatio_OnEnd()
        {
            float healed = -1f;
            health.TakeDamage(health.MaxHp * 0.7f);
            float lost = health.MaxHp - health.CurrentHp;

            Acquire(3);
            shield.ShieldEnded += amount => healed = amount;
            yield return WaitUntil(() => healed >= 0f, 2f);

            Assert.AreEqual(lost * data.maxLevelHealRatio, healed, 0.01f, "잃은 체력 × 20%");
            Assert.AreEqual(health.MaxHp - lost + healed, health.CurrentHp, 0.01f);
            Assert.IsTrue(shield.Effect.IsShowingHeal, "회복량 표시");
        }

        [UnityTest]
        public IEnumerator J5_BelowMaxLevel_NoHeal()
        {
            float healed = -1f;
            health.TakeDamage(health.MaxHp * 0.5f);
            Acquire(2);
            shield.ShieldEnded += amount => healed = amount;
            yield return WaitUntil(() => healed >= 0f, 2f);
            Assert.AreEqual(0f, healed, "Lv2는 회복 없음");
        }

        [UnityTest]
        public IEnumerator J5b_MaxLevel_FullHp_HealZero_NoDisplay()
        {
            float healed = -1f;
            int healedEvents = 0;
            health.Healed += _ => healedEvents++;
            Acquire(3);
            shield.ShieldEnded += amount => healed = amount;
            yield return WaitUntil(() => healed >= 0f, 2f);
            Assert.AreEqual(0f, healed, "체력 가득이면 회복 0");
            Assert.AreEqual(0, healedEvents, "표시 없음");
            Assert.IsFalse(shield.Effect.IsShowingHeal);
        }

        [UnityTest]
        public IEnumerator J6_Pause_FreezesShield()
        {
            Acquire();
            yield return WaitUntil(() => shield.IsShieldActive, 1f);
            Time.timeScale = 0f;
            float end = Time.realtimeSinceStartup + 1f; // 지속시간(0.6초)보다 길게
            while (Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(shield.IsShieldActive, "일시정지 중에는 꺼지지 않는다");
            Time.timeScale = 1f;
            yield return WaitUntil(() => !shield.IsShieldActive, 2f);
            Assert.IsFalse(shield.IsShieldActive);
        }

        [UnityTest]
        public IEnumerator J7_HitWhileOn_ShieldFlashes_NoRedTint()
        {
            var feedback = scene.Player.GetComponent<DamageFeedback>();
            data.durationPerLevel = new[] { 5f };
            Acquire();
            yield return WaitUntil(() => shield.IsShieldActive, 1f);
            Assert.IsTrue(shield.Effect.IsVisible, "막 표시");

            health.TakeDamage(1f);
            Assert.IsTrue(shield.Effect.IsFlashing, "막이 하얗게 번쩍");
            yield return null;
            Assert.AreEqual(1f, feedback.CurrentTint.g, 0.01f, "빨간 틴트 없음");
            Assert.IsTrue(feedback.IsBeingHit, "HP 바 흔들림은 그대로");
        }

        [UnityTest]
        public IEnumerator J8_EffectHidden_WhenOff()
        {
            Acquire();
            yield return WaitUntil(() => shield.IsShieldActive, 1f);
            Assert.IsTrue(shield.Effect.IsVisible);
            yield return WaitUntil(() => !shield.IsShieldActive, 2f);
            Assert.IsFalse(shield.Effect.IsVisible);
        }
    }
}
