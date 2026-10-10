using System.Collections;
using System.Collections.Generic;
using Game.Enemies;
using Game.Player;
using Game.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [8-A절 혈영궁수] 사거리 밖 접근·안에서 정지, 0.5초 떨림 후 그 순간 플레이어 위치로 발사, 2초 간격,
    /// 떨림 중 이탈 시 취소, 화살 1회 데미지·다른 적 통과·비행 거리 끝 소멸, 일시정지 중 정지, 레벨 기준 스폰 조합.
    /// </summary>
    public class ArcherTests
    {
        readonly InGameScene scene = new();
        readonly List<(float time, Arrow arrow)> shots = new();
        Enemy archerPrefab;
        PlayerHealth health;

        [UnitySetUp]
        public IEnumerator Load()
        {
            shots.Clear();
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            health = scene.Player.GetComponent<PlayerHealth>();
            Assert.IsNotNull(scene.Spawner.Composition, "스포너에 적 조합 데이터가 없다");
            archerPrefab = scene.Spawner.Composition.entries[0].prefab;
            Assert.IsNotNull(archerPrefab.GetComponent<ArcherBehaviour>(), "조합 첫 항목이 궁수가 아니다");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
                yield return null;
        }

        ArcherBehaviour SpawnArcher(Vector2 offset)
        {
            var archer = scene.SpawnAt(offset, chase: true, archerPrefab).GetComponent<ArcherBehaviour>();
            archer.Fired += a => shots.Add((Time.time, a));
            return archer;
        }

        void MovePlayer(Vector2 position)
        {
            scene.Player.transform.position = position;
            scene.Player.GetComponent<Rigidbody2D>().position = position;
        }

        [UnityTest]
        public IEnumerator A1_ApproachesUntilInRange_ThenStops()
        {
            var archer = SpawnArcher(new Vector2(6f, 0f));
            var data = archer.Data;
            Assert.AreEqual(3f, data.attackRange);

            yield return Wait(0.3f);
            float d0 = Vector2.Distance(archer.transform.position, scene.Player.transform.position);
            Assert.Less(d0, 6f, "사거리 밖이면 다가온다");

            yield return WaitUntil(() => archer.IsWindingUp, 4f);
            float dStop = Vector2.Distance(archer.transform.position, scene.Player.transform.position);
            Assert.LessOrEqual(dStop, data.attackRange + 0.05f, "사거리 안에서 멈추고 공격 시작");
            yield return Wait(0.3f);
            Assert.That(Vector2.Distance(archer.transform.position, scene.Player.transform.position), Is.EqualTo(dStop).Within(0.05f), "사거리 안에서는 멈춰 있다");
        }

        [UnityTest]
        public IEnumerator A2_WindupHalfSecond_ThenFiresAtPlayerPosition_AtSetSpeed()
        {
            var archer = SpawnArcher(new Vector2(2.5f, 0f));
            yield return new WaitForFixedUpdate(); // 행동은 물리 스텝에서 돈다
            Assert.IsTrue(archer.IsWindingUp, "사거리 안: 바로 떨림 시작");
            float windupStart = Time.time;

            yield return WaitUntil(() => shots.Count > 0, 1f);
            Assert.AreEqual(1, shots.Count);
            Assert.That(shots[0].time - windupStart, Is.EqualTo(0.5f).Within(0.08f), "0.5초 떨림 후 발사");

            var toPlayer = ((Vector2)(scene.Player.transform.position - archer.transform.position)).normalized;
            Assert.Greater(Vector2.Dot(shots[0].arrow.Velocity.normalized, toPlayer), 0.99f, "쏘는 순간의 플레이어 위치로");
            float expectedSpeed = scene.Player.MoveSpeed * archer.Data.arrowSpeedMultiplier;
            Assert.That(shots[0].arrow.Velocity.magnitude, Is.EqualTo(expectedSpeed).Within(0.01f), "플레이어 최고 속도 × 1.3");
        }

        [UnityTest]
        public IEnumerator A3_FiresEveryTwoSeconds_AndArrowHitsOnceForEight()
        {
            SpawnArcher(new Vector2(2.5f, 0f));
            yield return WaitUntil(() => shots.Count >= 1, 1f);
            float hpAfterFirstShot = health.CurrentHp;

            yield return WaitUntil(() => !shots[0].arrow.IsLive, 2f);
            Assert.That(hpAfterFirstShot - health.CurrentHp, Is.EqualTo(8f).Within(0.01f), "화살 1발 = 데미지 8 (1회)");

            yield return WaitUntil(() => shots.Count >= 2, 3f);
            Assert.AreEqual(2, shots.Count);
            Assert.That(shots[1].time - shots[0].time, Is.EqualTo(2f).Within(0.1f), "발사 간격 2초");
        }

        [UnityTest]
        public IEnumerator A4_PlayerLeavesDuringWindup_ShotCancelled_ArcherFollows()
        {
            var archer = SpawnArcher(new Vector2(2.5f, 0f));
            yield return new WaitForFixedUpdate(); // 행동은 물리 스텝에서 돈다
            Assert.IsTrue(archer.IsWindingUp);

            MovePlayer((Vector2)scene.Player.transform.position + new Vector2(-6f, 0f));
            yield return Wait(0.7f);
            Assert.IsEmpty(shots, "떨림 도중 사거리를 벗어나면 그 발은 취소");
            Assert.IsFalse(archer.IsWindingUp);
            Assert.Less(archer.GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "다시 플레이어를 따라간다");
        }

        [UnityTest]
        public IEnumerator A5_ArrowPassesThroughOtherEnemies()
        {
            var blocker = scene.SpawnAt(new Vector2(1.2f, 0f), chase: false);
            SpawnArcher(new Vector2(2.8f, 0f));
            float startHp = health.CurrentHp;
            yield return WaitUntil(() => shots.Count > 0, 1f);
            yield return WaitUntil(() => !shots[0].arrow.IsLive, 2f);

            Assert.AreEqual(blocker.Data.maxHp, blocker.CurrentHp, "가운데 적은 화살에 맞지 않는다");
            Assert.Less(health.CurrentHp, startHp, "화살은 적을 지나 플레이어에게 닿는다");
        }

        [UnityTest]
        public IEnumerator A6_MissedArrow_DisappearsAtRangeEnd()
        {
            var archer = SpawnArcher(new Vector2(2.5f, 0f));
            yield return WaitUntil(() => shots.Count > 0, 1f);
            var arrow = shots[0].arrow;
            // 쏜 직후 옆으로 비킨다 (조준은 쏘는 순간의 위치라 이제 빗나간다). 사거리 안이라 궁수는 그대로 있다.
            MovePlayer((Vector2)scene.Player.transform.position + new Vector2(0f, 1.5f));
            float startHp = health.CurrentHp;
            Vector2 start = arrow.transform.position;
            float farthest = 0f;
            while (arrow.IsLive)
            {
                farthest = Mathf.Max(farthest, Vector2.Distance(start, arrow.transform.position));
                yield return null;
            }
            Assert.AreEqual(startHp, health.CurrentHp, "피하면 데미지 없음");
            Assert.That(farthest, Is.EqualTo(archer.Data.ArrowTravelDistance).Within(0.3f), "비행 거리 = 사거리");
        }

        [UnityTest]
        public IEnumerator A7_Pause_StopsArrowAndWindup()
        {
            var archer = SpawnArcher(new Vector2(2.5f, 0f));
            yield return WaitUntil(() => shots.Count > 0, 1f);
            var arrow = shots[0].arrow;
            Vector2 before = arrow.transform.position;

            Time.timeScale = 0f;
            float end = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(before, (Vector2)arrow.transform.position, "일시정지 중 화살 정지");
            Assert.AreEqual(1, shots.Count, "일시정지 중 발사 없음");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator A8_Composition_ByPlayerLevel_NoArchersAtLv1_AboutThirtyPercentAtMax()
        {
            for (int i = 0; i < 100; i++)
                scene.Spawner.Spawn();
            Assert.AreEqual(0, scene.Spawner.PoolOf(archerPrefab).CountActive, "Lv1: 궁수 없음");
            scene.Spawner.ReleaseAll();

            var level = scene.Player.GetComponent<PlayerLevel>();
            while (level.Level < 10)
                level.AddXp(level.RequiredXp); // 카드 선택 대기는 스폰 비율과 무관하므로 그대로 둔다
            Assert.AreEqual(10, scene.Spawner.Level);

            Random.InitState(1234);
            for (int i = 0; i < 300; i++)
                scene.Spawner.Spawn();
            float ratio = scene.Spawner.PoolOf(archerPrefab).CountActive / 300f;
            Assert.That(ratio, Is.InRange(0.22f, 0.38f), $"최대 레벨 근거리 7 : 궁수 3 (실제 {ratio:P0})");
            yield return null;
        }
    }
}
