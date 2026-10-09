using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[기능 5~6] 임시 플레이어 오브젝트와 WASD/방향키 이동, 카메라 추적.</summary>
    public class PlayerTests : InputTestFixture
    {
        readonly InGameScene scene = new();
        Keyboard keyboard;

        IEnumerator Prepare()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            if (scene.Weapon != null)
                scene.Weapon.enabled = false;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
        }

        static IEnumerator Hold(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator F5_PlayerObject_IsSetUp()
        {
            yield return Prepare();
            var player = scene.Player;

            Assert.IsNotNull(player.GetComponent<SpriteRenderer>()?.sprite, "임시 스프라이트가 있어야 화면에 보인다");
            var body = player.GetComponent<Rigidbody2D>();
            Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType, "적에게 밀리지 않도록 Kinematic");
            Assert.IsNotNull(player.GetComponent<Collider2D>(), "적이 플레이어를 통과하지 않도록 콜라이더 필요");
            Assert.AreEqual(Vector2.zero, (Vector2)player.transform.position, "시작 위치는 원점");
        }

        [UnityTest]
        public IEnumerator F6_DKey_MovesRightAtMoveSpeed()
        {
            yield return Prepare();
            var player = scene.Player.transform;
            var start = player.position;

            Press(keyboard.dKey);
            yield return Hold(0.5f);
            Release(keyboard.dKey);
            yield return new WaitForFixedUpdate();

            var moved = player.position - start;
            float expected = scene.Player.MoveSpeed * 0.5f;
            Assert.That(moved.x, Is.EqualTo(expected).Within(expected * 0.3f), $"이동량 {moved.x}, 기대 {expected}");
            Assert.That(Mathf.Abs(moved.y), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator F6_ArrowKeys_MoveInEachDirection()
        {
            yield return Prepare();
            var player = scene.Player.transform;

            var keys = new[] { keyboard.upArrowKey, keyboard.downArrowKey, keyboard.leftArrowKey, keyboard.rightArrowKey, keyboard.wKey, keyboard.sKey, keyboard.aKey };
            var dirs = new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right, Vector2.up, Vector2.down, Vector2.left };

            for (int i = 0; i < keys.Length; i++)
            {
                var start = (Vector2)player.position;
                Press(keys[i]);
                yield return Hold(0.2f);
                Release(keys[i]);
                yield return new WaitForFixedUpdate();

                var moved = (Vector2)player.position - start;
                Assert.Greater(Vector2.Dot(moved, dirs[i]), 0.3f, $"{keys[i].name} 키로 {dirs[i]} 방향 이동이 안 됨 (moved {moved})");
            }
        }

        [UnityTest]
        public IEnumerator F6_ReleasingKeys_StopsPlayer()
        {
            yield return Prepare();
            Press(keyboard.wKey);
            yield return Hold(0.2f);
            Release(keyboard.wKey);
            yield return new WaitForFixedUpdate();
            yield return null;

            var stopped = scene.Player.transform.position;
            yield return Hold(0.3f);
            Assert.That(Vector3.Distance(stopped, scene.Player.transform.position), Is.LessThan(0.01f), "키를 떼면 멈춰야 한다");
        }

        [UnityTest]
        public IEnumerator F6_CameraFollowsPlayer()
        {
            yield return Prepare();
            Press(keyboard.dKey);
            Press(keyboard.wKey);
            yield return Hold(0.5f);
            Release(keyboard.dKey);
            Release(keyboard.wKey);
            yield return null;

            var cam = scene.Camera.transform.position;
            var player = scene.Player.transform.position;
            Assert.That(Vector2.Distance(cam, player), Is.LessThan(0.05f), "카메라가 플레이어를 중앙에 두어야 한다");
        }
    }
}
