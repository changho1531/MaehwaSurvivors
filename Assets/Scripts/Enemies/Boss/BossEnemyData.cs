using Game.Weapons;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 보스 혈각마존 데이터 (설계서 8-C절). 공통 스탯은 EnemyData(HP · 이동속도 · 접촉 데미지 · 넉백 면역),
    /// 페이즈 · 틈 · 패턴 목록은 여기에. 패턴별 수치는 각 패턴 에셋에 있다. 모든 수치는 ※ 예시값 (HP는 측정 후 확정).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss Enemy Data", fileName = "BossEnemyData")]
    public class BossEnemyData : EnemyData
    {
        public string displayName = "혈각마존";

        [Header("페이즈 · 틈 (8-C 패턴 선택 규칙)")]
        [Tooltip("HP가 이 비율 이하가 되면 (진행 중 패턴을 마친 뒤) 2페이즈")]
        [Range(0.05f, 0.95f)] public float phase2HpRatio = 0.5f;
        [Tooltip("패턴 사이 틈 [0] = 1페이즈, [1] = 2페이즈. 이 동안 추적만 = 반격 시간")]
        public float[] gapSecondsPerPhase = { 0.8f, 0.6f };
        [Tooltip("후보가 없을 때 다시 확인하는 간격")]
        [Min(0.02f)] public float retrySeconds = 0.2f;
        [Tooltip("페이즈 전환 연출 (멈추고 붉게 번쩍임, 공격 안 함)")]
        [Min(0f)] public float phaseTransitionSeconds = 1f;

        [Header("패턴")]
        public BossPatternData[] patterns = System.Array.Empty<BossPatternData>();
        [Tooltip("2페이즈 첫 패턴 (거리와 상관없이 한 번) — 지면 강타")]
        public BossPatternData phase2OpeningPattern;

        public float GetGap(int phase) => Mathf.Max(0f, LevelTable.Get(gapSecondsPerPhase, phase));
    }
}
