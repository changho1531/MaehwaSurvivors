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
    /// [매화보 7-H절] 레벨별 이동속도 배율, 최대 레벨 무피격 가속(켜짐·맞으면 꺼짐·다시 켜짐), Lv2 이하 가속 없음,
    /// 줄어든 피해도 맞은 것, 기본 이동속도 고정(화살·XP 끌림), 일시정지 중 정지, 꽃잎, 카탈로그 등록.
    /// 대기 시간을 줄이려고 카탈로그 에셋을 복제해 무피격 시간만 바꾼다.
    /// </summary>
    public class MovementPassiveTests
    {
        readonly InGameScene scene = new();
        readonly List<bool> boostEvents = new();
        MovementPassiveData asset;
        MovementPassiveData data;
        MovementPassive passive;
        PlayerHealth health;
        PlayerMovement movement;

        [UnitySetUp]
        public IEnumerator Load()
        {
            boostEvents.Clear();
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            health = scene.Player.GetComponent<PlayerHealth>();
            movement = scene.Player;

            asset = null;
            foreach (var weapon in scene.Inventory.Catalog.weapons)
                if (weapon is MovementPassiveData passiveData)
                    asset = passiveData;
            Assert.IsNotNull(asset, "카탈로그에 매화보(MovementPassiveData)가 없다");

            data = Object.Instantiate(asset);
            data.boostDelaySeconds = 0.5f;
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
            passive = (MovementPassive)scene.Inventory.Find(data);
            passive.BoostChanged += on => boostEvents.Add(on);
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
        }

        float Multiplier => movement.CurrentSpeed / movement.MoveSpeed;

        [Test]
        public void M0_CatalogAsset_MatchesTable_AndNewCardText()
        {
            Assert.AreEqual(3, asset.maxLevel);
            Assert.AreEqual(3f, asset.boostDelaySeconds, 0.0001f);
            Assert.AreEqual(1.25f, asset.GetSpeedMultiplier(3, true), 0.0001f);
            Assert.AreEqual("이동속도 +5%", asset.description, "획득 카드 문구");
        }

        [Test]
        public void M1_SpeedPerLevel_AppliedImmediately()
        {
            Assert.AreEqual(1f, Multiplier, 0.0001f, "매화보 없으면 기본 속도");
            Acquire();
            Assert.AreEqual(1.05f, Multiplier, 0.0001f, "획득 즉시");
            scene.Inventory.Upgrade(data);
            Assert.AreEqual(1.10f, Multiplier, 0.0001f, "강화 즉시");
            scene.Inventory.Upgrade(data);
            Assert.AreEqual(1.15f, Multiplier, 0.0001f);
        }

        [UnityTest]
        public IEnumerator M2_MaxLevel_BoostAfterNoHit_OffOnHit_OnAgain()
        {
            Acquire(3);
            Assert.AreEqual(1.15f, Multiplier, 0.0001f);
            yield return WaitUntil(() => passive.IsBoosted, 2f);
            Assert.AreEqual(1.25f, Multiplier, 0.0001f, "무피격 시간이 지나면 +10%p");

            health.TakeDamage(1f);
            Assert.IsFalse(passive.IsBoosted, "맞는 순간 꺼짐");
            Assert.AreEqual(1.15f, Multiplier, 0.0001f);

            float hitTime = Time.time;
            yield return WaitUntil(() => passive.IsBoosted, 2f);
            Assert.IsTrue(passive.IsBoosted);
            Assert.GreaterOrEqual(Time.time - hitTime, data.boostDelaySeconds - 0.02f, "다시 처음부터 센다");
            CollectionAssert.AreEqual(new[] { true, false, true }, boostEvents);
        }

        [UnityTest]
        public IEnumerator M3_BelowMaxLevel_NoBoost()
        {
            Acquire(2);
            float end = Time.time + data.boostDelaySeconds * 2f;
            while (Time.time < end)
                yield return null;
            Assert.IsFalse(passive.IsBoosted);
            Assert.AreEqual(1.10f, Multiplier, 0.0001f);
        }

        [UnityTest]
        public IEnumerator M4_ReducedDamage_StillCountsAsHit()
        {
            Acquire(3);
            yield return WaitUntil(() => passive.IsBoosted, 2f);
            health.IncomingDamageMultiplier = 0.8f; // 자하호신강 막이 켜진 상태
            health.TakeDamage(1f);
            health.IncomingDamageMultiplier = 1f;
            Assert.IsFalse(passive.IsBoosted, "줄어든 피해도 맞은 것");
        }

        [UnityTest]
        public IEnumerator M5_BaseSpeed_Unchanged_ForArrowAndMagnet()
        {
            float baseSpeed = movement.MoveSpeed;
            Acquire(3);
            yield return WaitUntil(() => passive.IsBoosted, 2f);
            Assert.AreEqual(baseSpeed, movement.MoveSpeed, "화살 속도 기준(기본 이동속도)은 매화보와 무관");
            Assert.Greater(scene.Magnet.PullSpeed, movement.CurrentSpeed, "XP 끌림 > 가속 중 플레이어 속도");
            Assert.Greater(scene.Magnet.PullSpeed, baseSpeed * 1.25f);
        }

        [UnityTest]
        public IEnumerator M6_Pause_StopsNoHitTimer()
        {
            Acquire(3);
            yield return null;
            Time.timeScale = 0f;
            float end = Time.realtimeSinceStartup + data.boostDelaySeconds * 2f;
            while (Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsFalse(passive.IsBoosted, "일시정지 중에는 무피격 시간이 흐르지 않는다");
            Time.timeScale = 1f;
            yield return WaitUntil(() => passive.IsBoosted, 2f);
            Assert.IsTrue(passive.IsBoosted);
        }

        [UnityTest]
        public IEnumerator M7_Petals_OnlyWhenMoving()
        {
            Acquire();
            yield return null;
            Assert.IsNotNull(passive.Petals, "꽃잎 연출");
            Assert.IsFalse(passive.Petals.IsEmitting, "멈춰 있으면 방출 없음");

            passive.Petals.SetState(moving: true, boosted: false);
            float normal = passive.Petals.CurrentRate;
            passive.Petals.SetState(moving: true, boosted: true);
            Assert.Greater(passive.Petals.CurrentRate, normal, "가속 중 강하게");
            Assert.Greater(normal, 0f);
        }

        [UnityTest]
        public IEnumerator M8_MaxLevelReached_CountsFromThatMoment()
        {
            Acquire(2);
            float end = Time.time + data.boostDelaySeconds * 2f; // Lv2에서 안 맞은 시간
            while (Time.time < end)
                yield return null;
            scene.Inventory.Upgrade(data);
            Assert.IsFalse(passive.IsBoosted, "그 전에 안 맞은 시간은 치지 않는다");
            yield return WaitUntil(() => passive.IsBoosted, 2f);
            Assert.IsTrue(passive.IsBoosted);
        }
    }
}
