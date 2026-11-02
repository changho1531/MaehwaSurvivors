using System.Collections.Generic;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[만천화우 7-F절] 카드 문구, 화면 사각형.</summary>
    public class RainWeaponDataTests
    {
        [Test]
        public void CardText_ComesFromTable_DamageLineOmitted()
        {
            var data = ScriptableObject.CreateInstance<RainWeaponData>();
            var lines = new List<string>();
            try
            {
                data.maxLevel = 3;
                data.radiusPerLevel = new[] { 2f, 2.5f, 3f };
                data.cooldownPerLevel = new[] { 8f, 7f, 6f };
                data.damagePerLevel = new[] { 6f, 6f, 6f };
                data.maxLevelEffect = "최대 레벨: 쏟아짐이 끝날 때 범위 전체 폭발";

                data.DescribeUpgrade(1, lines);
                CollectionAssert.AreEqual(new[] { "범위 +0.5칸", "쿨타임 -1초" }, lines);
                data.DescribeUpgrade(2, lines);
                CollectionAssert.AreEqual(new[] { "범위 +0.5칸", "쿨타임 -1초", "최대 레벨: 쏟아짐이 끝날 때 범위 전체 폭발" }, lines);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ViewRect_MatchesOrthographicCamera()
        {
            var go = new GameObject("cam");
            try
            {
                var camera = go.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.aspect = 2f;
                go.transform.position = new Vector3(1f, 2f, -10f);

                var rect = RainWeapon.ViewRect(camera, Vector2.zero);
                Assert.AreEqual(new Rect(-9f, -3f, 20f, 10f), rect);
                Assert.AreEqual(new Rect(3f, 4f, 0f, 0f), RainWeapon.ViewRect(null, new Vector2(3f, 4f)), "카메라가 없으면 한 점");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
