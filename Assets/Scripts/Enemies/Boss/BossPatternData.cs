using Game.Visuals;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 보스 패턴 하나의 데이터 (설계서 8-C절). 공통 = 사용 조건(페이즈 · 거리), 페이즈별 쿨타임, 경고 시간, 데미지.
    /// 패턴별 수치는 하위 클래스에. 패턴 추가 = 데이터 클래스 + 런타임 클래스 한 쌍 + 에셋 1개, BossBehaviour 수정 없음 —
    /// 무기(WeaponData → Weapon)와 같은 구조 (OCP). 모든 수치는 ※ 예시값.
    /// </summary>
    public abstract class BossPatternData : ScriptableObject
    {
        [Tooltip("이벤트 · 테스트 · 측정 로그에 쓰는 이름")]
        public string displayName = "패턴";
        [Tooltip("이 페이즈부터 사용 (1 = 처음부터, 2 = 2페이즈부터)")]
        [Range(1, 2)] public int minPhase = 1;
        [Tooltip("플레이어와의 거리가 이 범위 안일 때만 후보")]
        [Min(0f)] public float minDistance;
        [Min(0f)] public float maxDistance = 999f;
        [Tooltip("[0] = 1페이즈, [1] = 2페이즈. 패턴이 끝난 뒤부터 센다")]
        public float[] cooldownPerPhase = { 3f, 2.1f };
        [Tooltip("경고 시간 — 방향은 경고가 시작되는 순간 고정")]
        [Min(0f)] public float warningSeconds = 0.6f;
        [Min(0f)] public float damage = 15f;
        [Tooltip("(선택) 실행 순간 재생할 이펙트. 공격 방향으로 돌려 공유 풀에서 꺼낸다 (보스가 사라져도 끝까지 재생)")]
        public BurstEffect effectPrefab;

        public bool IsAllowed(int phase, float distance) =>
            phase >= minPhase && distance >= minDistance && distance <= maxDistance;

        public float GetCooldown(int phase) => Mathf.Max(0f, LevelTable.Get(cooldownPerPhase, phase));

        /// <summary>보스마다 진행 상태(쿨타임 · 경과 시간)를 따로 가지도록 런타임 객체를 만든다.</summary>
        public abstract BossPattern CreateRuntime();
    }
}
