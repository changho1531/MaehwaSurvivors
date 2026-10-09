using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 게임 흐름 FSM. 허용된 전이만 통과시키고, 전이가 일어나면 StateChanged 이벤트로 알린다.
    /// 전이 규칙은 설계서 4절을 그대로 옮긴 표이며, 상태별 실제 동작(씬 이동 등)은 구독자가 담당한다.
    /// </summary>
    public class GameStateMachine
    {
        static readonly Dictionary<GameState, GameState[]> Transitions = new()
        {
            { GameState.Title, new[] { GameState.Play } },
            { GameState.Play, new[] { GameState.Pause, GameState.LevelUp, GameState.GameOver, GameState.GameClear } },
            { GameState.Pause, new[] { GameState.Play, GameState.Title } },
            { GameState.LevelUp, new[] { GameState.Play } },
            { GameState.GameOver, new[] { GameState.Play, GameState.Title } },
            { GameState.GameClear, new[] { GameState.Play, GameState.Title } },
        };

        /// <summary>(이전 상태, 새 상태)</summary>
        public event Action<GameState, GameState> StateChanged;

        public GameState Current { get; private set; }

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

            var previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }
    }
}
