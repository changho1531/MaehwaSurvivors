using System.Collections.Generic;
using Game.Progression;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>[레벨업 카드] 후보에서 2개 무작위 추출, 중복 없음, 부족하면 있는 만큼 (설계서 6절).</summary>
    public class CardPickerTests
    {
        readonly List<string> result = new();

        [Test]
        public void PicksRequestedCount_WithoutDuplicates()
        {
            var candidates = new[] { "a", "b", "c", "d" };
            var random = new System.Random(7);
            for (int trial = 0; trial < 50; trial++)
            {
                CardPicker.Pick(candidates, 2, random, result);
                Assert.AreEqual(2, result.Count);
                Assert.AreNotEqual(result[0], result[1], "같은 항목 중복 금지");
                CollectionAssert.IsSubsetOf(result, candidates);
            }
        }

        [Test]
        public void SingleCandidate_ReturnsOneCard()
        {
            CardPicker.Pick(new[] { "only" }, 2, new System.Random(1), result);
            CollectionAssert.AreEqual(new[] { "only" }, result, "후보가 1개면 카드 1장");
        }

        [Test]
        public void NoCandidates_ReturnsEmpty()
        {
            CardPicker.Pick(new string[0], 2, new System.Random(1), result);
            Assert.IsEmpty(result);
        }

        [Test]
        public void EveryCandidate_CanAppear()
        {
            var candidates = new[] { "a", "b", "c" };
            var seen = new HashSet<string>();
            var random = new System.Random(3);
            for (int trial = 0; trial < 100; trial++)
            {
                CardPicker.Pick(candidates, 2, random, result);
                seen.UnionWith(result);
            }
            Assert.AreEqual(3, seen.Count, "무작위 추출이라 모든 후보가 나올 수 있어야 한다");
        }
    }
}
