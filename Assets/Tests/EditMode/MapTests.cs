using System.Collections.Generic;
using Game.Map;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class MapTests
    {
        [Test]
        public void FloorSnap_MovesOnlyInWholeTiles()
        {
            var tile = new Vector2(6f, 6f);
            Assert.AreEqual(Vector2.zero, InfiniteFloor.Snap(new Vector2(2.9f, -2.9f), tile));
            Assert.AreEqual(new Vector2(6f, -6f), InfiniteFloor.Snap(new Vector2(3.1f, -3.1f), tile));
            Assert.AreEqual(new Vector2(600f, 0f), InfiniteFloor.Snap(new Vector2(601f, 0f), tile), "멀리 가도 격자 위");
        }

        [Test]
        public void ChunkOf_FloorsNegativeCoordinates()
        {
            Assert.AreEqual(new Vector2Int(0, 0), DecorationScatter.ChunkOf(new Vector2(0.1f, 5.9f), 6f));
            Assert.AreEqual(new Vector2Int(-1, -1), DecorationScatter.ChunkOf(new Vector2(-0.1f, -0.1f), 6f), "0 근처 음수가 0 구역으로 겹치지 않게");
        }

        [Test]
        public void Layout_SameChunk_SameDecorations_InsideChunk()
        {
            var a = new List<DecorationScatter.Placement>();
            var b = new List<DecorationScatter.Placement>();
            var perChunk = new Vector2Int(1, 3);
            for (int x = -3; x <= 3; x++)
                for (int y = -3; y <= 3; y++)
                {
                    var chunk = new Vector2Int(x, y);
                    DecorationScatter.Layout(chunk, 1, 6f, perChunk, 5, a);
                    DecorationScatter.Layout(chunk, 1, 6f, perChunk, 5, b);
                    Assert.That(a.Count, Is.InRange(1, 3));
                    Assert.AreEqual(a.Count, b.Count, "같은 구역은 같은 개수");
                    for (int i = 0; i < a.Count; i++)
                    {
                        Assert.AreEqual(a[i].Position, b[i].Position, "되돌아와도 같은 자리");
                        Assert.AreEqual(a[i].SpriteIndex, b[i].SpriteIndex);
                        Assert.That(a[i].SpriteIndex, Is.InRange(0, 4));
                        Assert.AreEqual(chunk, DecorationScatter.ChunkOf(a[i].Position, 6f), "자기 구역 안에 놓임");
                    }
                }
        }

        [Test]
        public void Layout_DifferentChunks_Differ()
        {
            var a = new List<DecorationScatter.Placement>();
            var b = new List<DecorationScatter.Placement>();
            DecorationScatter.Layout(new Vector2Int(0, 0), 1, 6f, new Vector2Int(1, 3), 5, a);
            DecorationScatter.Layout(new Vector2Int(1, 0), 1, 6f, new Vector2Int(1, 3), 5, b);
            Assert.AreNotEqual(a[0].Position - Vector2.zero, b[0].Position - new Vector2(6f, 0f), "구역마다 배치가 달라 반복 무늬가 안 보임");
        }
    }
}
