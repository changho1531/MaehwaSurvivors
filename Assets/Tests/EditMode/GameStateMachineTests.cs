using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>[기능 4, 7-C절] 게임 시작 = Title → Play 전이, 설계서 4절 전이 규칙, Pause의 직전 상태 복귀.</summary>
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
            Assert.IsTrue(GameStateMachine.CanTransition(GameState.LevelUp, GameState.Pause), "카드 선택 중 일시정지 (7-C)");
            Assert.IsFalse(GameStateMachine.CanTransition(GameState.GameOver, GameState.Pause), "결과 화면에서 ESC 무시");
        }

        [Test]
        public void PauseFromPlay_ResumesToPlay()
        {
            var fsm = new GameStateMachine(GameState.Play);
            Assert.IsTrue(fsm.TryChangeState(GameState.Pause));
            Assert.AreEqual(GameState.Play, fsm.StateBeforePause);
            Assert.IsFalse(fsm.TryChangeState(GameState.LevelUp), "Play에서 멈췄으면 LevelUp으로 나갈 수 없다");
            Assert.IsTrue(fsm.TryResume());
            Assert.AreEqual(GameState.Play, fsm.Current);
        }

        [Test]
        public void PauseFromLevelUp_ResumesToLevelUp()
        {
            var fsm = new GameStateMachine(GameState.Play);
            fsm.TryChangeState(GameState.LevelUp);
            Assert.IsTrue(fsm.TryChangeState(GameState.Pause), "카드 선택 중 일시정지");
            Assert.AreEqual(GameState.LevelUp, fsm.StateBeforePause);
            Assert.IsFalse(fsm.TryChangeState(GameState.Play), "LevelUp에서 멈췄으면 Play로 건너뛸 수 없다");
            Assert.IsTrue(fsm.TryResume());
            Assert.AreEqual(GameState.LevelUp, fsm.Current, "재개하면 카드 선택으로 복귀");
        }

        [Test]
        public void PauseToTitle_IsAllowed_FromEither()
        {
            var fsm = new GameStateMachine(GameState.Play);
            fsm.TryChangeState(GameState.LevelUp);
            fsm.TryChangeState(GameState.Pause);
            Assert.IsTrue(fsm.TryChangeState(GameState.Title));
        }

        [Test]
        public void Resume_WhenNotPaused_IsRejected()
        {
            var fsm = new GameStateMachine(GameState.Play);
            Assert.IsFalse(fsm.TryResume());
            Assert.AreEqual(GameState.Play, fsm.Current);
        }
    }
}
