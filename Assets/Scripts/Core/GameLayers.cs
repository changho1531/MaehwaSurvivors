namespace Game.Core
{
    /// <summary>코드에서 쓰는 레이어 이름. 레이어 자체는 Editor/ProjectLayers.cs가 프로젝트에 등록한다.</summary>
    public static class GameLayers
    {
        /// <summary>경험치 구슬 전용. 다른 어떤 레이어와도 충돌하지 않고, PlayerMagnet의 조회에만 쓰인다.</summary>
        public const string Xp = "XP";

        /// <summary>모든 적. 적끼리는 물리 충돌하지 않는다 (7-B 규칙 1-A) — 겹쳐 몰려와야 포위 시 접촉 데미지가 마리 수만큼 합산된다.</summary>
        public const string Enemy = "Enemy";
    }
}
