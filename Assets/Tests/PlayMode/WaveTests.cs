using System.Collections;
using System.Collections.Generic;
using Game.Enemies;
using Game.Progression;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [웨이브 9-B절] 시간 → 웨이브 전환과 문구, 웨이브별 묶음 수, 구성 비율, 이동속도 상한, 일시정지 중 정지,
    /// 조기 소환 버튼(6분 전 없음 / 이후 클릭 → 즉시 보스 단계, 스폰 설정 변경 / 10분 자동 등장 시 숨김).
    /// 시간을 기다리지 않도록 WaveDirector.Advance로 해당 시각까지 건너뛴다.
    /// </summary>
    public class WaveTests
    {
        readonly InGameScene scene = new();
        readonly List<int> waveEvents = new();
        readonly List<(bool early, float at)> bossEvents = new();
        WaveDirector waves;
        WaveTable table;
        EnemySpawner spawner;

        [UnitySetUp]
        public IEnumerator Load()
        {
            waveEvents.Clear();
            bossEvents.Clear();
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            spawner = scene.Spawner;
            waves = spawner.Waves;
            Assert.IsNotNull(waves, "스포너에 WaveDirector가 없다");
            table = waves.Table;
            Assert.IsNotNull(table, "WaveDirector에 WaveTable이 없다");
            waves.WaveChanged += n => waveEvents.Add(n);
            waves.BossPhaseStarted += (early, at) => bossEvents.Add((early, at));
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        int CountArchers()
        {
            int count = 0;
            foreach (var enemy in EnemyRegistry.Active)
                if (enemy.GetComponent<ArcherBehaviour>() != null)
                    count++;
            return count;
        }

        [Test]
        public void W0_TableAsset_FiveWavesOfTwoMinutes_BossAt10_SummonAt6()
        {
            Assert.AreEqual(5, table.WaveCount);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(i * 120f, table.waves[i].startSeconds, 0.001f);
            Assert.AreEqual(600f, table.bossSeconds);
            Assert.AreEqual(360f, table.earlySummonSeconds);
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 2, 3 }, System.Array.ConvertAll(table.waves, w => w.settings.groupSize));
            Assert.AreEqual(1, table.bossPhase.groupSize, "보스전은 웨이브 2 수준");
        }

        [UnityTest]
        public IEnumerator W0b_OneRunClock_SurvivalTimeAndSpawnCurveFollowWaves()
        {
            var waves = scene.Spawner.Waves;
            var stats = Object.FindFirstObjectByType<RunStats>();
            yield return null;
            yield return null;
            Assert.Greater(waves.Elapsed, 0f);
            Assert.AreEqual(waves.Elapsed, stats.Elapsed, "생존 시간 = 판 시계");
            Assert.AreEqual(waves.Elapsed, scene.Spawner.Elapsed, "스폰 곡선 = 판 시계");

            waves.Advance(361f); // 시간을 건너뛰어도
            Assert.AreEqual(361f, stats.Elapsed, 0.001f, "생존 시간도 같이 건너뜀 (보스 소환 시각과 어긋나지 않게)");
            Assert.AreEqual(361f, scene.Spawner.Elapsed, 0.001f, "스폰 곡선도 같은 시각");
        }

        [UnityTest]
        public IEnumerator W1_WaveChangesAtBoundary_ShowsBanner_BossAt10Minutes()
        {
            var banner = Object.FindFirstObjectByType<WaveBanner>();
            Assert.IsNotNull(banner, "HUD에 WaveBanner가 없다");
            yield return null;
            Assert.IsFalse(banner.IsShowing, "시작할 때는 문구 없음");

            waves.Advance(119.9f);
            Assert.AreEqual(1, waves.CurrentWave);
            waves.Advance(120f);
            Assert.AreEqual(2, waves.CurrentWave);
            CollectionAssert.AreEqual(new[] { 2 }, waveEvents);
            Assert.IsTrue(banner.IsShowing);
            Assert.AreEqual("제 2파", banner.Label);

            float end = Time.time + table.bannerSeconds + 0.2f;
            while (Time.time < end)
                yield return null;
            Assert.IsFalse(banner.IsShowing, "2초 뒤 사라짐");

            waves.Advance(599.9f);
            Assert.IsFalse(waves.IsBossPhase);
            waves.Advance(600f);
            Assert.IsTrue(waves.IsBossPhase, "10:00 = 보스 단계");
            Assert.AreEqual(1, bossEvents.Count);
            Assert.IsFalse(bossEvents[0].early);
        }

        [UnityTest]
        public IEnumerator W2_GroupSize_Wave4Two_Wave5Three_SpawnedOutsideView()
        {
            Assert.AreEqual(1, spawner.SpawnGroup(), "웨이브 1: 1마리");
            spawner.ReleaseAll();
            waves.Advance(360f);
            Assert.AreEqual(2, spawner.SpawnGroup(), "웨이브 4: 2마리");
            spawner.ReleaseAll();
            waves.Advance(480f);
            Assert.AreEqual(3, spawner.SpawnGroup(), "웨이브 5: 3마리");

            Vector2 player = scene.Player.transform.position;
            foreach (var enemy in EnemyRegistry.Active)
                Assert.GreaterOrEqual(Vector2.Distance(enemy.transform.position, player), spawner.MinSpawnRadius - 0.01f, "묶음도 화면 밖");
            yield return null;
        }

        [UnityTest]
        public IEnumerator W3_Composition_NoArchersInWave1_ArchersFromWave2()
        {
            Random.InitState(1234);
            for (int i = 0; i < 150; i++)
                spawner.Spawn();
            Assert.AreEqual(0, CountArchers(), "웨이브 1: 도병만");
            spawner.ReleaseAll();

            waves.Advance(120f);
            for (int i = 0; i < 250; i++)
                spawner.Spawn();
            float ratio = CountArchers() / 250f;
            Assert.That(ratio, Is.InRange(0.17f, 0.33f), $"웨이브 2: 도병 75 : 궁수 25 (실제 {ratio:P0})");
            yield return null;
        }

        [UnityTest]
        public IEnumerator W4_EnemySpeed_WaveMultiplier_NeverAboveCap()
        {
            float cap = scene.Player.MoveSpeed * table.enemySpeedCapRatio;
            var melee = spawner.Spawn(spawner.BasePrefab);
            Assert.AreEqual(melee.Data.moveSpeed, melee.MoveSpeed, 0.0001f, "웨이브 1: ×1.0");

            waves.Advance(480f);
            var fast = spawner.Spawn(spawner.BasePrefab);
            float expected = Mathf.Min(melee.Data.moveSpeed * 1.2f, cap);
            Assert.AreEqual(expected, fast.MoveSpeed, 0.0001f, "웨이브 5: ×1.2");
            Assert.AreEqual(melee.Data.moveSpeed, melee.MoveSpeed, 0.0001f, "이미 나와 있는 적은 이전 속도 유지");

            Random.InitState(7);
            for (int i = 0; i < 60; i++)
                spawner.SpawnGroup();
            foreach (var enemy in EnemyRegistry.Active)
                Assert.LessOrEqual(enemy.MoveSpeed, cap + 0.0001f, "상한 = 플레이어 기본 × 0.7");
            yield return null;
        }

        [UnityTest]
        public IEnumerator W5_Pause_StopsWaveTime()
        {
            yield return null;
            Time.timeScale = 0f;
            float before = waves.Elapsed;
            float end = Time.realtimeSinceStartup + 0.3f;
            while (Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(before, waves.Elapsed, "일시정지 중 웨이브 시간 정지");
        }

        [UnityTest]
        public IEnumerator W6_EarlySummon_HiddenBefore6Min_ClickStartsBossPhase_ThenHidden()
        {
            var summon = Object.FindFirstObjectByType<BossSummonButton>();
            Assert.IsNotNull(summon, "HUD에 BossSummonButton이 없다");
            yield return null;
            Assert.IsFalse(summon.IsVisible, "6:00 전 버튼 없음");
            Assert.IsFalse(waves.SummonBossEarly(), "6:00 전에는 소환 불가");

            waves.Advance(359f);
            yield return null;
            Assert.IsFalse(summon.IsVisible);
            waves.Advance(360f);
            yield return null;
            Assert.IsTrue(summon.IsVisible, "6:00부터 버튼");

            summon.Button.onClick.Invoke();
            Assert.IsTrue(waves.IsBossPhase, "클릭 즉시 보스 단계 (확인창 없음)");
            Assert.AreEqual(1, bossEvents.Count);
            Assert.IsTrue(bossEvents[0].early, "조기 소환");
            Assert.That(waves.BossPhaseStartedAt, Is.InRange(360f, 361f), "경과 시간 기록");
            Assert.AreSame(table.bossPhase, waves.CurrentSettings, "이후 스폰은 보스전 설정");
            Assert.AreEqual(1, spawner.CurrentGroupSize);
            yield return null;
            Assert.IsFalse(summon.IsVisible, "보스 단계에서는 숨김");

            waves.Advance(600f);
            Assert.AreEqual(1, bossEvents.Count, "10:00에 다시 등장하지 않음");
        }

        [UnityTest]
        public IEnumerator W7_AutoBossAt10Min_HidesButton_SpawnIntervalFixed()
        {
            var summon = Object.FindFirstObjectByType<BossSummonButton>();
            waves.Advance(400f);
            yield return null;
            Assert.IsTrue(summon.IsVisible);
            float lateInterval = spawner.CurrentInterval; // 스포너 경과 시간은 0이라 곡선 시작값

            waves.Advance(600f);
            yield return null;
            Assert.IsFalse(summon.IsVisible, "자동 등장 시 버튼 숨김");
            Assert.IsFalse(waves.BossSummonedEarly);
            Assert.AreEqual(4, waves.CurrentWave, "남은 웨이브(5)는 건너뛰고 보스로");
            CollectionAssert.DoesNotContain(waveEvents, 5, "제 5파 문구 없음");
            Assert.AreNotEqual(lateInterval, spawner.CurrentInterval, "보스전 간격은 고정값(웨이브 2 구간)으로 바뀐다");
            Assert.IsFalse(waves.SummonBossEarly(), "보스 단계 후 다시 소환 불가");
        }

        [UnityTest]
        public IEnumerator W8_SummonWhilePaused_Ignored()
        {
            waves.Advance(400f);
            Time.timeScale = 0f;
            Assert.IsFalse(waves.SummonBossEarly(), "일시정지 중에는 소환 안 됨");
            Assert.IsFalse(waves.IsBossPhase);
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
