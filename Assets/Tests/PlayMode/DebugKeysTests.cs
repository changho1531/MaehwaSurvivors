using System.Collections;
using Game.Core;
using Game.Player;
using Game.UI;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[시연용 디버그 키 7-D절 5] F1 레벨업, F2 무적, F3 화산십이검 획득, F4 보유 무기 최대 레벨, F5 스폰 최대, DEBUG 표시.</summary>
    public class DebugKeysTests : InGameInputFixture
    {
        readonly InGameScene scene = new();
        Keyboard keyboard;
        DebugKeys keys;
        PlayerHealth health;
        UnityEngine.UI.Text label;

        IEnumerator Prepare()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            keys = Object.FindFirstObjectByType<DebugKeys>();
            health = Object.FindFirstObjectByType<PlayerHealth>();
            Assert.IsNotNull(keys, "InGame 씬에 DebugKeys가 없다");
            Assert.IsTrue(keys.enabled, "에디터(디버그 빌드)에서는 켜져 있다");
            label = GameObject.Find("DebugLabel").GetComponent<UnityEngine.UI.Text>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
        }

        IEnumerator Tap(KeyControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator D1_F1_LevelsUp_AndShowsCards()
        {
            yield return Prepare();
            yield return Tap(keyboard.f1Key);
            Assert.AreEqual(2, scene.Level.Level);
            Assert.AreEqual(GameState.LevelUp, GameManager.Instance.CurrentState, "평소와 같은 레벨업 흐름(카드 표시)");
        }

        [UnityTest]
        public IEnumerator D2_F2_TogglesInvincible_AndLabel()
        {
            yield return Prepare();
            StringAssert.StartsWith("DEBUG", label.text, "디버그 키가 켜져 있음을 표시");
            yield return Tap(keyboard.f2Key);
            Assert.IsTrue(health.Invincible);
            Assert.IsFalse(health.TakeDamage(health.MaxHp), "무적 중 데미지 무시");
            StringAssert.Contains("무적", label.text);

            yield return Tap(keyboard.f2Key);
            Assert.IsFalse(health.Invincible);
            Assert.IsTrue(health.TakeDamage(1f));
        }

        [UnityTest]
        public IEnumerator D3_F3_AcquiresSweep_F4_MaxesOwned()
        {
            yield return Prepare();
            yield return Tap(keyboard.f3Key);
            Assert.AreEqual(2, scene.Inventory.Owned.Count);
            Assert.IsInstanceOf<SweepWeapon>(scene.Inventory.Owned[1], "화산십이검 즉시 획득");

            yield return Tap(keyboard.f4Key);
            foreach (var weapon in scene.Inventory.Owned)
                Assert.AreEqual(weapon.Data.maxLevel, weapon.Level, "보유 무기 전부 최대 레벨");
            Assert.IsTrue(scene.Inventory.AllMaxed, "성장 완료 판정도 평소처럼");
        }

        [UnityTest]
        public IEnumerator D4_F5_TogglesFastestSpawn()
        {
            yield return Prepare();
            float normal = scene.Spawner.CurrentInterval;
            yield return Tap(keyboard.f5Key);
            Assert.IsTrue(scene.Spawner.ForceFastest);
            Assert.Less(scene.Spawner.CurrentInterval, normal, "곡선의 하한으로 스폰");
            StringAssert.Contains("스폰 최대", label.text);
            yield return Tap(keyboard.f5Key);
            Assert.IsFalse(scene.Spawner.ForceFastest);
        }

        [UnityTest]
        public IEnumerator D5_KeysIgnored_OutsidePlay()
        {
            yield return Prepare();
            GameManager.Instance.StateMachine.TryChangeState(GameState.Pause);
            yield return Tap(keyboard.f3Key);
            Assert.AreEqual(1, scene.Inventory.Owned.Count, "일시정지 중에는 디버그 키 무시");
        }
    }
}
