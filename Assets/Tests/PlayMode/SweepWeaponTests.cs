using System.Collections;
using System.Collections.Generic;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[화산십이검 7절] 적 없이도 발동, 콤보 12타 균등 간격, 부채꼴 안 모든 적 반복 타격, 콤보 후 쿨타임, 콤보 중 레벨업·일시정지.</summary>
    public class SweepWeaponTests
    {
        readonly InGameScene scene = new();
        readonly List<(float time, int index, int hits)> strikes = new();
        SweepWeaponData data;
        SweepWeapon sweep;

        [UnitySetUp]
        public IEnumerator Load()
        {
            strikes.Clear();
            watched = null;
            watchedHp.Clear();
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false; // 매화검기가 적을 처치해 결과가 흔들리지 않게
            // 적이 없으면 바라보는 방향(위)으로 이미 발사했을 수 있다. 비행 중인 검기가 부채꼴 안 적을 맞히지 않게 회수.
            scene.Weapon.Pool?.ReleaseAll();

            data = null;
            foreach (var weapon in scene.Inventory.Catalog.weapons)
                if (weapon is SweepWeaponData sweepData)
                    data = sweepData;
            Assert.IsNotNull(data, "카탈로그에 화산십이검(SweepWeaponData)이 없다");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        // 타격 직후 관찰 대상 적의 HP를 기록한다. 프레임이 튀면 한 프레임에 여러 타격이 몰릴 수 있으므로
        // (게임 시간 기준 스케줄을 따라잡음) 대기 시간이 아니라 "n번째 타격 직후 상태"로 검증한다.
        Game.Enemies.Enemy watched;
        readonly List<float> watchedHp = new();

        void Acquire()
        {
            Assert.IsTrue(scene.Inventory.Acquire(data));
            sweep = (SweepWeapon)scene.Inventory.Find(data);
            sweep.Struck += (index, hits) =>
            {
                strikes.Add((Time.time, index, hits));
                if (watched != null)
                    watchedHp.Add(watched.CurrentHp);
            };
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
                yield return null;
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator S1_TriggersWithoutEnemies_12HitsEvenlyOverComboDuration()
        {
            Acquire();
            yield return WaitUntil(() => strikes.Count >= data.hitCount && !sweep.IsAttacking, data.comboDuration + 1f);

            Assert.AreEqual(data.hitCount, strikes.Count, "적이 없어도 발동해 12타");
            for (int i = 0; i < strikes.Count; i++)
                Assert.AreEqual(i, strikes[i].index);
            // 첫 타격은 발동 즉시, 마지막 타격은 (지속 시간 - 간격) 시점 → 12타가 지속 시간 동안 균등 분포
            float interval = data.comboDuration / data.hitCount;
            float span = strikes[strikes.Count - 1].time - strikes[0].time;
            Assert.That(span, Is.EqualTo(data.comboDuration - interval).Within(0.1f), "12타가 콤보 지속 시간에 걸쳐 분포");
            Assert.IsFalse(sweep.IsAttacking, "콤보 지속 시간이 지나면 종료");
        }

        [UnityTest]
        public IEnumerator S2_EnemyInFan_IsHitRepeatedly_OthersAreNot()
        {
            var front = scene.SpawnAt(new Vector2(0f, 1.5f), chase: false);              // 바라보는 방향(위) 부채꼴 안
            var behind = scene.SpawnAt(new Vector2(0f, -1.5f), chase: false);            // 등 뒤
            var tooFar = scene.SpawnAt(new Vector2(0f, data.radius + 1.5f), chase: false); // 반경 밖
            var side = scene.SpawnAt(new Vector2(2f, -0.5f), chase: false);              // 각도 밖
            yield return null;
            float hp = front.CurrentHp;
            watched = front;

            Acquire();
            float damage = data.GetDamage(1);
            yield return WaitUntil(() => strikes.Count >= 2, 1f);

            Assert.GreaterOrEqual(watchedHp.Count, 2);
            Assert.AreEqual(hp - damage, watchedHp[0], 0.001f, "0번 타격");
            Assert.AreEqual(hp - damage * 2f, watchedHp[1], 0.001f, "같은 적이 매 타격마다 맞는다");
            Assert.AreEqual(behind.Data.maxHp, behind.CurrentHp, "등 뒤는 안 맞음");
            Assert.AreEqual(tooFar.Data.maxHp, tooFar.CurrentHp, "반경 밖은 안 맞음");
            Assert.AreEqual(side.Data.maxHp, side.CurrentHp, "각도 밖은 안 맞음");
        }

        [UnityTest]
        public IEnumerator S3_AllEnemiesInFan_AreHitBySameStrike()
        {
            scene.SpawnAt(new Vector2(-0.8f, 1.5f), chase: false);
            scene.SpawnAt(new Vector2(0f, 2f), chase: false);
            scene.SpawnAt(new Vector2(0.8f, 1.5f), chase: false);
            yield return null;

            Acquire();
            yield return null;
            Assert.IsNotEmpty(strikes);
            Assert.AreEqual(3, strikes[0].hits, "범위 안 모든 적에게 데미지");
        }

        [UnityTest]
        public IEnumerator S4_Cooldown_StartsAfterComboEnds()
        {
            Acquire();
            float firstStart = Time.time;
            float expectedGap = data.comboDuration + data.GetCooldown(1);

            yield return Wait(expectedGap - 0.3f);
            Assert.AreEqual(data.hitCount, strikes.Count, "콤보 + 쿨타임이 끝나기 전에는 다음 콤보 없음");

            yield return Wait(0.6f);
            Assert.Greater(strikes.Count, data.hitCount, "콤보 종료 후 쿨타임이 지나면 다시 발동");
            float secondStart = strikes[data.hitCount].time;
            Assert.That(secondStart - firstStart, Is.EqualTo(expectedGap).Within(0.15f), "쿨타임은 콤보가 끝난 뒤부터");
        }

        [UnityTest]
        public IEnumerator S5_LevelUpMidCombo_KeepsOldDamageUntilComboEnds()
        {
            var front = scene.SpawnAt(new Vector2(0f, 1.5f), chase: false);
            yield return null;
            float hp = front.CurrentHp;
            watched = front;

            Acquire();
            yield return WaitUntil(() => strikes.Count >= 1, 1f); // 0번 타격 (Lv1)
            Assert.IsTrue(sweep.IsAttacking);
            Assert.IsTrue(scene.Inventory.Upgrade(data), "콤보 도중 강화");
            Assert.AreNotEqual(data.GetDamage(1), data.GetDamage(2));

            yield return WaitUntil(() => strikes.Count >= 2, 1f); // 1번 타격
            Assert.AreEqual(hp - data.GetDamage(1) * 2f, watchedHp[1], 0.001f, "진행 중인 콤보는 이전 레벨 수치로");
        }

        [UnityTest]
        public IEnumerator S6_Pause_StopsCombo()
        {
            Acquire();
            yield return null;
            int before = strikes.Count;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(before, strikes.Count, "일시정지 중 콤보 진행 정지");
            Time.timeScale = 1f;
            yield return Wait(0.3f);
            Assert.Greater(strikes.Count, before, "재개 후 이어서 진행");
        }

        [UnityTest]
        public IEnumerator S7_Effect_FollowsPlayer_AndFacesDirection()
        {
            Acquire();
            yield return null;
            Assert.IsNotNull(sweep.Effect, "이펙트 프리팹 연결 누락");
            Assert.AreSame(scene.Player.transform, sweep.Effect.transform.parent, "플레이어 자식이라 이동을 따라간다");
            Assert.IsTrue(sweep.Effect.gameObject.activeInHierarchy, "콤보 중 표시");
            float expected = Mathf.Atan2(scene.Facing.Direction.y, scene.Facing.Direction.x) * Mathf.Rad2Deg;
            Assert.That(Mathf.DeltaAngle(sweep.Effect.transform.eulerAngles.z, expected), Is.EqualTo(0f).Within(0.5f), "바라보는 방향으로 회전");

            yield return Wait(data.comboDuration + 0.1f);
            Assert.IsFalse(sweep.Effect.gameObject.activeInHierarchy, "콤보가 끝나면 숨김");
        }
    }
}
