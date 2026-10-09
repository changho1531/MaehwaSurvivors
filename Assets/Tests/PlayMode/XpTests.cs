using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[XP·레벨] 구슬 드롭·유지·자석 습득, LevelUp 전이·연속 카드·입력 잠금, 성장 완료 (설계서 6-A절).</summary>
    public class XpTests
    {
        readonly InGameScene scene = new();

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            Assert.IsNotNull(scene.XpDropper, "InGame 씬에 XpDropper가 없다");
            Assert.IsNotNull(scene.Magnet, "Player에 PlayerMagnet이 없다");
            Assert.IsNotNull(scene.Level, "Player에 PlayerLevel이 없다");
            Assert.IsNotNull(scene.LevelUpFlow, "InGame 씬에 LevelUpFlow가 없다");

            scene.StopSpawningAndClear();
            if (scene.Weapon != null)
                scene.Weapon.enabled = false;

        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        XpSettings Settings => scene.XpDropper.Settings;
        Vector2 PlayerPos => scene.Player.transform.position;
        GameState State => GameManager.Instance.CurrentState;

        static IEnumerator WaitGame(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        static List<XpOrb> ActiveOrbs() => new(Object.FindObjectsByType<XpOrb>(FindObjectsSortMode.None));

        XpOrb SpawnOrbAt(Vector2 offset, int value = 1)
        {
            return scene.XpDropper.SpawnOrb(PlayerPos + offset, value, Color.white);
        }

        [UnityTest]
        public IEnumerator X1_KilledEnemy_DropsOneOrb_AfterDelay_AboveDeathPosition()
        {
            var enemy = scene.SpawnAt(new Vector2(8f, 0f), chase: false);
            yield return null;
            var deathPos = enemy.transform.position;
            var data = enemy.Data;

            enemy.TakeDamage(data.maxHp);
            Assert.AreEqual(0, ActiveOrbs().Count, "적이 사라진 즉시가 아니라 잠시 뒤에 생겨야 한다");
            Assert.AreEqual(1, scene.XpDropper.PendingCount);

            yield return WaitGame(Settings.spawnDelay * 0.5f);
            Assert.AreEqual(0, ActiveOrbs().Count, "지연 시간 전에는 생성되지 않음");

            yield return WaitGame(Settings.spawnDelay * 0.5f + 0.1f);
            var orbs = ActiveOrbs();
            Assert.AreEqual(1, orbs.Count, "적 1마리당 구슬 1개");

            var orb = orbs[0];
            var expected = deathPos + Vector3.up * Settings.spawnOffsetY;
            Assert.That(Vector2.Distance(orb.transform.position, expected), Is.LessThan(0.01f), "사망 위치 위쪽에 생성");
            Assert.AreEqual(data.xpAmount, orb.Value, "값은 적 데이터에서");
            Assert.AreEqual(data.xpOrbColor, orb.GetComponent<SpriteRenderer>().color, "색은 적 데이터에서");
            Assert.AreEqual(LayerMask.NameToLayer(GameLayers.Xp), orb.gameObject.layer, "XP 전용 레이어");
            Assert.IsTrue(orb.GetComponent<Collider2D>().isTrigger, "구슬은 트리거 콜라이더");
            Assert.AreEqual("[Pool] XpOrbs", orb.transform.parent?.name, "풀에서 나온 구슬");
        }

        [UnityTest]
        public IEnumerator X2_OrbOutsideMagnet_StaysForever()
        {
            var orb = SpawnOrbAt(new Vector2(Settings.magnetRadius + 4f, 0f));
            var pos = orb.transform.position;

            yield return WaitGame(3f);

            Assert.IsTrue(orb.gameObject.activeSelf, "습득 전까지 사라지지 않는다");
            Assert.AreEqual(pos, orb.transform.position, "반경 밖 구슬은 움직이지 않는다");
        }

        [UnityTest]
        public IEnumerator X3_OrbInsideMagnet_IsPulledAndCollected()
        {
            var near = SpawnOrbAt(new Vector2(Settings.magnetRadius * 0.6f, 0f), value: 3);
            var far = SpawnOrbAt(new Vector2(Settings.magnetRadius + 3f, 0f), value: 5);
            var farPos = far.transform.position;
            int xpBefore = scene.Level.CurrentXp;

            float timeout = Time.time + 1.5f;
            while (near.gameObject.activeSelf && Time.time < timeout)
                yield return null;

            Assert.IsFalse(near.gameObject.activeSelf, "반경 안 구슬은 끌려와 습득되어야 한다");
            Assert.IsFalse(scene.XpDropper.Pool.IsActive(near), "습득된 구슬은 풀 반환");
            Assert.AreEqual(xpBefore + 3, scene.Level.CurrentXp, "구슬 값만큼 경험치 증가");
            Assert.IsTrue(far.gameObject.activeSelf);
            Assert.AreEqual(farPos, far.transform.position, "반경 밖 구슬은 그대로");
        }

        [UnityTest]
        public IEnumerator X3_PullSpeed_IsFasterThanPlayer()
        {
            yield return null;
            Assert.Greater(scene.Magnet.PullSpeed, scene.Player.MoveSpeed, "끌림 속도는 항상 플레이어 최고 속도보다 빨라야 한다");
        }

        [UnityTest]
        public IEnumerator X4_LevelUp_PausesEverything_AndChoiceReturnsToPlay()
        {
            Assert.AreEqual(GameState.Play, State);
            scene.Magnet.enabled = false; // 레벨업 직전까지 구슬이 끌려오지 않게
            var orb = SpawnOrbAt(new Vector2(Settings.magnetRadius * 0.8f, 0f));
            yield return null;
            var orbPos = orb.transform.position;
            int orbValue = orb.Value;
            scene.Level.AddXp(scene.Level.RequiredXp);
            scene.Magnet.enabled = true;

            Assert.AreEqual(GameState.LevelUp, State, "XP 기준치 도달 → LevelUp");
            Assert.AreEqual(0f, Time.timeScale, "LevelUp 중 게임 일시정지");
            Assert.IsTrue(scene.LevelUpFlow.IsAwaitingChoice);

            // 일시정지 중 적이 죽어도 구슬 생성 대기 타이머가 흐르지 않는다.
            var enemy = scene.SpawnAt(new Vector2(-8f, 0f), chase: false);
            enemy.TakeDamage(enemy.Data.maxHp);
            int orbsBefore = ActiveOrbs().Count;

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(orbPos, orb.transform.position, "카드 선택 중에는 자석도 정지");
            Assert.AreEqual(1, scene.XpDropper.PendingCount, "생성 대기 타이머도 정지");
            Assert.AreEqual(orbsBefore, ActiveOrbs().Count);

            Assert.IsTrue(scene.LevelUpFlow.Choose(0));
            Assert.AreEqual(GameState.Play, State, "대기 0 → Play 복귀");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, scene.Level.PendingLevelUps);
            int xpAfterChoice = scene.Level.CurrentXp;

            yield return WaitGame(0.6f);
            Assert.AreEqual(0, scene.XpDropper.PendingCount, "재개 후 남은 시간만큼 기다렸다 생성");
            // 습득된 구슬 인스턴스는 풀로 돌아가 방금 생성된 드롭에 재사용될 수 있으므로 활성 여부 대신 경험치로 확인한다.
            Assert.AreEqual(xpAfterChoice + orbValue, scene.Level.CurrentXp, "재개 후 자석 동작 → 반경 안 구슬 습득");
        }

        [UnityTest]
        public IEnumerator X5_SeveralPendingLevels_ShowCardsBackToBack_InsideLevelUp()
        {
            var transitions = new List<(GameState, GameState)>();
            void OnChanged(GameState a, GameState b) => transitions.Add((a, b));
            int cardRequests = 0;
            void OnCards(IReadOnlyList<LevelUpCard> _) => cardRequests++;

            GameManager.Instance.StateMachine.StateChanged += OnChanged;
            scene.LevelUpFlow.CardsShown += OnCards;
            try
            {
                scene.Level.AddXp(scene.Level.RequiredXp);
                scene.Level.AddXp(scene.Level.RequiredXp); // 카드가 떠 있는 동안 한 번 더 오름 → 대기 2
                Assert.AreEqual(2, scene.Level.PendingLevelUps);
                Assert.AreEqual(1, cardRequests);

                yield return new WaitForSecondsRealtime(Settings.cardInputLockSeconds + 0.05f);
                Assert.IsTrue(scene.LevelUpFlow.Choose(0));
                Assert.AreEqual(GameState.LevelUp, State, "대기가 남아 있으면 Play로 나가지 않는다");
                Assert.AreEqual(2, cardRequests, "다음 카드를 바로 요청");

                yield return new WaitForSecondsRealtime(Settings.cardInputLockSeconds + 0.05f);
                Assert.IsTrue(scene.LevelUpFlow.Choose(0));
                Assert.AreEqual(GameState.Play, State);

                CollectionAssert.AreEqual(new[] { (GameState.Play, GameState.LevelUp), (GameState.LevelUp, GameState.Play) }, transitions,
                    "LevelUp 진입·복귀는 각각 한 번씩만");
            }
            finally
            {
                GameManager.Instance.StateMachine.StateChanged -= OnChanged;
                scene.LevelUpFlow.CardsShown -= OnCards;
            }
        }

        [UnityTest]
        public IEnumerator X6_NewCards_IgnoreInput_ForLockTime()
        {
            scene.Level.AddXp(scene.Level.RequiredXp);
            Assert.IsFalse(scene.LevelUpFlow.Choose(0), "카드가 뜬 직후 클릭은 무시");

            yield return new WaitForSecondsRealtime(Settings.cardInputLockSeconds * 0.4f);
            Assert.IsFalse(scene.LevelUpFlow.Choose(0), "잠금 시간 동안 계속 무시");
            Assert.AreEqual(GameState.LevelUp, State);

            yield return new WaitForSecondsRealtime(Settings.cardInputLockSeconds * 0.6f + 0.05f);
            Assert.IsTrue(scene.LevelUpFlow.Choose(0), "잠금 해제 후에는 선택 가능 (일시정지 중에도 실시간으로 풀림)");
        }

        [UnityTest]
        public IEnumerator X7_GrowthComplete_ClearsOrbs_StopsDrops_AndFixesBarAtMax()
        {
            SpawnOrbAt(new Vector2(8f, 0f));
            SpawnOrbAt(new Vector2(-8f, 3f));
            var enemy = scene.SpawnAt(new Vector2(0f, 8f), chase: false);
            enemy.TakeDamage(enemy.Data.maxHp);
            Assert.AreEqual(1, scene.XpDropper.PendingCount);

            scene.XpDropper.StopDropsAndClear();
            Assert.AreEqual(0, scene.XpDropper.PendingCount, "생성 대기 중이던 구슬 취소");
            Assert.AreEqual(0, ActiveOrbs().Count, "맵의 구슬 전부 제거");
            Assert.AreEqual(0, scene.XpDropper.Pool.CountActive, "제거 = 풀 반환");

            var another = scene.SpawnAt(new Vector2(0f, -8f), chase: false);
            another.TakeDamage(another.Data.maxHp);
            yield return WaitGame(Settings.spawnDelay + 0.2f);
            Assert.AreEqual(0, ActiveOrbs().Count, "이후 적이 죽어도 구슬 없음");

            int changedCur = -1, changedReq = -1;
            bool maxed = false;
            scene.Level.XpChanged += (c, r) => { changedCur = c; changedReq = r; };
            scene.Level.Maxed += () => maxed = true;
            scene.Level.AddXp(1);
            scene.Level.MarkMaxed();

            Assert.IsTrue(maxed, "MAX 표시용 이벤트");
            Assert.AreEqual(changedReq, changedCur, "경험치 바는 꽉 찬 상태로 고정");
            Assert.AreEqual(0, scene.Level.PendingLevelUps, "남은 대기 폐기");
            int level = scene.Level.Level;
            scene.Level.AddXp(100000);
            Assert.AreEqual(level, scene.Level.Level);
            Assert.AreEqual(GameState.Play, State, "성장 완료 후에는 레벨업이 일어나지 않는다");
        }

        [UnityTest]
        public IEnumerator X8_OrbPool_IsPrewarmed_AndGrowsWithoutLimit()
        {
            var pool = scene.XpDropper.Pool;
            int created = pool.TotalCreated;
            Assert.GreaterOrEqual(created, Settings.prewarmCount, "프리웜");

            for (int i = 0; i < 50; i++)
                SpawnOrbAt(new Vector2(20f + i, 20f));
            Assert.AreEqual(created, pool.TotalCreated, "프리웜 이내에서는 새로 만들지 않음");

            for (int i = 0; i < Settings.prewarmCount; i++)
                SpawnOrbAt(new Vector2(20f + i * 0.1f, 25f));
            Assert.AreEqual(50 + Settings.prewarmCount, pool.CountActive, "상한 없이 확장");
            yield return null;
        }
    }
}
