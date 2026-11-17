using System;
using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 플레이어 HP (설계서 7-B절). 데미지 공급원(근거리 접촉, 이후 화살·장판)은 TakeDamage만 호출하고,
    /// 사망 판정·GameOver 전이는 여기서만 한다. 연출(DamageFeedback)과 HUD는 이벤트를 구독한다 (Observer).
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] PlayerData data;

        Health health;

        /// <summary>(받은 데미지) — 피격 연출용</summary>
        public event Action<float> Damaged;
        /// <summary>(현재 HP, 최대 HP) — HUD용</summary>
        public event Action<float, float> HealthChanged;
        /// <summary>사망. 한 런에 1회만 발행된다.</summary>
        public event Action Died;
        /// <summary>(실제로 찬 양) — 회복 표시용</summary>
        public event Action<float> Healed;

        public PlayerData Data => data;
        public float CurrentHp => Health.Current;
        public float MaxHp => Health.Max;
        public bool IsDead => Health.IsDead;
        /// <summary>디버그 무적 (F2, 시연·측정용). 켜져 있으면 데미지를 받지 않는다.</summary>
        public bool Invincible { get; set; }

        /// <summary>보스 처치 연출 중처럼 "이미 이긴" 동안 데미지를 받지 않는다 (8-C 예외). 디버그 무적과 따로 둔다.</summary>
        public bool DamageLocked { get; set; }

        /// <summary>사망한 프레임 (-1 = 살아 있음). 보스 처치와 같은 프레임이면 클리어 우선 (8-C 예외).</summary>
        public int DiedFrame { get; private set; } = -1;

        /// <summary>
        /// 받는 피해 배율 (기본 1, 자하호신강 막이 켜지면 1 − 감소율, 7-G절).
        /// 모든 데미지가 TakeDamage 한 곳을 지나므로, 공급원(접촉·화살·이후 보스)이 늘어도 감소를 따로 처리하지 않는다.
        /// </summary>
        public float IncomingDamageMultiplier { get; set; } = 1f;
        public bool IsDamageReduced => IncomingDamageMultiplier < 1f;

        Health Health => health ??= new Health(data.maxHp);

        void Start()
        {
            HealthChanged?.Invoke(Health.Current, Health.Max); // HUD 초기값
        }

        /// <summary>데미지 적용. 사망 후·일시정지 중이면 무시하고 false.</summary>
        public bool TakeDamage(float amount)
        {
            // 일시정지(LevelUp·Pause·GameOver) 중에는 전투가 멈춘 상태이므로 데미지도 받지 않는다.
            if (Time.timeScale <= 0f || Invincible || DamageLocked)
                return false;
            amount *= Mathf.Max(0f, IncomingDamageMultiplier);
            if (!Health.TryDamage(amount, out bool justDied))
                return false;

            Damaged?.Invoke(amount);
            HealthChanged?.Invoke(Health.Current, Health.Max);
            if (justDied)
            {
                DiedFrame = Time.frameCount;
                Died?.Invoke();
                // GameOver로 전이하면 TimeScalePolicy가 게임 시간을 멈춘다 (적·무기·구슬·타이머 정지, 7-B 규칙 5).
                // 같은 프레임에 레벨업이 겹쳐도 FSM이 GameOver → LevelUp을 막는다 (6-A절 예외: GameOver 우선).
                GameManager.TryChangeState(GameState.GameOver);
            }
            return true;
        }

        /// <summary>회복. 실제로 찬 양을 돌려주며, 0이면 이벤트를 내지 않는다 (체력이 가득이면 표시 없음).</summary>
        public float Heal(float amount)
        {
            float healed = Health.Heal(amount);
            if (healed <= 0f)
                return 0f;

            Healed?.Invoke(healed);
            HealthChanged?.Invoke(Health.Current, Health.Max);
            return healed;
        }
    }
}
