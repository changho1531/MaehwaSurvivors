namespace Game.Core
{
    /// <summary>게임 흐름 상태 (설계서 4절).</summary>
    public enum GameState
    {
        Title,
        Play,
        Pause,
        LevelUp,
        GameOver,
        GameClear
    }
}
