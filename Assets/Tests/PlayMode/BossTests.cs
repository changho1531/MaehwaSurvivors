using System.Collections;
using System.Collections.Generic;
using Game.CameraSystem;
using Game.Core;
using Game.Enemies;
using Game.Enemies.Boss;
using Game.Player;
using Game.Pooling;
using Game.Visuals;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [보스 혈각마존 8-C절] 등장(1마리, 화면 밖 위쪽, HP 바 · 문구), 화면 밖 패턴 없음, 거리별 패턴, 틈, 경고 방향 고정,
    /// 페이즈 전환(패턴 끝난 뒤 · 첫 패턴 지면 강타), 충격파 1회 판정, 혈풍선 · 돌진 · 혈기탄, 자하호신강 감소, 넉백 면역,
    /// 처치 → 정리 → GameClear(조기 소환 문구), 같은 프레임 사망은 클리어 우선.
    /// </summary>
    public class BossTests
    {
        readonly InGameScene scene = new();
        readonly List<string> started = new();
        BossSpawner bossSpawner;
        PlayerHealth health;
        Enemy boss;
        BossBehaviour behaviour;

        [UnitySetUp]
        public IEnumerator Load()
        {
            started.Clear();
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false; // 매화검기가 보스를 때리지 않게
            scene.Weapon.Pool?.ReleaseAll();
            health = scene.Player.GetComponent<PlayerHealth>();
            bossSpawner = Object.FindFirstObjectByType<BossSpawner>();
            Assert.IsNotNull(bossSpawner, "씬에 BossSpawner가 없다");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            PrefabPools.ReleaseAll();
            if (boss != null)
                Object.Destroy(boss.gameObject);
            boss = null;
            yield return null;
        }

        /// <summary>보스를 만들고 플레이어 기준 offset에 둔다 (화면 안).</summary>
        IEnumerator SpawnAt(Vector2 offset, float roll = -1f)
        {
            boss = bossSpawner.SpawnBoss();
            Assert.IsNotNull(boss);
            behaviour = boss.GetComponent<BossBehaviour>();
            behaviour.FixedRoll = roll; // 첫 물리 스텝에서 바로 고를 수 있으므로 그 전에
            behaviour.PatternStarted += n => started.Add(n);
            Place(offset);
            yield return new WaitForFixedUpdate();
        }

        void Place(Vector2 offset)
        {
            var p = (Vector2)scene.Player.transform.position + offset;
            boss.transform.position = p;
            boss.GetComponent<Rigidbody2D>().position = p;
        }

        void MovePlayer(Vector2 offset)
        {
            var body = scene.Player.GetComponent<Rigidbody2D>();
            body.position += offset;
            scene.Player.transform.position = body.position;
        }

        static IEnumerator WaitGameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float realtime)
        {
            float end = Time.realtimeSinceStartup + realtime;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator B1_BossPhase_SpawnsOneAboveScreen_ShowsHpBarAndBanner()
        {
            var waves = scene.Spawner.Waves;
            waves.Advance(361f);
            Assert.IsTrue(waves.SummonBossEarly());
            boss = bossSpawner.Boss;
            Assert.IsNotNull(boss, "BossPhaseStarted → 보스 생성");
            Assert.AreSame(boss, bossSpawner.SpawnBoss(), "한 번에 1마리만");
            Assert.Greater(boss.transform.position.y, scene.Player.transform.position.y + scene.Camera.orthographicSize, "화면 밖 위쪽");
            Assert.IsFalse(ScreenCheck.IsVisible(boss.transform.position));
            yield return null;

            var bar = Object.FindFirstObjectByType<BossHpBar>();
            Assert.IsTrue(bar.IsShown, "보스 HP 바 표시");
            Assert.AreEqual("혈각마존", bar.NameLabel);
            Assert.AreEqual(1f, bar.FillAmount, 0.001f);
            var banner = Object.FindFirstObjectByType<WaveBanner>();
            Assert.AreEqual("혈각마존 출현", banner.Label);
            Assert.IsTrue(Object.FindFirstObjectByType<BossAlertView>().IsFlashing, "화면 가장자리 붉은 번쩍임");

            boss.TakeDamage(boss.Data.maxHp * 0.25f);
            Assert.AreEqual(0.75f, bar.FillAmount, 0.001f);
        }

        [UnityTest]
        public IEnumerator B2_OffScreen_NoPatterns_WalksIn()
        {
            health.Invincible = true;
            boss = bossSpawner.SpawnBoss();
            behaviour = boss.GetComponent<BossBehaviour>();
            behaviour.PatternStarted += n => started.Add(n);
            float startY = boss.transform.position.y;
            Time.timeScale = 3f;
            yield return WaitUntil(() => ScreenCheck.IsVisible(boss.transform.position) || started.Count > 0, 5f);
            Time.timeScale = 1f;
            Assert.AreEqual(0, started.Count, "화면 밖에서는 패턴 없음");
            Assert.Less(boss.transform.position.y, startY, "걸어 들어온다");
        }

        [UnityTest]
        public IEnumerator B3_Near_Swing_Far_ChargeOrOrb_NoPhase2PatternsInPhase1()
        {
            health.Invincible = true;
            yield return SpawnAt(new Vector2(2f, 0f));
            yield return WaitUntil(() => started.Count > 0, 2f);
            Assert.AreEqual("휘두르기", started[0], "2.5칸 이하 → 휘두르기");

            yield return WaitUntil(() => behaviour.Current == null, 3f);
            started.Clear();
            Place(new Vector2(6f, 0f));
            yield return WaitUntil(() => started.Count > 0, 3f);
            Assert.That(started[0], Is.EqualTo("돌진").Or.EqualTo("혈기탄"), "4칸 이상 → 돌진 · 혈기탄");
            Assert.AreEqual(1, behaviour.Phase);
            CollectionAssert.DoesNotContain(started, "지면 강타");
            CollectionAssert.DoesNotContain(started, "혈풍선");
        }

        [UnityTest]
        public IEnumerator B4_GapAfterPattern_NoPatternFor08Seconds()
        {
            health.Invincible = true;
            yield return SpawnAt(new Vector2(2f, 0f));
            yield return WaitUntil(() => started.Count > 0, 2f);
            yield return WaitUntil(() => behaviour.Current == null, 3f);
            float endedAt = Time.time;
            Assert.IsTrue(behaviour.IsInGap, "패턴이 끝나면 틈");
            int count = started.Count;
            yield return WaitUntil(() => started.Count > count, 3f);
            Assert.GreaterOrEqual(Time.time - endedAt, 0.78f, "틈 0.8초 동안 패턴 없음");
        }

        [UnityTest]
        public IEnumerator B5_WarningDirection_LockedAtStart()
        {
            health.Invincible = true;
            yield return SpawnAt(new Vector2(2f, 0f));
            yield return WaitUntil(() => behaviour.Current != null && behaviour.Current.InWarning, 2f);
            var swing = (SwingPattern)behaviour.Current;
            var locked = swing.LockedDirection;
            Assert.Less(locked.x, -0.9f, "경고 시작 순간의 플레이어 쪽 (보스가 오른쪽에 있으므로 왼쪽)");

            MovePlayer(new Vector2(-4f, 3f)); // 경고 중 반대편으로
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(locked, swing.LockedDirection, "경고 방향은 바뀌지 않는다");
            Assert.AreEqual(locked, behaviour.Telegraph.Direction, "반원 표시도 그대로");
        }

        [UnityTest]
        public IEnumerator B6_HalfHp_FinishesPattern_ThenTransition_FirstGroundSlam()
        {
            health.Invincible = true;
            yield return SpawnAt(new Vector2(6f, 0f));
            int phaseEvents = 0;
            behaviour.PhaseChanged += _ => phaseEvents++;
            yield return WaitUntil(() => behaviour.Current != null, 2f);
            var running = behaviour.Current;

            boss.TakeDamage(boss.Data.maxHp * 0.55f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreSame(running, behaviour.Current, "진행 중 패턴은 끝까지");
            Assert.AreEqual(1, behaviour.Phase);

            Time.timeScale = 3f;
            yield return WaitUntil(() => behaviour.IsTransitioning, 4f);
            Assert.IsTrue(behaviour.IsTransitioning, "패턴이 끝난 뒤 전환 연출");
            int before = started.Count;
            yield return WaitUntil(() => started.Count > before, 4f);
            Time.timeScale = 1f;
            Assert.AreEqual(2, behaviour.Phase);
            Assert.AreEqual(1, phaseEvents);
            Assert.AreEqual("지면 강타", started[before], "2페이즈 첫 패턴은 거리와 상관없이 지면 강타");
        }

        [UnityTest]
        public IEnumerator B7_Shockwave_HitsOnceWhenPassing_NotWhenAlreadyInside()
        {
            yield return SpawnAt(new Vector2(30f, 0f)); // 화면 밖에 세워 둔다 (패턴 없음)
            var slam = (GroundSlamPatternData)System.Array.Find(behaviour.Data.patterns, p => p is GroundSlamPatternData);
            var pool = PrefabPools.For(slam.ringPrefab);

            // 3칸 밖에서: 고리가 지나갈 때 1회
            float hp = health.CurrentHp;
            var ring = pool.Get((Vector2)scene.Player.transform.position + new Vector2(3f, 0f));
            ring.Begin(6f, 1f, 0.5f, 15f, health, pool.Release);
            yield return WaitGameSeconds(1.2f);
            Assert.IsTrue(ring.HasHit || hp - health.CurrentHp > 0f);
            Assert.AreEqual(15f, hp - health.CurrentHp, 0.01f, "지나가는 순간 1회만");

            // 고리가 지나간 안쪽으로 들어가면 맞지 않음
            hp = health.CurrentHp;
            var center = (Vector2)scene.Player.transform.position + new Vector2(5f, 0f);
            var ring2 = pool.Get(center);
            ring2.Begin(6f, 1f, 0.5f, 15f, health, pool.Release);
            yield return WaitUntil(() => ring2.CurrentRadius >= 2.5f, 2f);
            Assert.IsFalse(ring2.HasHit);
            MovePlayer(center + new Vector2(1f, 0f) - (Vector2)scene.Player.transform.position);
            yield return WaitGameSeconds(1f);
            Assert.AreEqual(hp, health.CurrentHp, 0.01f, "이미 지나간 안쪽은 맞지 않음");
        }

        [UnityTest]
        public IEnumerator B8_Charge_EightCells_ThenRests()
        {
            health.Invincible = true;
            yield return SpawnAt(new Vector2(6f, 0f), roll: 0f); // 후보 [돌진, 혈기탄] 중 첫 번째
            yield return WaitUntil(() => behaviour.Current is ChargePattern, 3f);
            var charge = (ChargePattern)behaviour.Current;
            yield return WaitUntil(() => !charge.InWarning, 2f);
            Vector2 start = boss.GetComponent<Rigidbody2D>().position;
            yield return WaitUntil(() => charge.IsResting, 2f);
            yield return new WaitForFixedUpdate();
            float moved = (boss.GetComponent<Rigidbody2D>().position - start).magnitude;
            Assert.AreEqual(8f, moved, 0.6f, "직선 8칸");
            Assert.IsTrue(charge.HasHit, "경로 위 플레이어 1회 명중");
            Assert.Less(boss.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 0.01f, "숨 고르기 동안 멈춤");
        }

        [UnityTest]
        public IEnumerator B16_Art_WalkFrames_SwingEffectFacesAttack()
        {
            health.Invincible = true;
            boss = bossSpawner.SpawnBoss(); // 화면 밖에서 걸어 들어오는 중
            for (int i = 0; i < 5; i++)
                yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(boss.transform.Find("Art").GetComponent<FrameAnimation>().enabled, "걷는 동안 걷기 프레임");
            Object.Destroy(boss.gameObject);
            boss = null;
            yield return null;

            yield return SpawnAt(new Vector2(2f, 0f));
            yield return WaitUntil(() => behaviour.Current is SwingPattern swing && !swing.InWarning, 3f);
            var swingPattern = (SwingPattern)behaviour.Current;
            var swingFx = PrefabPools.For(swingPattern.Data.effectPrefab);
            Assert.AreEqual(1, swingFx.CountActive, "휘두르는 순간 이펙트 1개");
            var fx = FindEffect("SwingEffect_Boss");
            Assert.Greater(Vector2.Dot(fx.transform.right, swingPattern.LockedDirection), 0.99f, "이펙트가 공격 방향을 향함");
        }

        [UnityTest]
        public IEnumerator B17_ChargeTrail_FollowsBossWhileDashing()
        {
            health.Invincible = true;
            yield return SpawnAt(new Vector2(6f, 0f), roll: 0f); // 돌진
            yield return WaitUntil(() => behaviour.Current is ChargePattern c && !c.InWarning, 3f);
            var charge = (ChargePattern)behaviour.Current;
            for (int i = 0; i < 6; i++)
                yield return new WaitForFixedUpdate();
            var trail = FindEffect("ChargeTrail_Boss");
            Assert.IsNotNull(trail, "돌진 불꽃 꼬리");
            Vector2 expected = boss.GetComponent<Rigidbody2D>().position + Vector2.up * ((ChargePatternData)charge.Data).effectHeight;
            Assert.Less(Vector2.Distance(trail.transform.position, expected), 0.5f, "보스를 따라간다");
            Assert.Greater(Vector2.Dot(trail.transform.right, charge.LockedDirection), 0.99f, "돌진 방향을 향함");
        }

        static BurstEffect FindEffect(string prefabName)
        {
            foreach (var e in Object.FindObjectsByType<BurstEffect>(FindObjectsSortMode.None))
                if (e.name.StartsWith(prefabName))
                    return e;
            return null;
        }

        [UnityTest]
        public IEnumerator B9_BloodOrb_FiveSlowOrbs()
        {
            health.Invincible = true;
            yield return SpawnAt(new Vector2(6f, 0f), roll: 0.99f); // 후보 [돌진, 혈기탄] 중 두 번째
            yield return WaitUntil(() => behaviour.Current is BloodOrbPattern, 3f);
            var orb = (BloodOrbPattern)behaviour.Current;
            yield return WaitUntil(() => behaviour.Current != orb || !orb.InWarning, 2f);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(5, orb.LastFired);
            var arrows = Object.FindObjectsByType<Arrow>(FindObjectsSortMode.None);
            int live = 0;
            foreach (var a in arrows)
                if (a.IsLive)
                {
                    live++;
                    Assert.AreEqual(4.5f, a.Velocity.magnitude, 0.01f, "플레이어 기본 속도(5)보다 느림");
                }
            Assert.AreEqual(5, live);
        }

        [UnityTest]
        public IEnumerator B10_Swing_ShieldReduces_KnockbackImmune()
        {
            yield return SpawnAt(new Vector2(2f, 0f));
            Assert.IsFalse(boss.TryKnockback(Vector2.zero, 10f), "넉백 면역");
            boss.GetComponent<Collider2D>().enabled = false; // 접촉 데미지를 빼고 휘두르기만 잰다
            health.IncomingDamageMultiplier = 0.8f;
            float hp = health.CurrentHp;
            yield return WaitUntil(() => behaviour.Current is SwingPattern, 2f);
            var swing = (SwingPattern)behaviour.Current;
            yield return WaitUntil(() => behaviour.Current != swing, 2f);
            health.IncomingDamageMultiplier = 1f;
            Assert.IsTrue(swing.LastHit);
            Assert.AreEqual(12f, hp - health.CurrentHp, 0.01f, "15 × 0.8 (자하호신강)");
        }

        [UnityTest]
        public IEnumerator B11_Whirlwind_DamageWhileSpinning_ThenDizzy()
        {
            yield return SpawnAt(new Vector2(1f, 0f));
            boss.GetComponent<Collider2D>().enabled = false; // 접촉 데미지 제외
            boss.TakeDamage(boss.Data.maxHp * 0.6f);
            Time.timeScale = 2f;
            // 2페이즈 첫 패턴(지면 강타)이 끝난 뒤 혈풍선이 나오도록 다른 패턴을 쿨타임에 둔다
            yield return WaitUntil(() => behaviour.Last is GroundSlamPattern && behaviour.Current == null, 8f);
            Assert.IsTrue(behaviour.Last is GroundSlamPattern, "2페이즈 첫 패턴(지면 강타)이 끝나야 한다");
            foreach (var p in behaviour.Patterns)
                if (!(p is WhirlwindPattern))
                    p.StartCooldown(2);
            yield return WaitUntil(() => behaviour.Current is WhirlwindPattern, 4f);
            var whirl = (WhirlwindPattern)behaviour.Current;
            yield return WaitUntil(() => whirl.IsSpinning, 3f);
            health.Invincible = false;
            float hp = health.CurrentHp;
            yield return WaitUntil(() => whirl.IsDizzy, 4f);
            float dealt = hp - health.CurrentHp;
            Assert.AreEqual(40f, dealt, 6f, "2초 × 초당 20 (계속 붙어 있음)");
            yield return new WaitForFixedUpdate();
            Assert.Less(boss.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 0.01f, "어지러움 동안 멈춤");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator B12_Defeat_ClearsField_GameClear()
        {
            health.Invincible = true;
            yield return SpawnAt(new Vector2(6f, 0f));
            scene.SpawnAt(new Vector2(3f, 3f), chase: false);
            scene.XpDropper.SpawnOrb(scene.Player.transform.position + new Vector3(10f, 0f), 1, Color.white);
            boss.TakeDamage(boss.Data.maxHp * 2f);
            Assert.IsTrue(bossSpawner.IsDefeated);
            Assert.IsTrue(health.DamageLocked, "처치 순간부터 플레이어는 맞지 않음");
            Assert.Less(Time.timeScale, 1f, "슬로 모션");
            yield return WaitUntil(() => GameManager.IsIn(GameState.GameClear), 2f);
            Assert.IsTrue(GameManager.IsIn(GameState.GameClear));
            Assert.AreEqual(0, scene.Spawner.CountActive, "남은 적 정리");
            Assert.AreEqual(0, scene.XpDropper.Pool.CountActive, "남은 경험치 구슬 정리");

            var result = Object.FindFirstObjectByType<ResultScreen>();
            Assert.IsTrue(result.Panel.activeSelf);
            Assert.AreEqual("게임 클리어", result.HeaderText.text);
            StringAssert.StartsWith("클리어 시간", result.TimeText.text);
        }

        [UnityTest]
        public IEnumerator B13_EarlySummonClear_ShowsEarlyTitleAndTime()
        {
            health.Invincible = true;
            var waves = scene.Spawner.Waves;
            waves.Advance(361f);
            Assert.IsTrue(waves.SummonBossEarly());
            boss = bossSpawner.Boss;
            boss.TakeDamage(boss.Data.maxHp * 2f);
            yield return WaitUntil(() => GameManager.IsIn(GameState.GameClear), 2f);
            var result = Object.FindFirstObjectByType<ResultScreen>();
            Assert.AreEqual("조기 소환 클리어", result.HeaderText.text);
            StringAssert.Contains("보스 소환 06:01", result.TimeText.text);
        }

        [UnityTest]
        public IEnumerator B14_SameFrameDeath_ClearWins()
        {
            yield return SpawnAt(new Vector2(6f, 0f));
            health.TakeDamage(100000f);
            Assert.IsTrue(GameManager.IsIn(GameState.GameOver));
            boss.TakeDamage(boss.Data.maxHp * 2f); // 같은 프레임
            Assert.IsTrue(GameManager.IsIn(GameState.GameClear), "같은 프레임이면 클리어 우선");
            yield return null;
            Assert.AreEqual("게임 클리어", Object.FindFirstObjectByType<ResultScreen>().HeaderText.text);
        }

        [UnityTest]
        public IEnumerator B15_Camera_ShakesOnAppear_PullsOnPhase_ZoomsTowardBossOnDefeat()
        {
            health.Invincible = true;
            var fx = Object.FindFirstObjectByType<CameraFx>();
            Assert.IsNotNull(fx, "카메라에 CameraFx");
            var cam = fx.GetComponent<Camera>();
            yield return SpawnAt(new Vector2(6f, 0f));
            Assert.IsTrue(fx.IsShaking, "등장 때 화면 흔들림");
            yield return WaitGameSeconds(1f);
            Assert.IsFalse(fx.IsShaking);
            Assert.Less(fx.Offset.magnitude, 0.001f, "흔들림이 끝나면 다시 플레이어 중앙");

            behaviour.FixedRoll = -1f;
            boss.TakeDamage(boss.Data.maxHp * 0.55f);
            yield return WaitUntil(() => behaviour.IsTransitioning, 6f);
            Assert.IsTrue(fx.IsShaking, "페이즈 전환 흔들림");
            yield return WaitGameSeconds(0.25f);
            Assert.Less(cam.orthographicSize, fx.BaseSize, "잠깐 당겨짐");
            yield return WaitGameSeconds(1f);
            Assert.AreEqual(fx.BaseSize, cam.orthographicSize, 0.001f, "원래 크기로");

            Vector2 toBoss = boss.transform.position - scene.Player.transform.position;
            boss.TakeDamage(boss.Data.maxHp * 2f);
            float end = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < end)
                yield return null;
            Assert.Less(cam.orthographicSize, fx.BaseSize * 0.9f, "처치 순간 확대");
            Assert.Greater(Vector2.Dot(fx.Offset, toBoss.normalized), toBoss.magnitude * 0.3f, "보스 쪽으로 초점 이동");
        }
    }
}
