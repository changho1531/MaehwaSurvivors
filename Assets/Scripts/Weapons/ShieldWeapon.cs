using System;
using Game.Player;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 주기형 피해 감소막 (자하호신강, 설계서 7-G절). 공통 Weapon 루프에서 "막이 켜져 있는 시간 = 공격 진행 시간"으로 본다.
    /// 그래서 "막이 꺼진 뒤부터 쿨타임"과 "켜진 채로 다시 켜지지 않음"이 추가 코드 없이 루프 구조로 보장된다.
    /// 감소 자체는 PlayerHealth의 받는 피해 배율 하나로 처리하므로, 데미지 공급원은 막의 존재를 모른다.
    /// </summary>
    public class ShieldWeapon : Weapon<ShieldWeaponData>
    {
        PlayerHealth health;
        ShieldEffect effect;
        float remaining;
        bool healOnEnd; // 켜질 때 정한다: 이번 막은 켜질 때 수치로 끝까지 (중간 레벨업은 다음 발동부터)

        /// <summary>(감소율, 지속시간)</summary>
        public event Action<float, float> ShieldStarted;
        /// <summary>(회복량, 0이면 회복 없음)</summary>
        public event Action<float> ShieldEnded;

        public bool IsShieldActive => IsAttacking;
        public ShieldEffect Effect => effect;

        protected override void OnInit()
        {
            health = GetComponent<PlayerHealth>();
            if (Data.effectPrefab != null)
            {
                // 막은 플레이어를 따라다닌다 (바닥에 고정된 장판과 구분, 7-G 연출)
                effect = Instantiate(Data.effectPrefab, transform);
                effect.transform.localPosition = OriginOffset;
                effect.Bind(health);
            }
        }

        protected override bool TryStartAttack()
        {
            float reduction = Data.GetReduction(Level);
            float duration = Data.GetDuration(Level);
            remaining = duration;
            healOnEnd = Level >= Data.maxLevel;

            if (health != null)
                health.IncomingDamageMultiplier = 1f - reduction;
            if (effect != null)
                effect.Begin(duration);

            ShieldStarted?.Invoke(reduction, duration);
            return true; // 적 유무와 관계없이 켜진다
        }

        protected override bool TickAttack(float dt)
        {
            remaining -= dt;
            if (remaining > 0f)
                return false;

            EndShield();
            return true; // 여기서부터 공통 루프가 쿨타임을 센다
        }

        void EndShield()
        {
            float healed = 0f;
            if (health != null)
            {
                health.IncomingDamageMultiplier = 1f;
                if (healOnEnd && !health.IsDead)
                    healed = health.Heal(Data.GetHealAmount(Level, health.CurrentHp, health.MaxHp));
            }
            if (effect != null)
                effect.End(healed);
            ShieldEnded?.Invoke(healed);
        }

        void OnDestroy()
        {
            // 막이 켜진 채 무기가 사라져도 감소가 남지 않게
            if (IsAttacking && health != null)
                health.IncomingDamageMultiplier = 1f;
        }
    }
}
