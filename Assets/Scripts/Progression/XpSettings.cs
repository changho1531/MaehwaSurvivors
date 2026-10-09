using UnityEngine;

namespace Game.Progression
{
    /// <summary>경험치 구슬·자석 수치 (설계서 6-A절). 모든 값은 ※ 예시값.</summary>
    [CreateAssetMenu(menuName = "Game/XP Settings", fileName = "XpSettings")]
    public class XpSettings : ScriptableObject
    {
        public XpOrb orbPrefab;

        [Tooltip("적이 죽고 구슬이 생길 때까지의 게임 시간(초)")]
        [Min(0f)] public float spawnDelay = 0.3f;
        [Tooltip("사망 위치 기준 위쪽으로 띄우는 거리")]
        public float spawnOffsetY = 0.5f;
        [Tooltip("자석 반경 (고정, 성장 없음)")]
        [Min(0f)] public float magnetRadius = 2.5f;
        [Tooltip("끌림 속도. 플레이어 최고 속도보다 느리면 PlayerMagnet이 자동으로 올려 쓴다")]
        [Min(0f)] public float pullSpeed = 10f;
        [Tooltip("플레이어 중심에서 이 거리 안에 들어오면 습득")]
        [Min(0.01f)] public float pickupDistance = 0.3f;
        [Tooltip("카드가 새로 뜬 뒤 클릭을 무시하는 실시간(초)")]
        [Min(0f)] public float cardInputLockSeconds = 0.2f;
        [Min(0)] public int prewarmCount = 200;
    }
}
