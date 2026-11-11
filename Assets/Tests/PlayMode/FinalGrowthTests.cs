using System.Collections;
using Game.Core;
using Game.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[최종 성장 구조 7-I절] 매화검기 Lv1 시작 → 카드 17번 → 6종 모두 Lv3(성장 완료), 플레이어 Lv18, 측정 기록 17줄.</summary>
    public class FinalGrowthTests
    {
        readonly InGameScene scene = new();

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            scene.Weapon.enabled = false;
            scene.Weapon.Pool?.ReleaseAll();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator G1_SeventeenCards_MaxEverything_PlayerLv18_LogsSeventeenLevels()
        {
            var logger = Object.FindFirstObjectByType<GrowthLogger>();
            Assert.IsNotNull(logger, "InGame 씬에 GrowthLogger가 없다");
            Assert.AreEqual(1, scene.Inventory.Owned.Count, "매화검기 하나로 시작");
            Assert.AreEqual(1, scene.Inventory.Owned[0].Level);

            int cards = 0;
            float timeout = Time.realtimeSinceStartup + 60f;
            while (!scene.Inventory.AllMaxed && Time.realtimeSinceStartup < timeout)
            {
                if (GameManager.Instance.CurrentState == GameState.Play)
                    scene.Level.AddXp(scene.Level.RequiredXp);
                if (scene.LevelUpFlow.CanAcceptChoice && scene.LevelUpFlow.Choose(0))
                    cards++;
                yield return null;
            }

            Assert.IsTrue(scene.Inventory.AllMaxed, "성장 완료");
            Assert.AreEqual(17, cards, "카드 17번");
            Assert.AreEqual(18, scene.Level.Level, "플레이어 최대 Lv18");
            Assert.AreEqual(6, scene.Inventory.Owned.Count);
            foreach (var weapon in scene.Inventory.Owned)
                Assert.AreEqual(3, weapon.Level, weapon.Data.displayName);

            var levels = logger.Record.Levels;
            Assert.AreEqual(17, levels.Count, "레벨 도달 시각 17개");
            for (int i = 0; i < levels.Count; i++)
            {
                Assert.AreEqual(i + 2, levels[i].level, "Lv2부터 순서대로");
                if (i > 0)
                    Assert.GreaterOrEqual(levels[i].time, levels[i - 1].time);
            }
            yield return null;
            Assert.AreEqual(GameState.Play, GameManager.Instance.CurrentState);
        }
    }
}
