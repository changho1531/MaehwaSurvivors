using System.Collections;
using Game.Core;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[적 접촉 데미지 → 플레이어 HP → GameOver] 접촉 시 피해, 무적 시간, HP 0 → GameOver, 레벨업과 겹치면 GameOver 우선, 일시정지 중 무피해.</summary>
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

        static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator H1_EnemyContact_DamagesPlayer_ThenInvulnerable()
        {
            Assert.AreEqual(health.Data.maxHp, health.CurrentHp, "시작 HP = 최대");
            var enemy = scene.SpawnAt(new Vector2(0.6f, 0.45f), chase: true); // 몸통 콜라이더 옆에서 붙는다
            float damage = enemy.Data.contactDamage;

            yield return WaitUntil(() => health.CurrentHp < health.Data.maxHp, 2f);
            Assert.AreEqual(health.Data.maxHp - damage, health.CurrentHp, 0.001f, "접촉 1회 데미지");
            Assert.IsTrue(health.IsInvulnerable);

            yield return Wait(health.Data.invulnerabilitySeconds * 0.6f);
            Assert.AreEqual(health.Data.maxHp - damage, health.CurrentHp, 0.001f, "무적 시간 중에는 계속 닿아 있어도 추가 피해 없음");

            yield return Wait(health.Data.invulnerabilitySeconds * 0.8f);
            Assert.AreEqual(health.Data.maxHp - damage * 2f, health.CurrentHp, 0.001f, "무적이 끝나면 다시 피해");
        }

        [UnityTest]
        public IEnumerator H2_HpZero_GoesToGameOver_AndStopsTime()
        {
            bool died = false;
            health.Died += () => died = true;

            // 무적 시간을 기다리며 최대 HP만큼 데미지를 넣는다
            // (사망하면 게임 시간이 멈추므로 사망 직후에는 게임 시간 기준으로 기다리지 않는다)
            int guard = 100;
            while (guard-- > 0)
            {
                health.TakeDamage(health.Data.maxHp * 0.5f);
                if (health.IsDead)
                    break;
                yield return Wait(health.Data.invulnerabilitySeconds + 0.05f);
            }
            yield return null;

            Assert.IsTrue(health.IsDead);
            Assert.IsTrue(died, "Died 이벤트");
            Assert.AreEqual(GameState.GameOver, GameManager.Instance.CurrentState, "HP 0 → GameOver");
            Assert.AreEqual(0f, Time.timeScale, "GameOver 중 게임 시간 정지");
            Assert.IsFalse(health.TakeDamage(10f), "사망 후 추가 피해 없음");
        }

        [UnityTest]
        public IEnumerator H3_DeathAndLevelUpSameFrame_GameOverWins_NoCards()
        {
            // 한 방에 죽을 만큼 HP를 깎아 둔다
            while (health.CurrentHp > 1f)
            {
                health.TakeDamage(health.CurrentHp - 1f);
                yield return Wait(health.Data.invulnerabilitySeconds + 0.05f);
            }

            // 같은 프레임: 접촉 사망 → 레벨업 XP 습득 (물리가 Update보다 먼저 도는 실제 순서)
            health.TakeDamage(10f);
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;

            Assert.AreEqual(GameState.GameOver, GameManager.Instance.CurrentState, "GameOver 우선");
            Assert.IsFalse(scene.LevelUpFlow.IsAwaitingChoice, "카드 표시 안 함");
        }

        [UnityTest]
        public IEnumerator H4_DuringLevelUpPause_NoDamage()
        {
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;
            Assert.AreEqual(GameState.LevelUp, GameManager.Instance.CurrentState);

            Assert.IsFalse(health.TakeDamage(10f), "일시정지 중 피해 없음");
            Assert.AreEqual(health.Data.maxHp, health.CurrentHp);
        }
    }
}
