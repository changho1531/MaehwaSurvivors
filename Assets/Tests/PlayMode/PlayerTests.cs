using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>[기능 5~6] 임시 플레이어 오브젝트와 WASD/방향키 이동, 카메라 추적.</summary>
    public class PlayerTests : InputTestFixture
    {
        readonly InGameScene scene = new();
        Keyboard keyboard;

        // InputTestFixture는 입력 시스템 상태를 통째로 바꿔 끼운다. UI 입력 모듈의 기본 액션은 static으로 공유되어
        // 교체 후에도 사라진 상태를 참조해 예외(statePtr null)를 내므로, 교체 직전(Setup)과 복원 직전(TearDown)에
        // UnassignActions()로 공유 액션을 폐기하고 모듈을 파괴한다. 테스트 씬은 매번 새로 로드하므로 영향 없음.
        public override void Setup()
        {
            DestroyUiInputModules();
            base.Setup();
        }

        public override void TearDown()
        {
            DestroyUiInputModules();
            base.TearDown();
        }

        static void DestroyUiInputModules()
        {
            foreach (var module in Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                module.UnassignActions();
                Object.DestroyImmediate(module.gameObject);
            }
        }

        IEnumerator Prepare()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            if (scene.Weapon != null)
                scene.Weapon.enabled = false;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
        }

        // 이동은 FixedUpdate에서 일어나므로 실시간이 아니라 물리 스텝 수로 기다린다.
        // Time.time 기준이면 에디터 프레임이 한 번 길게 튀었을 때 이동 스텝이 0회로 끝나 테스트가 흔들렸다.
        static IEnumerator Hold(float seconds)
        {
            yield return null; // 누른 입력을 PlayerMovement.Update가 먼저 읽게 한다
            int steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (int i = 0; i < steps; i++)
                yield return new WaitForFixedUpdate();
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
            // 카메라는 LateUpdate에서 따라가므로, 마지막 물리 이동이 끝나고 LateUpdate까지 지난 프레임 끝에서 비교한다.
            // (yield null은 LateUpdate 이전에 재개되어, 그 프레임 물리 이동만큼 간헐적으로 어긋난다)
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();

            var cam = scene.Camera.transform.position;
            var player = scene.Player.transform.position;
            Assert.That(Vector2.Distance(cam, player), Is.LessThan(0.05f), "카메라가 플레이어를 중앙에 두어야 한다");
        }
    }
}
