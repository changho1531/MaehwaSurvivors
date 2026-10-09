using System.Collections;
using Game.Core;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [7-B절] 접촉 중 초당 데미지(무적 없음, 마리 수 합산), HP 0 → GameOver·시간 정지·Died 1회,
    /// 레벨업과 겹치면 GameOver 우선, 일시정지 중 무피해, 피격 연출(틴트·복귀).
    /// </summary>
    public class PlayerHealthTests
    {
        readonly InGameScene scene = new();
        PlayerHealth health;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
            health = scene.Player.GetComponent<PlayerHealth>();
            Assert.IsNotNull(health, "플레이어에 PlayerHealth가 없다");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
        }

        static IEnumerator WaitFixed(float seconds)
        {
            int steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (int i = 0; i < steps; i++)
                yield return new WaitForFixedUpdate();
        }

        // 플레이어 콜라이더(발 위치) 옆에 두고 추적시켜 계속 밀고 들어오게 한다 → 접촉이 유지된다
        Game.Enemies.Enemy SpawnTouching(Vector2 side) => scene.SpawnAt(side * 0.9f, chase: true);

        [UnityTest]
        public IEnumerator H1_Contact_DrainsHpPerSecond_NoInvulnerability()
        {
            Assert.AreEqual(health.Data.maxHp, health.CurrentHp, "시작 HP = 최대");
            var enemy = SpawnTouching(Vector2.right);
            float dps = enemy.Data.contactDps;
            yield return WaitUntil(() => health.CurrentHp < health.MaxHp, 2f);

            float start = health.CurrentHp;
            yield return WaitFixed(1f);
            float lost = start - health.CurrentHp;
            Assert.That(lost, Is.EqualTo(dps).Within(dps * 0.2f), "1초 접촉 ≈ contactDps (무적 시간 없이 계속 깎임)");
        }

        [UnityTest]
        public IEnumerator H2_MultipleEnemies_DamageAddsUp()
        {
            var a = SpawnTouching(Vector2.right);
            SpawnTouching(Vector2.left); // 적 콜라이더가 1칸 상자라 플레이어 둘레에 동시에 붙을 수 있는 수가 제한된다 → 맞은편 2마리로 검증
            float dps = a.Data.contactDps;
            yield return WaitUntil(() => health.CurrentHp < health.MaxHp, 2f);
            yield return WaitFixed(0.2f); // 두 마리 모두 접촉이 잡히도록

            float start = health.CurrentHp;
            yield return WaitFixed(1f);
            float lost = start - health.CurrentHp;
            Assert.That(lost, Is.EqualTo(dps * 2f).Within(dps * 2f * 0.2f), "마리 수만큼 합산");
        }

        [UnityTest]
        public IEnumerator H3_HpZero_GameOver_StopsTime_DiedOnce()
        {
            int died = 0;
            health.Died += () => died++;

            health.TakeDamage(health.MaxHp - 1f);
            Assert.IsFalse(health.IsDead);
            // 같은 프레임에 여러 적이 마지막 데미지를 주는 상황 (Pause 전이 전에 연속 호출)
            health.TakeDamage(5f);
            health.TakeDamage(5f);
            yield return null;

            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, died, "Died 1회만");
            Assert.AreEqual(GameState.GameOver, GameManager.Instance.CurrentState, "HP 0 → GameOver");
            Assert.AreEqual(0f, Time.timeScale, "GameOver 중 게임 시간 정지");
        }

        [UnityTest]
        public IEnumerator H4_DeathAndLevelUpSameFrame_GameOverWins_NoCards()
        {
            health.TakeDamage(health.MaxHp - 1f);
            // 같은 프레임: 접촉 사망 → 레벨업 XP 습득 (물리가 Update보다 먼저 도는 실제 순서)
            health.TakeDamage(10f);
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;

            Assert.AreEqual(GameState.GameOver, GameManager.Instance.CurrentState, "GameOver 우선");
            Assert.IsFalse(scene.LevelUpFlow.IsAwaitingChoice, "카드 표시 안 함");
        }

        [UnityTest]
        public IEnumerator H5_DuringLevelUpPause_NoDamage()
        {
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;
            Assert.AreEqual(GameState.LevelUp, GameManager.Instance.CurrentState);

            Assert.IsFalse(health.TakeDamage(10f), "일시정지 중 피해 없음");
            Assert.AreEqual(health.MaxHp, health.CurrentHp);
        }

        [UnityTest]
        public IEnumerator H6_Feedback_RedTintWhileHit_RecoversAfter()
        {
            var feedback = scene.Player.GetComponent<DamageFeedback>();
            Assert.IsNotNull(feedback, "DamageFeedback 없음");
            yield return null;
            Assert.AreEqual(Color.white, feedback.CurrentTint, "평소에는 원래 색");

            var enemy = SpawnTouching(Vector2.right);
            yield return WaitUntil(() => feedback.IsBeingHit, 2f);
            yield return null;
            Assert.IsTrue(feedback.IsBeingHit);
            Assert.Less(feedback.CurrentTint.g, 0.6f, "맞는 동안 빨간 틴트");

            scene.Spawner.Pool.Release(enemy); // 데미지 중단
            float end = Time.time + 0.6f;
            while (Time.time < end)
                yield return null;
            Assert.IsFalse(feedback.IsBeingHit);
            Assert.AreEqual(1f, feedback.CurrentTint.g, 0.01f, "데미지가 멈추면 원래 색으로 복귀");
        }
    }
}
