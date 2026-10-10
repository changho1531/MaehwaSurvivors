using Game.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[8-A절 적 조합] 플레이어 레벨 → 궁수 비율: Lv1 0%, Lv2부터 등장, 최대 레벨(10)에 30%, 사이는 선형.</summary>
    public class SpawnCompositionTests
    {
        SpawnComposition composition;
        SpawnComposition.Entry archer;
        Enemy archerPrefab;

        [SetUp]
        public void SetUp()
        {
            archerPrefab = new GameObject("Archer").AddComponent<Enemy>();
            archer = new SpawnComposition.Entry { prefab = archerPrefab, firstLevel = 2, ratioAtMaxLevel = 0.3f };
            composition = ScriptableObject.CreateInstance<SpawnComposition>();
            composition.maxLevel = 10;
            composition.entries = new[] { archer };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(composition);
            Object.DestroyImmediate(archerPrefab.gameObject);
        }

        [Test]
        public void Ratio_ZeroBeforeFirstLevel_LinearToMax_ClampedAfter()
        {
            Assert.AreEqual(0f, composition.GetRatio(archer, 1), "Lv1에는 궁수 없음");
            Assert.That(composition.GetRatio(archer, 2), Is.GreaterThan(0f), "Lv2부터 등장");
            Assert.That(composition.GetRatio(archer, 10), Is.EqualTo(0.3f).Within(1e-5f), "최대 레벨 7:3");
            Assert.That(composition.GetRatio(archer, 15), Is.EqualTo(0.3f).Within(1e-5f), "최대 레벨 이후 고정");
            for (int level = 3; level <= 10; level++)
                Assert.Greater(composition.GetRatio(archer, level), composition.GetRatio(archer, level - 1), "레벨이 오를수록 증가");
        }

        [Test]
        public void Pick_UsesRollAgainstRatio_NullMeansBaseEnemy()
        {
            Assert.IsNull(composition.Pick(1, 0f), "Lv1은 항상 기본 적");
            Assert.AreSame(archerPrefab, composition.Pick(10, 0.29f));
            Assert.IsNull(composition.Pick(10, 0.31f));
        }
    }
}
