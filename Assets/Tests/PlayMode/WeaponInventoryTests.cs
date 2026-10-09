using System.Collections;
using System.Collections.Generic;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[무기 구조 7절] 시작 무기 지급, 강화·최대 레벨, 카드 후보, 성장 완료 연결 (6-A절 규칙 8).</summary>
    public class WeaponInventoryTests
    {
        readonly InGameScene scene = new();

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            Assert.IsNotNull(scene.Inventory, "Player에 WeaponInventory가 없다");
            scene.StopSpawningAndClear();
        }

        [UnityTest]
        public IEnumerator I1_StartingWeapon_IsGivenAtLv1()
        {
            yield return null;
            Assert.AreEqual(1, scene.Inventory.Owned.Count);
            var weapon = scene.Inventory.Owned[0];
            Assert.IsInstanceOf<ProjectileWeapon>(weapon, "기본 무기 = 투사체형(매화검기)");
            Assert.AreEqual(1, weapon.Level);
            Assert.Contains(weapon.Data, scene.Inventory.Catalog.weapons, "카탈로그에 등록된 무기");
        }

        [UnityTest]
        public IEnumerator I2_Upgrade_StopsAtMaxLevel()
        {
            yield return null;
            var data = scene.Inventory.Owned[0].Data;
            var upgraded = new List<int>();
            scene.Inventory.WeaponUpgraded += (_, level) => upgraded.Add(level);

            for (int i = 1; i < data.maxLevel; i++)
                Assert.IsTrue(scene.Inventory.Upgrade(data));
            Assert.IsFalse(scene.Inventory.Upgrade(data), "최대 레벨에서 더 강화 불가");

            Assert.AreEqual(data.maxLevel, scene.Inventory.GetLevel(data));
            Assert.IsTrue(scene.Inventory.IsMaxed(data));
            Assert.AreEqual(data.maxLevel - 1, upgraded.Count, "강화 성공 횟수만큼 이벤트");
            Assert.AreEqual(data.maxLevel, upgraded[upgraded.Count - 1]);
        }

        [UnityTest]
        public IEnumerator I3_Candidates_ExcludeMaxedWeapons()
        {
            yield return null;
            var candidates = new List<WeaponData>();
            scene.Inventory.GetCandidates(candidates);
            Assert.AreEqual(scene.Inventory.Catalog.weapons.Count, candidates.Count, "처음엔 카탈로그 전체가 후보 (보유 중 미만렙 + 미보유)");

            var owned = scene.Inventory.Owned[0].Data;
            while (scene.Inventory.Upgrade(owned)) { }
            scene.Inventory.GetCandidates(candidates);
            CollectionAssert.DoesNotContain(candidates, owned, "최대 레벨 무기는 후보에서 제외");
        }

        [UnityTest]
        public IEnumerator I4_AllWeaponsMaxed_TriggersGrowthCompletion()
        {
            yield return null;
            scene.XpDropper.SpawnOrb(scene.Player.transform.position + new Vector3(10f, 0f), 1, Color.white);
            int maxedEvents = 0;
            scene.Inventory.AllWeaponsMaxed += () => maxedEvents++;

            foreach (var data in scene.Inventory.Catalog.weapons)
            {
                scene.Inventory.Acquire(data);
                while (scene.Inventory.Upgrade(data)) { }
            }

            Assert.IsTrue(scene.Inventory.AllMaxed);
            Assert.AreEqual(1, maxedEvents, "성장 완료 이벤트는 한 번만");
            Assert.IsTrue(scene.XpDropper.DropsStopped, "구슬 드롭 중지");
            Assert.AreEqual(0, scene.XpDropper.Pool.CountActive, "맵의 구슬 제거");
            Assert.IsTrue(scene.Level.IsMaxed, "경험치 바 MAX");
        }
    }
}
