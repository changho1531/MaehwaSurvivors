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

        public PlayerData Data => data;
        public float CurrentHp => Health.Current;
        public float MaxHp => Health.Max;
        public bool IsDead => Health.IsDead;

        Health Health => health ??= new Health(data.maxHp);

        void Start()
        {
            HealthChanged?.Invoke(Health.Current, Health.Max); // HUD 초기값
        }

        /// <summary>데미지 적용. 사망 후·일시정지 중이면 무시하고 false.</summary>
        public bool TakeDamage(float amount)
        {
            // 일시정지(LevelUp·Pause·GameOver) 중에는 전투가 멈춘 상태이므로 데미지도 받지 않는다.
            if (Time.timeScale <= 0f)
                return false;
            if (!Health.TryDamage(amount, out bool justDied))
                return false;

            Damaged?.Invoke(amount);
            HealthChanged?.Invoke(Health.Current, Health.Max);
            if (justDied)
            {
                Died?.Invoke();
                // GameOver로 전이하면 TimeScalePolicy가 게임 시간을 멈춘다 (적·무기·구슬·타이머 정지, 7-B 규칙 5).
                // 같은 프레임에 레벨업이 겹쳐도 FSM이 GameOver → LevelUp을 막는다 (6-A절 예외: GameOver 우선).
                GameManager.Instance.StateMachine.TryChangeState(GameState.GameOver);
            }
            return true;
        }
    }
}
