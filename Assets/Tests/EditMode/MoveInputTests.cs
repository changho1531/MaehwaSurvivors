using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Tests.EditMode
{
    /// <summary>[기능 6] WASD와 방향키 동시 지원.</summary>
    public class MoveInputTests : InputTestFixture
    {
        Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.That(Vector2.Distance(expected, actual), Is.LessThan(0.001f), $"expected {expected}, actual {actual}");
        }

        [Test]
        public void NoKeys_IsZero()
        {
            AssertVector(Vector2.zero, MoveInput.Read(keyboard));
        }

        [Test]
        public void NullKeyboard_IsZero()
        {
            AssertVector(Vector2.zero, MoveInput.Read(null));
        }

        [Test]
        public void Wasd_MapsToFourDirections()
        {
            Press(keyboard.wKey); AssertVector(Vector2.up, MoveInput.Read(keyboard)); Release(keyboard.wKey);
            Press(keyboard.sKey); AssertVector(Vector2.down, MoveInput.Read(keyboard)); Release(keyboard.sKey);
            Press(keyboard.aKey); AssertVector(Vector2.left, MoveInput.Read(keyboard)); Release(keyboard.aKey);
            Press(keyboard.dKey); AssertVector(Vector2.right, MoveInput.Read(keyboard)); Release(keyboard.dKey);
        }

        [Test]
        public void Arrows_MapToFourDirections()
        {
            Press(keyboard.upArrowKey); AssertVector(Vector2.up, MoveInput.Read(keyboard)); Release(keyboard.upArrowKey);
            Press(keyboard.downArrowKey); AssertVector(Vector2.down, MoveInput.Read(keyboard)); Release(keyboard.downArrowKey);
            Press(keyboard.leftArrowKey); AssertVector(Vector2.left, MoveInput.Read(keyboard)); Release(keyboard.leftArrowKey);
            Press(keyboard.rightArrowKey); AssertVector(Vector2.right, MoveInput.Read(keyboard)); Release(keyboard.rightArrowKey);
        }

        [Test]
        public void Diagonal_IsNormalized()
        {
            Press(keyboard.dKey);
            Press(keyboard.sKey);
            var move = MoveInput.Read(keyboard);

            AssertVector(new Vector2(1f, -1f).normalized, move);
            Assert.That(move.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void WasdAndArrowSameDirection_DoesNotDoubleSpeed()
        {
            Press(keyboard.wKey);
            Press(keyboard.upArrowKey);
            AssertVector(Vector2.up, MoveInput.Read(keyboard));
        }

        [Test]
        public void OppositeKeys_CancelOut()
        {
            Press(keyboard.aKey);
            Press(keyboard.rightArrowKey);
            AssertVector(Vector2.zero, MoveInput.Read(keyboard));
        }
    }
}
