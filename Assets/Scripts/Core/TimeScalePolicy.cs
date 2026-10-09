namespace Game.Core
{
    /// <summary>
    /// 상태별 게임 시간 배율. 일시정지 규칙을 한곳에 모아 두어, 각 시스템은 Time.deltaTime만 쓰면
    /// Pause·LevelUp 중에 자동으로 멈춘다 (설계서 6-A절 "일시정지 규칙을 하나로 통일").
    /// </summary>
    public static class TimeScalePolicy
    {
        public static float For(GameState state)
        {
            return state is GameState.Pause or GameState.LevelUp or GameState.GameOver or GameState.GameClear ? 0f : 1f;
        }
    }
}
