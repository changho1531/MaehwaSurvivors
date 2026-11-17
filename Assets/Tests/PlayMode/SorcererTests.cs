using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Enemies;
using Game.Player;
using Game.Pooling;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [저주술사 8-B절] 거리 유지(물러나지 않음), 5초마다 경고 → 장판, 화면 밖이면 안 쏨, 장판 데미지(자하호신강 감소 포함),
    /// 저주(×0.8, 벗어난 뒤 2초, 중첩 없음, 매화보와 곱연산), 10초마다 도병 2마리(상한 내), 넉백 면역,
    /// 웨이브 3/4/5 등장 간격 · 동시 최대 수 · 보스전 미등장, 죽어도 장판 · 소환 유지.
    /// 시간을 기다리는 테스트는 게임 속도를 올려 짧게 돌린다.
    /// </summary>
    public class SorcererTests
    {
        readonly InGameScene scene = new();
        PlayerHealth health;
        PlayerCurse curse;
        Enemy sorcererPrefab;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            health = scene.Player.GetComponent<PlayerHealth>();
            curse = scene.Player.GetComponent<PlayerCurse>();
            Assert.IsNotNull(curse, "플레이어에 PlayerCurse가 없다");

            var prefabs = new List<Enemy>();
            scene.Spawner.Waves.Table.CollectPrefabs(prefabs);
            sorcererPrefab = prefabs.Find(p => p.GetComponent<SorcererBehaviour>() != null);
            Assert.IsNotNull(sorcererPrefab, "웨이브 표에 저주술사가 없다");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            PrefabPools.ReleaseAll();
            yield return null;
        }

        SorcererBehaviour Spawn(Vector2 offset, bool chase = true)
        {
            var enemy = scene.SpawnAt(offset, chase, sorcererPrefab);
            return enemy.GetComponent<SorcererBehaviour>();
        }

        static IEnumerator WaitGameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [Test]
        public void C0_DataAndWaveTable()
        {
            var data = (SorcererEnemyData)sorcererPrefab.Data;
            Assert.AreEqual(25f, data.maxHp);
            Assert.AreEqual(5f, data.contactDps);
            Assert.AreEqual(10, data.xpAmount);
            Assert.IsTrue(data.knockbackImmune);
            Assert.AreEqual(4.5f, data.keepDistance);
            Assert.AreEqual(5f, data.castInterval);
            Assert.AreEqual(0.2f, data.curseSlowRatio, 0.0001f);
            Assert.AreEqual(scene.Spawner.BasePrefab, data.summonPrefab, "소환은 일반 도병과 같은 프리팹(같은 풀)");

            var table = scene.Spawner.Waves.Table;
            Assert.AreEqual(0, table.waves[0].settings.specials.Length);
            Assert.AreEqual(0, table.waves[1].settings.specials.Length);
            float[] intervals = { 40f, 30f, 20f };
            int[] maxAlive = { 2, 3, 3 };
            for (int i = 0; i < 3; i++)
            {
                var special = table.waves[i + 2].settings.specials[0];
                Assert.AreEqual(sorcererPrefab, special.prefab);
                Assert.AreEqual(intervals[i], special.intervalSeconds);
                Assert.AreEqual(maxAlive[i], special.maxAlive);
            }
            Assert.AreEqual(0, table.bossPhase.specials.Length, "보스전에는 나오지 않음");
        }

        [UnityTest]
        public IEnumerator C1_ApproachesWhenFar_StopsInside_NeverRetreats()
        {
            var far = Spawn(new Vector2(7f, 0f));
            var near = Spawn(new Vector2(0f, 3f));
            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            var farBody = far.GetComponent<Rigidbody2D>();
            var nearBody = near.GetComponent<Rigidbody2D>();
            Assert.Less(farBody.linearVelocity.x, -0.5f, "4.5칸 밖이면 다가온다");
            Assert.Less(nearBody.linearVelocity.magnitude, 0.6f, "안이면 멈춤(분리 힘 정도만)");
            Vector2 before = nearBody.position;
            for (int i = 0; i < 20; i++)
                yield return new WaitForFixedUpdate();
            float distBefore = (before - (Vector2)scene.Player.transform.position).magnitude;
            float distAfter = (nearBody.position - (Vector2)scene.Player.transform.position).magnitude;
            Assert.LessOrEqual(distAfter, distBefore + 0.2f, "뒤로 물러나지 않는다");
        }

        [UnityTest]
        public IEnumerator C2_CastsEveryFiveSeconds_AtPlayerPosition()
        {
            health.Invincible = true;
            var sorcerer = Spawn(new Vector2(3f, 0f));
            var casts = new List<(float time, Vector2 at)>();
            sorcerer.ZoneCast += p => casts.Add((Time.time, p));
            float start = Time.time;
            Time.timeScale = 4f;
            yield return WaitGameSeconds(10.5f);
            Time.timeScale = 1f;

            Assert.AreEqual(2, casts.Count, "10.5초 동안 5초 · 10초에 2번");
            Assert.AreEqual(5f, casts[0].time - start, 0.3f);
            Assert.AreEqual(5f, casts[1].time - casts[0].time, 0.3f);
            Assert.Less((casts[0].at - (Vector2)scene.Player.transform.position).magnitude, 0.01f, "플레이어 위치에");
        }

        [UnityTest]
        public IEnumerator C3_OffScreen_DoesNotCast()
        {
            var sorcerer = Spawn(new Vector2(30f, 0f));
            int casts = 0;
            sorcerer.ZoneCast += _ => casts++;
            Time.timeScale = 4f;
            yield return WaitGameSeconds(6f);
            Time.timeScale = 1f;
            Assert.IsFalse(ScreenCheck.IsVisible(sorcerer.transform.position), "아직 화면 밖이어야 한다");
            Assert.AreEqual(0, casts, "화면 밖이면 장판을 쏘지 않음");
        }

        [UnityTest]
        public IEnumerator C4_Zone_WarningNoDamage_Then15OverThreeSeconds()
        {
            var sorcerer = Spawn(new Vector2(4f, 0f));
            yield return new WaitForFixedUpdate(); // 대상(플레이어)을 한 번 읽은 뒤
            float hp = health.CurrentHp;
            sorcerer.CastZone(scene.Player.transform.position);
            yield return WaitGameSeconds(0.9f);
            Assert.AreEqual(hp, health.CurrentHp, 0.001f, "경고 중 판정 없음");
            Assert.IsFalse(curse.IsCursed);

            Time.timeScale = 3f;
            yield return WaitGameSeconds(3.5f);
            Time.timeScale = 1f;
            Assert.AreEqual(15f, hp - health.CurrentHp, 1.5f, "3초 다 맞으면 약 15");
        }

        [UnityTest]
        public IEnumerator C5_Zone_ShieldReduces_OutsideNoDamage()
        {
            var sorcerer = Spawn(new Vector2(4f, 0f));
            yield return new WaitForFixedUpdate();
            health.IncomingDamageMultiplier = 0.8f; // 자하호신강 Lv1 막이 켜진 상태
            float hp = health.CurrentHp;
            sorcerer.CastZone(scene.Player.transform.position);
            sorcerer.CastZone((Vector2)scene.Player.transform.position + new Vector2(0f, 5f)); // 밖
            Time.timeScale = 3f;
            yield return WaitGameSeconds(4.5f);
            Time.timeScale = 1f;
            health.IncomingDamageMultiplier = 1f;
            Assert.AreEqual(12f, hp - health.CurrentHp, 1.5f, "15 × 0.8, 밖의 장판은 0");
        }

        [UnityTest]
        public IEnumerator C6_Curse_WhileInsideAndTwoSecondsAfter_NoStacking()
        {
            health.Invincible = true;
            var movement = scene.Player;
            var sorcerer = Spawn(new Vector2(4f, 0f));
            yield return new WaitForFixedUpdate();
            sorcerer.CastZone(scene.Player.transform.position);
            yield return WaitGameSeconds(1.3f);
            Assert.IsTrue(curse.IsCursed, "장판 안이면 저주");
            Assert.AreEqual(movement.MoveSpeed * 0.8f, movement.CurrentSpeed, 0.001f);

            // 장판 밖으로 옮긴다 → 그 순간부터 2초
            var body = scene.Player.GetComponent<Rigidbody2D>();
            body.position += new Vector2(0f, 4f);
            scene.Player.transform.position = body.position;
            yield return new WaitForFixedUpdate();
            yield return WaitGameSeconds(1.5f);
            Assert.IsTrue(curse.IsCursed, "벗어난 뒤 2초 동안 유지");
            yield return WaitGameSeconds(0.8f);
            Assert.IsFalse(curse.IsCursed, "2초 뒤 해제");
            Assert.AreEqual(movement.MoveSpeed, movement.CurrentSpeed, 0.001f);

            curse.Apply(0.2f, 2f);
            curse.Apply(0.2f, 2f);
            Assert.AreEqual(0.8f, movement.SlowMultiplier, 0.0001f, "중첩 없음");
            Assert.AreEqual(2f, curse.Remaining, 0.0001f, "남은 시간만 되돌림");
        }

        [Test]
        public void C7_Curse_MultipliesWithStepBoost()
        {
            var movement = scene.Player;
            movement.SpeedMultiplier = 1.25f; // 매화보 최대 가속 (15% + 10%)
            curse.Apply(0.2f, 2f);
            Assert.AreEqual(movement.MoveSpeed * 1f, movement.CurrentSpeed, 0.001f, "1.25 × 0.8 = 기본 속도");
            movement.SpeedMultiplier = 1.15f; // 맞아서 가속 꺼짐
            Assert.AreEqual(movement.MoveSpeed * 1.15f * 0.8f, movement.CurrentSpeed, 0.001f);
            curse.Clear();
            movement.SpeedMultiplier = 1f;
        }

        [Test]
        public void C8_Summon_TwoMeleeNearby_LimitedByCap()
        {
            var sorcerer = Spawn(new Vector2(3f, 0f), chase: false);
            int before = scene.Spawner.PoolOf(scene.Spawner.BasePrefab).CountActive;
            Assert.AreEqual(2, sorcerer.Summon());
            Assert.AreEqual(before + 2, scene.Spawner.PoolOf(scene.Spawner.BasePrefab).CountActive);
            foreach (var enemy in EnemyRegistry.Active)
                if (enemy.Data == scene.Spawner.BasePrefab.Data)
                    Assert.Less(((Vector2)(enemy.transform.position - sorcerer.transform.position)).magnitude, 1.6f, "주변 1.5칸");

            var field = typeof(EnemySpawner).GetField("maxActive", BindingFlags.NonPublic | BindingFlags.Instance);
            int original = (int)field.GetValue(scene.Spawner);
            field.SetValue(scene.Spawner, scene.Spawner.CountActive + 1);
            Assert.AreEqual(1, sorcerer.Summon(), "상한에 닿으면 들어가는 만큼만");
            field.SetValue(scene.Spawner, original);
        }

        [UnityTest]
        public IEnumerator C9_SummonsEveryTenSeconds()
        {
            health.Invincible = true;
            var sorcerer = Spawn(new Vector2(3f, 0f));
            var summons = new List<float>();
            sorcerer.Summoned += _ => summons.Add(Time.time);
            float start = Time.time;
            Time.timeScale = 5f;
            yield return WaitGameSeconds(10.5f);
            Time.timeScale = 1f;
            Assert.AreEqual(1, summons.Count);
            Assert.AreEqual(10f, summons[0] - start, 0.3f);
        }

        [Test]
        public void C10_KnockbackImmune_TakesDamage()
        {
            var enemy = Spawn(new Vector2(3f, 0f), chase: false).GetComponent<Enemy>();
            Assert.IsFalse(enemy.TryKnockback(Vector2.zero, 10f), "넉백 면역");
            enemy.TakeDamage(10f);
            Assert.AreEqual(15f, enemy.CurrentHp, 0.001f, "데미지는 받는다");
        }

        [Test]
        public void C11_Waves_IntervalAndMaxAlive_NoneInBoss()
        {
            var spawner = scene.Spawner;
            var waves = spawner.Waves;
            Assert.AreEqual(0, spawner.AdvanceSpecials(100f), "웨이브 1 · 2에는 없음");

            waves.Advance(240f); // 웨이브 3
            Assert.AreEqual(1, spawner.AdvanceSpecials(0.01f), "웨이브 시작 순간 첫 1마리");
            Assert.AreEqual(0, spawner.AdvanceSpecials(39f));
            Assert.AreEqual(1, spawner.AdvanceSpecials(1.1f), "40초 간격");
            Assert.AreEqual(0, spawner.AdvanceSpecials(40f), "동시 최대 2 → 건너뜀");
            Assert.AreEqual(2, spawner.PoolOf(sorcererPrefab).CountActive);

            waves.Advance(360f); // 웨이브 4: 최대 3, 30초
            Assert.AreEqual(1, spawner.AdvanceSpecials(0.01f));
            Assert.AreEqual(0, spawner.AdvanceSpecials(30f), "3마리 → 건너뜀");

            spawner.PoolOf(sorcererPrefab).ReleaseAll();
            waves.Advance(480f); // 웨이브 5: 20초
            Assert.AreEqual(1, spawner.AdvanceSpecials(0.01f));
            Assert.AreEqual(1, spawner.AdvanceSpecials(20f));

            spawner.PoolOf(sorcererPrefab).ReleaseAll();
            Assert.IsTrue(waves.SummonBossEarly());
            Assert.AreEqual(0, spawner.AdvanceSpecials(100f), "보스전에는 새로 나오지 않음");
        }

        [UnityTest]
        public IEnumerator C12_DeathKeepsZoneAndSummons()
        {
            health.Invincible = true;
            var sorcerer = Spawn(new Vector2(3f, 0f));
            yield return new WaitForFixedUpdate();
            sorcerer.CastZone(scene.Player.transform.position);
            Assert.AreEqual(2, sorcerer.Summon());
            var zonePool = PrefabPools.For(sorcerer.Data.zonePrefab);
            int melee = scene.Spawner.PoolOf(scene.Spawner.BasePrefab).CountActive;

            sorcerer.GetComponent<Enemy>().TakeDamage(1000f);
            yield return null;
            Assert.AreEqual(1, zonePool.CountActive, "죽어도 장판은 남는다");
            Assert.AreEqual(melee, scene.Spawner.PoolOf(scene.Spawner.BasePrefab).CountActive, "소환된 도병도 남는다");

            yield return WaitGameSeconds(1.2f);
            Assert.IsTrue(curse.IsCursed, "경고가 끝나면 그대로 깔려 저주");
        }

        [Test]
        public void C13_DeathClearsCurse()
        {
            curse.Apply(0.2f, 2f);
            health.TakeDamage(10000f);
            Assert.IsFalse(curse.IsCursed, "게임 오버 → 저주 해제");
            Assert.AreEqual(1f, scene.Player.SlowMultiplier);
        }
    }
}
