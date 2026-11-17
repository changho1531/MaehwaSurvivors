using System;
using Game.Player;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 상시 이동속도 증가 (매화보, 설계서 7-H절). 쿨타임 개념이 없으므로 공통 공격 루프를 쓰지 않고(Update를 대체),
    /// 카드·레벨 관리만 Weapon 틀을 쓴다. 획득·강화 즉시 PlayerMovement의 이동속도 배율을 갱신한다.
    /// 최대 레벨에서는 마지막으로 맞은 뒤 일정 시간이 지나면 추가 속도를 켜고, PlayerHealth.Damaged가 오면 바로 끈다.
    /// </summary>
    public class MovementPassive : Weapon<MovementPassiveData>
    {
        PlayerMovement movement;
        PlayerHealth health;
        PetalTrail petals;
        float sinceLastHit;

        /// <summary>(가속 켜짐 여부) — 꽃잎 연출·테스트용</summary>
        public event Action<bool> BoostChanged;

        public bool IsBoosted { get; private set; }
        public float SinceLastHit => sinceLastHit;
        public PetalTrail Petals => petals;

        protected override void OnInit()
        {
            movement = GetComponent<PlayerMovement>();
            health = GetComponent<PlayerHealth>();
            if (health != null)
                health.Damaged += OnDamaged;
            if (Data.petalPrefab != null)
                petals = Instantiate(Data.petalPrefab, transform); // 발밑에서 흩날린다
            sinceLastHit = 0f;
            ApplyMultiplier();
        }

        protected override void OnLevelChanged()
        {
            // 최대 레벨이 된 순간부터 센다 (그 전에 안 맞은 시간은 치지 않음)
            if (Level >= Data.maxLevel)
                sinceLastHit = 0f;
            ApplyMultiplier();
        }

        // 공격하지 않는다. 공통 루프를 쓰지 않으므로 호출되지 않는다
        protected override bool TryStartAttack() => false;

        /// <summary>쿨타임이 없어 항상 준비 완료(덮개 없음), 최대 레벨 가속 중에만 강조 (7-J 규칙 4).</summary>
        public override WeaponSlotState GetSlotState() => new(1f, false, 0f, IsBoosted);

        protected override void Update()
        {
            float dt = Time.deltaTime;
            if (Data == null || dt <= 0f)
                return; // 일시정지 중에는 무피격 시간도 멈춘다

            if (Level >= Data.maxLevel && !IsBoosted)
            {
                sinceLastHit += dt;
                if (sinceLastHit >= Data.boostDelaySeconds)
                    SetBoost(true);
            }

            if (petals != null)
                petals.SetState(movement != null && movement.CurrentInput != Vector2.zero, IsBoosted);
        }

        void OnDamaged(float amount)
        {
            // 줄어든 피해(자하호신강)도 "맞은 것" — 버티기와 피하기의 역할을 섞지 않는다
            sinceLastHit = 0f;
            if (IsBoosted)
                SetBoost(false);
        }

        void SetBoost(bool on)
        {
            IsBoosted = on;
            ApplyMultiplier();
            BoostChanged?.Invoke(on);
        }

        void ApplyMultiplier()
        {
            if (movement != null)
                movement.SpeedMultiplier = Data.GetSpeedMultiplier(Level, IsBoosted);
        }

        void OnDestroy()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
            if (movement != null)
                movement.SpeedMultiplier = 1f;
        }
    }
}
