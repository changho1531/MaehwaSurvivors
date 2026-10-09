namespace Game.Core
{
    /// <summary>코드에서 쓰는 레이어 이름. 레이어 자체는 Editor/ProjectLayers.cs가 프로젝트에 등록한다.</summary>
    public static class GameLayers
    {
        /// <summary>경험치 구슬 전용. 다른 어떤 레이어와도 충돌하지 않고, PlayerMagnet의 조회에만 쓰인다.</summary>
        public const string Xp = "XP";
    }
}
