using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>[기능 4] 게임 시작 = Title → Play 전이. 설계서 4절 전이 규칙 검증.</summary>
    public class GameStateMachineTests
    {
        [Test]
        public void StartsInTitle()
        {
            Assert.AreEqual(GameState.Title, new GameStateMachine().Current);
        }

        [Test]
        public void TitleToPlay_IsAllowed_AndRaisesEvent()
        {
            var fsm = new GameStateMachine();
            GameState? from = null, to = null;
            fsm.StateChanged += (a, b) => { from = a; to = b; };

            Assert.IsTrue(fsm.TryChangeState(GameState.Play));
            Assert.AreEqual(GameState.Play, fsm.Current);
            Assert.AreEqual(GameState.Title, from);
            Assert.AreEqual(GameState.Play, to);
        }

        [TestCase(GameState.Pause)]
        [TestCase(GameState.LevelUp)]
        [TestCase(GameState.GameOver)]
        [TestCase(GameState.GameClear)]
        public void TitleToOtherThanPlay_IsRejected(GameState target)
        {
            var fsm = new GameStateMachine();
            bool raised = false;
            fsm.StateChanged += (_, _) => raised = true;

            Assert.IsFalse(fsm.TryChangeState(target));
            Assert.AreEqual(GameState.Title, fsm.Current);
            Assert.IsFalse(raised);
        }

        [Test]
        public void PlayingTwice_IsRejected()
        {
            // 게임 시작 버튼 연타로 씬이 두 번 로드되지 않아야 한다.
            var fsm = new GameStateMachine();
            Assert.IsTrue(fsm.TryChangeState(GameState.Play));
            Assert.IsFalse(fsm.TryChangeState(GameState.Play));
        }

        [Test]
        public void DesignTransitions_AreAllowed()
        {
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Play, GameState.Pause));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Pause, GameState.Play));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Pause, GameState.Title));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Play, GameState.LevelUp));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.LevelUp, GameState.Play));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.Play, GameState.GameOver));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.GameOver, GameState.Play));
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.GameClear, GameState.Title));
            Assert.IsFalse(GameStateMachine.CanTransition(GameState.LevelUp, GameState.Title));
        }
    }
}
