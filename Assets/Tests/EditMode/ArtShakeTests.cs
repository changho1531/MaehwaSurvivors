using Game.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>적 그림 떨림 공용 계산: 가로만, 시작은 제자리, 멈추면 제자리 (궁수 · 저주술사 · 보스 공용).</summary>
    public class ArtShakeTests
    {
        [Test]
        public void Offset_StartsAtZero_PeaksAtQuarterPeriod()
        {
            Assert.AreEqual(0f, ArtShake.Offset(0f, 0.06f, 30f), 1e-6f);
            Assert.AreEqual(0.06f, ArtShake.Offset(1f / 120f, 0.06f, 30f), 1e-5f, "주기(1/30초)의 1/4에서 최대");
        }

        [Test]
        public void Tick_MovesOnlyX_StopReturns()
        {
            var go = new GameObject("art");
            try
            {
                go.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                var shake = new ArtShake(go.transform);
                shake.Tick(0.06f, 30f, 1f / 120f);
                Assert.AreEqual(0.06f, go.transform.localPosition.x, 1e-5f);
                Assert.AreEqual(0.2f, go.transform.localPosition.y, 1e-6f, "세로(걷기 통통)는 건드리지 않음");
                shake.Stop();
                Assert.AreEqual(0f, go.transform.localPosition.x, 1e-6f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
