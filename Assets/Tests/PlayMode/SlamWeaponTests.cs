using System.Collections;
using System.Collections.Generic;
using Game.Enemies;
using Game.Player;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[낙매진각 7-E절] 범위 데미지, 테두리까지 넉백, 넉백 시간, 넉백 중 접촉 데미지 없음, 면역, 궁수 취소, 최대 레벨 장판.</summary>
    public class SlamWeaponTests
    {
        readonly InGameScene scene = new();
        readonly List<int> slams = new();
        SlamWeaponData data;
        SlamWeapon slam;

        [UnitySetUp]
        public IEnumerator Load()
        {
            slams.Clear();
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false; // 매화검기가 적을 맞혀 결과가 흔들리지 않게
            scene.Weapon.Pool?.ReleaseAll();

            data = null;
            foreach (var weapon in scene.Inventory.Catalog.weapons)
                if (weapon is SlamWeaponData slamData)
                    data = slamData;
            Assert.IsNotNull(data, "카탈로그에 낙매진각(SlamWeaponData)이 없다");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        // 프레임 없이 바로 강화하므로 첫 발동부터 원하는 레벨로 나간다
        void Acquire(int level = 1)
        {
            Assert.IsTrue(scene.Inventory.Acquire(data));
            for (int i = 1; i < level; i++)
                Assert.IsTrue(scene.Inventory.Upgrade(data));
            slam = (SlamWeapon)scene.Inventory.Find(data);
            slam.Slammed += hits => slams.Add(hits);
        }

        // 위치는 플레이어가 아니라 무기 원점 기준
        Enemy SpawnFromOrigin(Vector2 offset, bool chase, Enemy prefab = null)
        {
            return scene.SpawnAt(new Vector2(0f, 0.45f) + offset, chase, prefab);
        }

        Vector2 Origin => slam.Origin;

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator K1_EnemyInRange_DamagedAndPushedToEdge_OutOfRangeUntouched()
        {
            var inside = SpawnFromOrigin(new Vector2(1f, 0f), chase: false);
            var outside = SpawnFromOrigin(new Vector2(data.GetRadius(1) + 1f, 0f), chase: false);
            yield return new WaitForFixedUpdate();
            Vector2 outsideBefore = outside.transform.position;

            Acquire();
            yield return WaitUntil(() => slams.Count > 0, 1f);
            Assert.AreEqual(1, slams[0], "범위 안 적만 맞는다");
            Assert.AreEqual(inside.Data.maxHp - data.GetDamage(1), inside.CurrentHp, 0.001f);
            Assert.AreEqual(outside.Data.maxHp, outside.CurrentHp);
            Assert.IsTrue(inside.IsKnockedBack);

            yield return WaitUntil(() => !inside.IsKnockedBack, 1f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(data.GetRadius(1), Vector2.Distance(inside.transform.position, Origin), 0.05f, "테두리까지 밀림");
            Assert.That(Vector2.Distance(outside.transform.position, outsideBefore), Is.LessThan(0.01f), "범위 밖은 그대로");
        }

        [UnityTest]
        public IEnumerator K2_KnockbackTime_ContactAboutBase_NearEdgeShorter_SameAtMaxLevel([Values(1, 3)] int level)
        {
            var contact = SpawnFromOrigin(new Vector2(0f, data.contactDistance), chase: false);
            var nearEdge = SpawnFromOrigin(new Vector2(data.GetRadius(level) - 0.3f, 0f), chase: false);
            yield return new WaitForFixedUpdate();

            Acquire(level);
            yield return WaitUntil(() => slams.Count > 0, 1f);
            float start = Time.time;
            float nearEdgeTime = -1f;
            while (contact.IsKnockedBack && Time.time - start < 1f)
            {
                if (nearEdgeTime < 0f && !nearEdge.IsKnockedBack)
                    nearEdgeTime = Time.time - start;
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(data.baseKnockbackSeconds, Time.time - start, 0.06f, "닿아 있던 적은 약 0.2초");
            Assert.That(nearEdgeTime, Is.InRange(0f, data.baseKnockbackSeconds * 0.5f), "테두리 근처 적은 더 짧게");
        }

        [UnityTest]
        public IEnumerator K3_NoContactDamageWhileKnockedBack_ThenChaseResumes()
        {
            var health = scene.Player.GetComponent<PlayerHealth>();
            var enemy = SpawnFromOrigin(new Vector2(0f, data.contactDistance), chase: true);
            Acquire();
            yield return WaitUntil(() => slams.Count > 0, 1f);
            float hp = health.CurrentHp;

            while (enemy.IsKnockedBack)
            {
                Assert.AreEqual(hp, health.CurrentHp, "넉백 중 접촉 데미지 없음");
                yield return new WaitForFixedUpdate();
            }
            float edge = Vector2.Distance(enemy.transform.position, Origin);
            yield return new WaitForSeconds(0.2f);
            Assert.Less(Vector2.Distance(enemy.transform.position, Origin), edge, "끝나면 곧바로 다시 다가온다");
        }

        [UnityTest]
        public IEnumerator K4_Immune_StaysButTakesDamage()
        {
            var enemy = SpawnFromOrigin(new Vector2(1f, 0f), chase: false);
            yield return new WaitForFixedUpdate();
            Vector2 before = enemy.transform.position;
            enemy.Data.knockbackImmune = true;
            try
            {
                Acquire();
                yield return WaitUntil(() => slams.Count > 0, 1f);
                yield return new WaitForSeconds(0.1f);
                Assert.IsFalse(enemy.IsKnockedBack);
                Assert.That(Vector2.Distance(enemy.transform.position, before), Is.LessThan(0.01f), "면역 적은 위치 그대로");
                Assert.AreEqual(enemy.Data.maxHp - data.GetDamage(1), enemy.CurrentHp, 0.001f, "데미지는 받는다");
            }
            finally
            {
                enemy.Data.knockbackImmune = false; // 공유 에셋이라 되돌린다
            }
        }

        [UnityTest]
        public IEnumerator K5_ArcherWindingUp_ShotCancelled_RestartsAfterKnockback()
        {
            var prefab = scene.Spawner.Composition.entries[0].prefab;
            var enemy = SpawnFromOrigin(new Vector2(1.5f, 0f), chase: true, prefab);
            var archer = enemy.GetComponent<ArcherBehaviour>();
            float firedAt = -1f;
            archer.Fired += _ => firedAt = Time.time;
            yield return WaitUntil(() => archer.IsWindingUp, 1f);
            Assert.IsTrue(archer.IsWindingUp);

            Acquire();
            yield return WaitUntil(() => slams.Count > 0, 1f);
            Assert.IsTrue(enemy.IsKnockedBack);
            Assert.IsFalse(archer.IsWindingUp, "떨림 취소");

            yield return WaitUntil(() => !enemy.IsKnockedBack, 1f);
            float knockbackEnd = Time.time;
            Assert.Less(firedAt, 0f, "그 화살은 나가지 않는다");
            yield return WaitUntil(() => firedAt >= 0f, 2f);
            Assert.GreaterOrEqual(firedAt - knockbackEnd, archer.Data.windupSeconds - 0.1f, "떨림부터 다시");
        }

        [UnityTest]
        public IEnumerator K6_NoZone_BeforeMaxLevel()
        {
            Acquire(data.maxLevel - 1);
            yield return WaitUntil(() => slams.Count > 0, 1f);
            Assert.AreEqual(0, slam.ZonePool.CountActive, "최대 레벨 전에는 장판 없음");
        }

        [UnityTest]
        public IEnumerator K7_MaxLevelZone_DealsDamagePerSecond_ForDuration()
        {
            Acquire(data.maxLevel);
            yield return WaitUntil(() => slams.Count > 0, 1f);
            Assert.AreEqual(1, slam.ZonePool.CountActive, "최대 레벨에서 장판 생성");
            float start = Time.time;

            // 내려찍은 뒤에 들어온 적이라 내려찍기 데미지 없이 장판 데미지만 받는다
            var enemy = SpawnFromOrigin(new Vector2(1f, 0f), chase: false);
            float hp = enemy.CurrentHp;
            yield return WaitUntil(() => slam.ZonePool.CountActive == 0, data.zoneSeconds + 0.5f);

            Assert.AreEqual(data.zoneSeconds, Time.time - start, 0.1f, "지속 시간 뒤 풀 반환");
            Assert.AreEqual(hp - data.zoneSeconds * data.zoneDamagePerSecond, enemy.CurrentHp, 0.1f, "초당 데미지 × 지속 시간");
        }

        [UnityTest]
        public IEnumerator K8_Pause_StopsZoneAndKnockback()
        {
            var enemy = SpawnFromOrigin(new Vector2(0f, data.contactDistance), chase: false);
            yield return new WaitForFixedUpdate();
            Acquire(data.maxLevel);
            yield return WaitUntil(() => slams.Count > 0, 1f);

            Time.timeScale = 0f;
            Vector2 before = enemy.transform.position;
            float hp = enemy.CurrentHp;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(before, (Vector2)enemy.transform.position, "넉백이 함께 멈춘다");
            Assert.AreEqual(hp, enemy.CurrentHp, "장판 데미지도 멈춘다");
            Assert.AreEqual(1, slam.ZonePool.CountActive);
            Time.timeScale = 1f;
            yield return WaitUntil(() => !enemy.IsKnockedBack, 1f);
            Assert.IsFalse(enemy.IsKnockedBack, "재개 후 이어서 진행");
        }

        [UnityTest]
        public IEnumerator K9_KilledDuringKnockback_ComesBackClean()
        {
            var enemy = SpawnFromOrigin(new Vector2(1f, 0f), chase: false);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(enemy.TryKnockback(Vector2.right * 5f, 5f));
            enemy.TakeDamage(enemy.CurrentHp);
            Assert.IsFalse(enemy.gameObject.activeSelf, "넉백 중에도 즉시 처치 처리");

            enemy.gameObject.SetActive(true); // 풀에서 다시 꺼낼 때와 같은 경로
            Assert.IsFalse(enemy.IsKnockedBack, "넉백 상태는 다시 꺼낼 때 초기화");
            enemy.gameObject.SetActive(false);
        }
    }
}
