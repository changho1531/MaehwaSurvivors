using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적의 "어떻게 움직이고 공격하나" (설계서 8-A절 적 행동 분리). HP·피격·사망·풀 반환·접촉 데미지는 공통 Enemy가 맡고,
    /// 유형별 행동만 이 컴포넌트로 갈아 끼운다 — 무기의 공통 Weapon + 유형별 공격과 같은 구조.
    /// 새 적 유형 = 행동 클래스 1개 + 프리팹/데이터 추가, Enemy 수정 없음 (OCP).
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public abstract class EnemyBehaviour : MonoBehaviour
    {
        protected Enemy Owner { get; private set; }
        protected Rigidbody2D Body { get; private set; }

        protected virtual void Awake()
        {
            Owner = GetComponent<Enemy>();
            Body = GetComponent<Rigidbody2D>();
        }

        /// <summary>스폰(풀에서 꺼냄)마다 호출. 이전 생애의 타이머·상태를 지운다.</summary>
        public virtual void ResetState() { }

        /// <summary>물리 스텝마다 Enemy가 호출한다. 게임 시간이 멈추면 FixedUpdate도 멈추므로 일시정지 처리가 따로 필요 없다.</summary>
        public abstract void Tick(Transform target, float dt);
    }
}
