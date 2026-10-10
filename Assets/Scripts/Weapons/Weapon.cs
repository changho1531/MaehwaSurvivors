using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 모든 무기의 공통 쿨타임 루프 (설계서 7절 "언제 발동하나"는 공통).
    /// 쿨타임 종료 → TryStartAttack() → 공격이 끝날 때까지 TickAttack() → 끝난 뒤부터 다시 쿨타임.
    /// 하위 클래스는 "어떻게 공격하나"만 구현하므로, 새 무기 유형은 클래스 1개 추가로 끝난다 (OCP).
    /// 공격이 끝난 뒤에 쿨타임을 시작하므로, 쿨이 짧아져도 지속형 공격(콤보)이 겹치지 않는다.
    /// </summary>
    [DefaultExecutionOrder(20)] // PlayerFacing(10)이 이번 프레임 방향을 정한 뒤에 공격한다
    public abstract class Weapon : MonoBehaviour
    {
        float cooldownRemaining;

        public WeaponData Data { get; private set; }
        public int Level { get; private set; }
        public bool IsAttacking { get; private set; }

        /// <summary>플레이어 원점(발 위치)에서 무기 원점(몸통 중앙)까지. 발밑에서 칼이 나가 보이지 않게 (7절 무기 원점).</summary>
        public Vector2 OriginOffset { get; set; }

        /// <summary>투사체 발사 위치·부채꼴 판정 중심·이펙트 피벗의 공통 기준점.</summary>
        public Vector2 Origin => (Vector2)transform.position + OriginOffset;

        /// <summary>WeaponInventory가 무기를 붙인 직후 한 번 호출한다.</summary>
        public void Init(WeaponData data, int level)
        {
            Data = data;
            Level = level;
            OnInit();
        }

        /// <summary>레벨 변경. 진행 중인 공격은 시작할 때 잡아 둔 수치로 끝까지 가고, 다음 발동부터 새 수치를 쓴다.</summary>
        public void SetLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, Data.maxLevel);
        }

        protected virtual void OnInit() { }

        /// <summary>공격을 시작할 수 있으면 시작하고 true. 조건이 안 맞으면 false (쿨타임 끝난 상태로 대기).</summary>
        protected abstract bool TryStartAttack();

        /// <summary>진행 중인 공격을 dt만큼 진행하고, 끝났으면 true. 즉발형은 기본 구현 그대로 바로 끝난다.</summary>
        protected virtual bool TickAttack(float dt) => true;

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            if (Data == null || dt <= 0f)
                return; // 일시정지 중에는 쿨타임·공격 진행 모두 정지

            if (IsAttacking)
            {
                if (!TickAttack(dt))
                    return;
                FinishAttack();
                // 쿨타임 0(최대 레벨 연속 발동)이면 같은 프레임에 다음 공격을 시작해 이펙트가 끊기지 않게 한다
                if (cooldownRemaining > 0f)
                    return;
            }
            else if (cooldownRemaining > 0f)
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
            cooldownRemaining = Data.GetCooldown(Level);
        }
    }

    /// <summary>데이터 타입을 고정한 무기 베이스. 하위 클래스가 매번 캐스팅하지 않게 한다.</summary>
    public abstract class Weapon<TData> : Weapon where TData : WeaponData
    {
        public new TData Data => (TData)base.Data;
    }
}
