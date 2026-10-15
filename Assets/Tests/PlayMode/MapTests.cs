using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Map;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class MapTests
    {
        InGameScene scene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            scene = new InGameScene();
            yield return scene.Load();
        }

        [UnityTest]
        public IEnumerator M1_Floor_CoversScreen_UnderEverything()
        {
            var floor = Object.FindFirstObjectByType<InfiniteFloor>();
            Assert.IsNotNull(floor, "InGame 씬에 바닥이 없다");
            var sr = floor.GetComponent<SpriteRenderer>();
            Assert.AreEqual(SpriteDrawMode.Tiled, sr.drawMode);
            Assert.Less(sr.sortingOrder, -50, "바닥 < 장식");

            // 멀리 이동해도 화면이 바닥으로 덮여 있어야 한다 (바닥이 카메라를 따라옴)
            scene.StopSpawningAndClear();
            scene.Player.GetComponent<Rigidbody2D>().position = new Vector2(503f, -211f);
            scene.Player.transform.position = new Vector3(503f, -211f, 0f);
            yield return null;
            yield return null;

            var cam = scene.Camera;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            var b = sr.bounds;
            var c = cam.transform.position;
            Assert.LessOrEqual(b.min.x, c.x - halfW);
            Assert.GreaterOrEqual(b.max.x, c.x + halfW);
            Assert.LessOrEqual(b.min.y, c.y - halfH);
            Assert.GreaterOrEqual(b.max.y, c.y + halfH);
        }

        [UnityTest]
        public IEnumerator M2_Decorations_ReturnSame_AndArePooled()
        {
            var scatter = Object.FindFirstObjectByType<DecorationScatter>();
            Assert.IsNotNull(scatter, "InGame 씬에 장식이 없다");
            scatter.enabled = false; // 카메라 대신 테스트가 위치를 정한다
            yield return null;

            scatter.Refresh(new Vector2(0.5f, 0.5f));
            var before = Snapshot(scatter);
            int created = scatter.TotalCreated;
            Assert.Greater(before.Count, 0);
            Assert.IsTrue(scatter.GetComponentsInChildren<Collider2D>().Length == 0, "장식은 충돌 없음 (9-A 규칙 4)");

            for (int i = 1; i <= 20; i++)
                scatter.Refresh(new Vector2(i * 40f, i * -25f)); // 멀리 돌아다님
            scatter.Refresh(new Vector2(0.5f, 0.5f));

            CollectionAssert.AreEquivalent(before, Snapshot(scatter), "되돌아오면 같은 장식이 같은 자리에");
            Assert.AreEqual(created, scatter.TotalCreated, "멀리 다녀도 새로 만들지 않고 풀에서 재사용");
        }

        static List<string> Snapshot(DecorationScatter scatter) =>
            scatter.GetComponentsInChildren<SpriteRenderer>(false)
                .Select(s => $"{s.transform.position.x:F3},{s.transform.position.y:F3},{s.sprite.name},{s.flipX}")
                .ToList();
    }
}
