using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 게임 흐름 FSM. 허용된 전이만 통과시키고, 전이가 일어나면 StateChanged 이벤트로 알린다.
    /// 전이 규칙은 설계서 4절을 그대로 옮긴 표이며, 상태별 실제 동작(씬 이동 등)은 구독자가 담당한다.
    /// Pause는 진입 전 상태(Play 또는 LevelUp)를 기억하고, 재개하면 그 상태로만 돌아간다 (7-C절).
    /// → 카드 선택 중에 멈춰도 재개하면 카드 선택이 이어지고, Play에서 멈췄는데 LevelUp으로 빠지는 일은 구조적으로 막힌다.
    /// </summary>
    public class GameStateMachine
    {
        static readonly Dictionary<GameState, GameState[]> Transitions = new()
        {
            { GameState.Title, new[] { GameState.Play } },
            { GameState.Play, new[] { GameState.Pause, GameState.LevelUp, GameState.GameOver, GameState.GameClear } },
            { GameState.Pause, new[] { GameState.Play, GameState.LevelUp, GameState.Title } },
            { GameState.LevelUp, new[] { GameState.Play, GameState.Pause } },
            { GameState.GameOver, new[] { GameState.Play, GameState.Title } },
            { GameState.GameClear, new[] { GameState.Play, GameState.Title } },
        };

        /// <summary>(이전 상태, 새 상태)</summary>
        public event Action<GameState, GameState> StateChanged;

        public GameState Current { get; private set; }

        /// <summary>Pause에 들어오기 직전 상태. 재개할 곳이다. Pause가 아닐 때는 의미 없음.</summary>
        public GameState StateBeforePause { get; private set; } = GameState.Play;

        public GameStateMachine(GameState initial = GameState.Title)
        {
            Current = initial;
        }

        public static bool CanTransition(GameState from, GameState to)
        {
            return Transitions.TryGetValue(from, out var targets) && Array.IndexOf(targets, to) >= 0;
        }

        public bool TryChangeState(GameState next)
        {
            if (!CanTransition(Current, next))
                return false;
            // Pause에서 게임으로 돌아갈 때는 들어오기 전 상태로만 (Title로 나가는 것은 별개)
            if (Current == GameState.Pause && next != GameState.Title && next != StateBeforePause)
                return false;

            var previous = Current;
            if (next == GameState.Pause)
                StateBeforePause = previous;
            Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }

        /// <summary>Pause → 진입 전 상태로 복귀. Pause가 아니면 false.</summary>
        public bool TryResume()
        {
            return Current == GameState.Pause && TryChangeState(StateBeforePause);
        }
    }
}
