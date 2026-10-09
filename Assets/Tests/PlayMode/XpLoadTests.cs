using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [XP 부하 테스트] 활성 구슬 2,000개 + 플레이어 이동 + 자석 동작 중 프레임 시간 측정 (설계서 6-A절).
    /// 구슬은 스스로 계산하지 않으므로 구슬 수가 늘어도 프레임 시간이 거의 변하지 않아야 한다.
    /// 측정값은 콘솔에 "[XpLoadTest]"로 남겨 작업정리·발표 자료에 옮긴다.
    /// </summary>
    public class XpLoadTests
    {
        const int OrbCount = 2000;
        const int WarmupFrames = 30;
        const int MeasureFrames = 600;
        const float FrameBudgetMs = 1000f / 60f; // ※ 목표 60FPS

        readonly InGameScene scene = new();

        [UnityTest]
        public IEnumerator Load_2000Orbs_WhilePlayerMovesAndMagnetRuns()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            if (scene.Weapon != null)
                scene.Weapon.enabled = false;
            scene.LevelUpFlow.AutoChooseForPrototype = true;

            // 기준선: 구슬 없이 같은 경로를 돌 때의 프레임 시간
            var baseline = new FrameStats();
            yield return MovePlayerAndMeasure(baseline);

            // 플레이어가 도는 원 경로 주변을 포함해 넓게 흩뿌린다. 값 0 → 습득해도 레벨업으로 멈추지 않음.
            var random = new System.Random(1234);
            for (int i = 0; i < OrbCount; i++)
            {
                var pos = new Vector2((float)(random.NextDouble() * 80 - 40), (float)(random.NextDouble() * 80 - 40));
                scene.XpDropper.SpawnOrb(pos, 0, Color.white);
            }
            int activeAtStart = scene.XpDropper.Pool.CountActive;
            Assert.AreEqual(OrbCount, activeAtStart);

            var loaded = new FrameStats();
            yield return MovePlayerAndMeasure(loaded);
            int collected = activeAtStart - scene.XpDropper.Pool.CountActive;

            Debug.Log($"[XpLoadTest] orbs={OrbCount} frames={MeasureFrames} collected={collected} | " +
                      $"baseline avg={baseline.AvgMs:F2}ms worst={baseline.WorstMs:F2}ms | " +
                      $"2000 orbs avg={loaded.AvgMs:F2}ms worst={loaded.WorstMs:F2}ms | " +
                      $"orbs+magnet logic avg={loaded.AvgLogicMs:F3}ms worst={loaded.WorstLogicMs:F3}ms");

            Assert.Greater(collected, 0, "이동 경로의 구슬은 자석으로 습득되어야 한다");
            Assert.Less(loaded.AvgMs, FrameBudgetMs, "평균 프레임 시간이 60FPS 예산 안이어야 한다");
        }

        /// <summary>플레이어를 반경 15 원 위로 돌리며 프레임 시간을 잰다. 로직 시간은 자석 Update 구간만 따로 잰다.</summary>
        IEnumerator MovePlayerAndMeasure(FrameStats stats)
        {
            var body = scene.Player.GetComponent<Rigidbody2D>();
            var magnet = scene.Magnet;
            var watch = new System.Diagnostics.Stopwatch();
            float angle = 0f;

            for (int frame = 0; frame < WarmupFrames + MeasureFrames; frame++)
            {
                angle += Time.deltaTime * 0.6f;
                var pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 15f;
                body.position = pos;
                scene.Player.transform.position = pos;

                yield return null;

                if (frame < WarmupFrames)
                    continue;

                // 자석 1프레임 비용을 별도로 측정 (구슬 수와 무관해야 함)
                watch.Restart();
                magnet.SendMessage("Update");
                watch.Stop();

                stats.Add(Time.unscaledDeltaTime * 1000f, (float)watch.Elapsed.TotalMilliseconds);
            }
        }

        class FrameStats
        {
            float totalMs, totalLogicMs;
            int count;

            public float WorstMs { get; private set; }
            public float WorstLogicMs { get; private set; }
            public float AvgMs => count > 0 ? totalMs / count : 0f;
            public float AvgLogicMs => count > 0 ? totalLogicMs / count : 0f;

            public void Add(float frameMs, float logicMs)
            {
                totalMs += frameMs;
                totalLogicMs += logicMs;
                count++;
                WorstMs = Mathf.Max(WorstMs, frameMs);
                WorstLogicMs = Mathf.Max(WorstLogicMs, logicMs);
            }
        }
    }
}
