using System.Collections;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[7-A절] 플레이어 스프라이트·애니메이션: 키 1칸·화면 높이 약 10%, 방향별 걷기 클립, 왼쪽 반전, 정지 = 정면 첫 프레임.</summary>
    public class PlayerAnimationTests : InGameInputFixture
    {
        readonly InGameScene scene = new();
        Keyboard keyboard;
        PlayerAnimator anim;
        SpriteRenderer sprite;

        IEnumerator Prepare()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            keyboard = InputSystem.AddDevice<Keyboard>();
            anim = scene.Player.GetComponent<PlayerAnimator>();
            sprite = scene.Player.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(anim, "플레이어에 PlayerAnimator가 없다");
        }

        IEnumerator Walk(Key key, float seconds)
        {
            Press(keyboard[key]);
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator A1_Setup_HeightIsOneUnit_AboutTenPercentOfScreen()
        {
            yield return Prepare();
            var animator = scene.Player.GetComponent<Animator>();
            Assert.IsNotNull(animator.runtimeAnimatorController, "Animator Controller 연결 누락");
            Assert.AreEqual(5f, scene.Camera.orthographicSize, 0.001f, "카메라 Orthographic Size 5");

            // 캐릭터 실제 그림 높이(투명 여백 제외)는 약 1칸. 스프라이트 사각형은 캔버스 전체라 조금 더 크다.
            float rectHeight = sprite.sprite.rect.height / sprite.sprite.pixelsPerUnit;
            Assert.That(rectHeight, Is.InRange(1.0f, 1.15f), "스프라이트 높이 ≈ 키 1칸");
            Assert.AreEqual(sprite.sprite.texture.height, 256, "축소 높이 256px");
            float screenRatio = 1f / (scene.Camera.orthographicSize * 2f);
            Assert.That(screenRatio, Is.InRange(0.08f, 0.12f), "화면 높이의 약 10%");

            // 피벗은 발 위치: 그림이 transform 위쪽으로 그려진다
            Assert.Less(sprite.bounds.min.y, scene.Player.transform.position.y + 0.05f);
            Assert.Greater(sprite.bounds.center.y, scene.Player.transform.position.y + 0.3f);
        }

        [UnityTest]
        public IEnumerator A2_Stopped_ShowsFrontFirstFrame()
        {
            yield return Prepare();
            yield return null;
            yield return null;
            Assert.AreEqual(PlayerAnimator.IdleState, anim.CurrentState);
            Assert.AreEqual("Walk_Front_0", sprite.sprite.name, "정지 = 정면 첫 프레임");
            Assert.IsFalse(sprite.flipX);
        }

        [UnityTest]
        public IEnumerator A3_EachDirection_PlaysItsWalkClip()
        {
            yield return Prepare();

            yield return Walk(Key.W, 0.2f);
            Assert.AreEqual(PlayerAnimator.WalkBackState, anim.CurrentState, "위 = 뒷모습");
            StringAssert.StartsWith("Walk_Back_", sprite.sprite.name);
            Release(keyboard.wKey);
            yield return null; // 같은 프레임에 떼기·누르기가 겹치면 키보드 상태 이벤트가 합쳐진다

            yield return Walk(Key.S, 0.2f);
            Assert.AreEqual(PlayerAnimator.WalkFrontState, anim.CurrentState, "아래 = 앞모습");
            StringAssert.StartsWith("Walk_Front_", sprite.sprite.name);
            Release(keyboard.sKey);
            yield return null; // 같은 프레임에 떼기·누르기가 겹치면 키보드 상태 이벤트가 합쳐진다

            yield return Walk(Key.RightArrow, 0.2f);
            Assert.AreEqual(PlayerAnimator.WalkRightState, anim.CurrentState);
            StringAssert.StartsWith("Walk_Right_", sprite.sprite.name);
            Assert.IsFalse(sprite.flipX);
            Release(keyboard.rightArrowKey);
            yield return null; // 같은 프레임에 떼기·누르기가 겹치면 키보드 상태 이벤트가 합쳐진다

            yield return Walk(Key.A, 0.2f);
            Assert.AreEqual(PlayerAnimator.WalkRightState, anim.CurrentState, "왼쪽은 오른쪽 클립");
            Assert.IsTrue(sprite.flipX, "왼쪽은 좌우 반전");
            Release(keyboard.aKey);

            yield return null;
            yield return null;
            Assert.AreEqual(PlayerAnimator.IdleState, anim.CurrentState, "키를 떼면 정지");
            Assert.IsFalse(sprite.flipX, "정지는 정면이라 반전 해제");
        }

        [UnityTest]
        public IEnumerator A4_WalkClip_AdvancesFrames()
        {
            yield return Prepare();
            Press(keyboard.dKey);
            var seen = new System.Collections.Generic.HashSet<string>();
            float end = Time.time + 0.75f; // 8프레임 / 12fps ≈ 0.67초 한 바퀴
            while (Time.time < end)
            {
                yield return null;
                seen.Add(sprite.sprite.name);
            }
            Release(keyboard.dKey);
            Assert.GreaterOrEqual(seen.Count, 6, "걷기 중 프레임이 바뀌며 재생된다: " + string.Join(",", seen));
        }
    }
}
