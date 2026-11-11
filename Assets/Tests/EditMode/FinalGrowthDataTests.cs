using Game.Progression;
using Game.Weapons;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode
{
    /// <summary>[최종 성장 구조 7-I절] 실제 에셋 값: 카탈로그 6종 · 모두 최대 Lv3, 최대 레벨 효과가 Lv3에서 켜짐, 필요 XP 17칸.</summary>
    public class FinalGrowthDataTests
    {
        const string CatalogPath = "Assets/Data/Weapons/WeaponCatalog_Final.asset";
        const string CurvePath = "Assets/Data/Progression/LevelCurve.asset";

        static WeaponCatalog Catalog => AssetDatabase.LoadAssetAtPath<WeaponCatalog>(CatalogPath);

        [Test]
        public void Catalog_HasSixSkills_AllMaxLevel3()
        {
            var catalog = Catalog;
            Assert.IsNotNull(catalog, CatalogPath);
            Assert.AreEqual(6, catalog.weapons.Count);
            foreach (var weapon in catalog.weapons)
            {
                Assert.IsNotNull(weapon);
                Assert.AreEqual(3, weapon.maxLevel, weapon.displayName);
            }
            CollectionAssert.AllItemsAreUnique(catalog.weapons);
        }

        [Test]
        public void MaxLevelEffects_TurnOnAtLv3()
        {
            ProjectileWeaponData sword = null;
            SweepWeaponData sweep = null;
            foreach (var weapon in Catalog.weapons)
            {
                if (weapon is ProjectileWeaponData p) sword = p;
                if (weapon is SweepWeaponData s) sweep = s;
            }
            Assert.IsNotNull(sword);
            Assert.IsNotNull(sweep);

            Assert.AreEqual(0, sword.GetPierce(2), "매화검기 Lv2 관통 없음");
            Assert.AreEqual(-1, sword.GetPierce(3), "매화검기 Lv3 관통 무제한");
            Assert.AreEqual(16f, sword.GetDamage(3));

            Assert.Greater(sweep.GetCooldown(2), 0f);
            Assert.AreEqual(0f, sweep.GetCooldown(3), "화산십이검 Lv3 연속 발동");
            Assert.AreEqual(3f, sweep.GetDamage(3), "Lv3 타격당 데미지 3");
        }

        [Test]
        public void LevelCurve_Has17Steps_Increasing()
        {
            var curve = AssetDatabase.LoadAssetAtPath<LevelCurve>(CurvePath);
            Assert.IsNotNull(curve, CurvePath);
            Assert.AreEqual(17, curve.xpToNextLevel.Length, "Lv1→2 … Lv17→18");
            for (int i = 1; i < curve.xpToNextLevel.Length; i++)
                Assert.Greater(curve.xpToNextLevel[i], curve.xpToNextLevel[i - 1], $"칸 {i}");
        }

        [Test]
        public void CardCount_ToMaxEverything_Is17()
        {
            // 매화검기 Lv1로 시작: 매화검기 강화 2 + 나머지 5종 × (획득 1 + 강화 2)
            int cards = 0;
            foreach (var weapon in Catalog.weapons)
                cards += weapon.maxLevel;
            cards -= 1; // 시작 무기 Lv1은 카드 없이 지급
            Assert.AreEqual(17, cards);
        }
    }
}
