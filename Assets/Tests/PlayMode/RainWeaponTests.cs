using System.Collections;
using System.Collections.Generic;
using Game.Enemies;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[만천화우 7-F절] 화면 안 적 선택, 범위 지속 데미지, 고정, 최대 레벨 끝 폭발, 겹침 합산, 즉발(쿨타임 바로 시작).</summary>
    public class RainWeaponTests
    {
        const float TestHp = 100f; // 2초 12 데미지 + 폭발을 다 받아도 죽지 않게

        readonly InGameScene scene = new();
        readonly List<(Vector2 center, bool picked)> casts = new();
        RainWeaponData data;
        RainWeapon rain;
        EnemyData enemyData;
        float originalHp;

        [UnitySetUp]
        public IEnumerator Load()
        {
            casts.Clear();
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            scene.Player.GetComponent<Game.Player.PlayerHealth>().Invincible = true;

            data = null;
            foreach (var weapon in scene.Inventory.Catalog.weapons)
                if (weapon is RainWeaponData rainData)
                    data = rainData;
            Assert.IsNotNull(data, "카탈로그에 만천화우(RainWeaponData)가 없다");

            enemyData = scene.Spawner.BasePrefab.Data;
            originalHp = enemyData.maxHp;
            enemyData.maxHp = TestHp;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            enemyData.maxHp = originalHp; // 공유 에셋이라 되돌린다
            Time.timeScale = 1f;
            yield return null;
        }

        void Acquire(int level = 1)
        {
            Assert.IsTrue(scene.Inventory.Acquire(data));
            for (int i = 1; i < level; i++)
                Assert.IsTrue(scene.Inventory.Upgrade(data));
            rain = (RainWeapon)scene.Inventory.Find(data);
            rain.Cast += (center, picked) => casts.Add((center, picked));
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
                yield return null;
        }

        IEnumerator WaitCast()
        {
            yield return WaitUntil(() => casts.Count > 0, 1f);
            Assert.IsNotEmpty(casts, "발동하지 않았다");
        }

        IEnumerator WaitZoneEnd() => WaitUntil(() => rain.ZonePool.CountActive == 0, data.GetDuration(1) + 0.5f);

        Rect View => RainWeapon.ViewRect(scene.Camera, scene.Player.transform.position);

        [UnityTest]
        public IEnumerator R1_PicksOnlyEnemyInView()
        {
            var inside = scene.SpawnAt(new Vector2(2f, 1f), chase: false);
            scene.SpawnAt(new Vector2(View.width + 5f, 0f), chase: false);
            yield return new WaitForFixedUpdate();

            Acquire();
            yield return WaitCast();
            Assert.IsTrue(casts[0].picked);
            Assert.That(Vector2.Distance(casts[0].center, inside.transform.position), Is.LessThan(0.01f), "화면 안 적 자리에 생성");
        }

        [UnityTest]
        public IEnumerator R2_NoEnemyInView_CastsAtPointInView_AndCooldownStartsImmediately()
        {
            scene.SpawnAt(new Vector2(View.width + 5f, 0f), chase: false);
            yield return new WaitForFixedUpdate();

            Acquire();
            yield return WaitCast();
            Assert.IsFalse(casts[0].picked, "화면 밖 적은 고르지 않는다");
            Assert.IsTrue(View.Contains(casts[0].center), "화면 안 무작위 지점");
            Assert.IsFalse(rain.IsAttacking, "즉발: 범위를 놓으면 공격 끝");
            Assert.AreEqual(1, rain.ZonePool.CountActive, "범위는 무기와 따로 남는다");
        }

        [UnityTest]
        public IEnumerator R3_InsideTakesDamageOverDuration_OutsideNone_NoExplosionBelowMax()
        {
            var inside = scene.SpawnAt(new Vector2(2f, 1f), chase: false);
            yield return new WaitForFixedUpdate();
            Acquire();
            yield return WaitCast();
            var zone = Object.FindFirstObjectByType<DamageZone>();
            bool? exploded = null;
            zone.Ended += (e, _) => exploded = e;
            // 범위가 놓인 뒤에 두어 선택 대상이 되지 않게 한다. 화면 안이지만 원 밖
            var outside = scene.SpawnAt(new Vector2(2f - data.GetRadius(1) - 1f, 1f), chase: false);
            float start = Time.time;

            yield return WaitZoneEnd();
            Assert.AreEqual(data.GetDuration(1), Time.time - start, 0.1f);
            Assert.AreEqual(TestHp - 12f, inside.CurrentHp, 0.5f, "Lv1: 2초 × 초당 6");
            Assert.AreEqual(TestHp, outside.CurrentHp, "범위 밖 0");
            Assert.AreEqual(false, exploded, "최대 레벨 전에는 폭발 없음");
        }

        [UnityTest]
        public IEnumerator R4_ZoneStaysFixed_WhenPickedEnemyMoves()
        {
            var enemy = scene.SpawnAt(new Vector2(3f, 0f), chase: true);
            yield return new WaitForFixedUpdate();
            Acquire();
            yield return WaitCast();
            var zone = Object.FindFirstObjectByType<DamageZone>();
            Vector2 center = zone.transform.position;

            yield return new WaitForSeconds(0.5f);
            Assert.That(Vector2.Distance(enemy.transform.position, center), Is.GreaterThan(0.3f), "적은 움직였다");
            Assert.AreEqual(center, (Vector2)zone.transform.position, "범위 중심은 그대로");
        }

        [UnityTest]
        public IEnumerator R5_MaxLevel_ExplodesOnlyOnEnemiesInsideAtEnd()
        {
            var stays = scene.SpawnAt(new Vector2(2f, 1f), chase: false);
            yield return new WaitForFixedUpdate();
            Acquire(data.maxLevel);
            yield return WaitCast();
            var zone = Object.FindFirstObjectByType<DamageZone>();
            bool exploded = false;
            int hits = -1;
            zone.Ended += (e, h) => { exploded = e; hits = h; };

            // 폭발 직전에 범위 밖으로 나간 적은 폭발을 맞지 않는다
            var leaves = scene.SpawnAt(new Vector2(2f, 1.5f), chase: false);
            yield return new WaitForSeconds(data.GetDuration(data.maxLevel) * 0.5f);
            Vector2 away = casts[0].center + Vector2.right * (data.GetRadius(data.maxLevel) + 2f);
            leaves.transform.position = away;
            leaves.GetComponent<Rigidbody2D>().position = away;
            yield return new WaitForFixedUpdate();
            float hpAfterLeaving = leaves.CurrentHp;

            yield return WaitZoneEnd();
            Assert.IsTrue(exploded, "최대 레벨은 끝날 때 폭발");
            Assert.AreEqual(1, hits, "그때 원 안에 있는 적만");
            Assert.AreEqual(TestHp - 12f - data.explosionDamage, stays.CurrentHp, 0.5f);
            Assert.AreEqual(hpAfterLeaving, leaves.CurrentHp, "나간 적은 지속 데미지도 폭발도 없음");
        }

        [UnityTest]
        public IEnumerator R6_OverlappingZones_DamageAdds()
        {
            var enemy = scene.SpawnAt(new Vector2(2f, 1f), chase: false);
            yield return new WaitForFixedUpdate();
            Acquire();
            yield return WaitCast();
            rain.ZonePool.Get(casts[0].center).Begin(data.GetRadius(1), data.GetDuration(1), data.GetDamage(1), rain.ZonePool.Release);

            yield return WaitZoneEnd();
            Assert.AreEqual(TestHp - 24f, enemy.CurrentHp, 1f, "두 범위가 겹치면 합산");
        }

        [UnityTest]
        public IEnumerator R7_RainArt_PlaysOnceOverDuration_FallThenLand()
        {
            Acquire();
            yield return WaitCast();
            var art = Object.FindFirstObjectByType<DamageZone>().GetComponentInChildren<FrameAnimation>();
            Assert.Less(art.CurrentFrame, 5, "처음은 사선 낙하 시작");
            yield return new WaitForSeconds(data.GetDuration(1) - 0.15f);
            Assert.GreaterOrEqual(art.CurrentFrame, art.FrameCount - 5, "장판이 끝날 무렵 착탄 장면까지 재생");
        }
    }
}
