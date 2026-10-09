using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 모든 무기의 공통 쿨타임 루프 (설계서 7절 "언제 발동하나"는 공통).
    /// 쿨타임 종료 → TryStartAttack() → 공격이 끝날 때까지 TickAttack() → 끝난 뒤부터 다시 쿨타임.
    /// 하위 클래스는 "어떻게 공격하나"만 구현하므로, 새 무기 유형은 클래스 1개 추가로 끝난다 (OCP).
    /// 공격이 끝난 뒤에 쿨타임을 시작하므로, 쿨이 짧아져도 지속형 공격(콤보)이 겹치지 않는다.
    /// </summary>
    public abstract class Weapon : MonoBehaviour
    {
        float cooldownRemaining;

        public bool IsAttacking { get; private set; }

        protected abstract float Cooldown { get; }

        /// <summary>공격을 시작할 수 있으면 시작하고 true. 조건이 안 맞으면 false (쿨타임 끝난 상태로 대기).</summary>
        protected abstract bool TryStartAttack();

        /// <summary>진행 중인 공격을 dt만큼 진행하고, 끝났으면 true. 즉발형은 기본 구현 그대로 바로 끝난다.</summary>
        protected virtual bool TickAttack(float dt) => true;

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return; // 일시정지 중에는 쿨타임·공격 진행 모두 정지

            if (IsAttacking)
            {
                if (TickAttack(dt))
                    FinishAttack();
                return;
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= dt;
                return;
            }

            if (!TryStartAttack())
                return;

            IsAttacking = true;
            if (TickAttack(0f))
                FinishAttack();
        }

        void FinishAttack()
        {
            IsAttacking = false;
            cooldownRemaining = Cooldown;
        }
    }
}
