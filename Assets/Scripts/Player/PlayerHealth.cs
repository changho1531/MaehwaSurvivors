using System;
using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 플레이어 HP (설계서 4절 "Play → GameOver: HP 0 이하"). 데미지를 주는 쪽(적 접촉, 이후 화살 등)은
    /// TakeDamage만 호출하고, 무적 시간·사망 판정·GameOver 전이는 여기서만 한다.
    /// HUD는 HealthChanged 이벤트를 구독한다 (UI와 직접 참조 없이 Observer).
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] PlayerData data;

        SpriteRenderer spriteRenderer;
        Health health;

        /// <summary>(현재 HP, 최대 HP)</summary>
        public event Action<float, float> HealthChanged;
        public event Action Died;

        public PlayerData Data => data;
        public float CurrentHp => health.Current;
        public float MaxHp => health.Max;
        public bool IsDead => health.IsDead;
        public bool IsInvulnerable => health.IsInvulnerable(Time.time);

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            health = new Health(data.maxHp, data.invulnerabilitySeconds);
        }

        void Start()
        {
            HealthChanged?.Invoke(health.Current, health.Max); // HUD 초기값
        }

        /// <summary>데미지 적용. 무적 중·사망 후·일시정지 중이면 무시하고 false.</summary>
        public bool TakeDamage(float amount)
        {
            // 일시정지(LevelUp·Pause) 중에는 전투가 멈춘 상태이므로 데미지도 받지 않는다.
            if (Time.timeScale <= 0f)
                return false;
            if (!health.TryDamage(amount, Time.time))
                return false;

            HealthChanged?.Invoke(health.Current, health.Max);
            if (health.IsDead)
            {
                Died?.Invoke();
                // 같은 프레임에 레벨업이 겹쳐도 GameOver가 먼저 들어가면 FSM이 GameOver → LevelUp을 막는다 (6-A절 예외: GameOver 우선).
                GameManager.Instance.StateMachine.TryChangeState(GameState.GameOver);
            }
            return true;
        }

        void Update()
        {
            // 무적 시간 동안 깜빡여 피격을 알린다 (임시 연출).
            if (spriteRenderer == null)
                return;
            bool flash = !health.IsDead && health.IsInvulnerable(Time.time) && Mathf.Repeat(Time.time, 0.1f) < 0.05f;
            spriteRenderer.color = flash ? data.hitFlashColor : Color.white;
        }
    }
}
