using Game.Visuals;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 저주술사 데이터 (설계서 8-B절). 공통 스탯은 EnemyData, 저주 장판 · 소환 값만 여기에. 모든 수치는 ※ 예시값.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Sorcerer Enemy Data", fileName = "SorcererEnemyData")]
    public class SorcererEnemyData : EnemyData
    {
        [Header("이동 (8-B 규칙 1)")]
        [Tooltip("이보다 멀면 다가오고, 안이면 멈춘다. 뒤로 물러나지 않음")]
        [Min(0f)] public float keepDistance = 4.5f;

        [Header("저주 장판 (규칙 2~5)")]
        [Tooltip("쏘는 순간부터 다음 주기")]
        [Min(0.1f)] public float castInterval = 5f;
        [Tooltip("경고 시간 — 테두리만 깜빡이고 판정 없음")]
        [Min(0f)] public float zoneWarningSeconds = 1f;
        [Min(0.1f)] public float zoneSeconds = 3f;
        [Min(0.1f)] public float zoneRadius = 1.5f;
        [Min(0f)] public float zoneDamagePerSecond = 5f;
        public DamageZone zonePrefab;

        [Header("저주 (중첩 없음, 매화보와 곱연산)")]
        [Range(0f, 0.9f)] public float curseSlowRatio = 0.2f;
        [Tooltip("장판을 벗어난 뒤 유지 시간")]
        [Min(0f)] public float curseSeconds = 2f;

        [Header("소환 (규칙 6)")]
        [Min(0.1f)] public float summonInterval = 10f;
        [Min(0)] public int summonCount = 2;
        [Min(0f)] public float summonRadius = 1.5f;
        [Tooltip("불러내는 적 (도병). 일반 도병과 같은 프리팹 = 같은 풀")]
        public Enemy summonPrefab;
        [Tooltip("(선택) 불러낸 자리마다 1회 재생하는 연기. 저주술사가 죽어도 끝까지 재생되도록 공유 풀에서 꺼낸다")]
        public BurstEffect summonEffectPrefab;

        [Header("연출")]
        [Tooltip("장판을 쏠 때 손을 드는 떨림 시간 ※")]
        [Min(0f)] public float castShakeSeconds = 0.3f;
        [Min(0f)] public float shakeAmplitude = 0.05f;
        [Min(0f)] public float shakeFrequency = 30f;
    }
}
