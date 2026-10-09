using Game.Player;
using Game.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 상단 HUD (MU-B-001, 설계서 7-B 규칙 8): HP 바, 그 아래 경험치 바 + "Lv N".
    /// PlayerHealth/PlayerLevel 이벤트를 구독해 바뀔 때만 갱신한다 (Observer). 생존 시간은 SurvivalTimeView 담당.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] PlayerLevel level;
        [SerializeField] UiBar hpBar;
        [SerializeField] UiBar xpBar;
        [SerializeField] Text levelText;

        public UiBar HpBar => hpBar;
        public UiBar XpBar => xpBar;
        public Text LevelText => levelText;

        void OnEnable()
        {
            health.HealthChanged += OnHealthChanged;
            level.XpChanged += OnXpChanged;
            level.Maxed += OnMaxed;
        }

        void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged;
            level.XpChanged -= OnXpChanged;
            level.Maxed -= OnMaxed;
        }

        void Start()
        {
            // 이벤트가 오기 전 초기 상태 (PlayerHealth.Start의 첫 알림보다 늦게 붙는 경우 대비)
            OnHealthChanged(health.CurrentHp, health.MaxHp);
            OnXpChanged(level.CurrentXp, level.RequiredXp);
        }

        void OnHealthChanged(float current, float max) => hpBar.Set(current, max);

        void OnXpChanged(int current, int required)
        {
            xpBar.Set(current, required);
            levelText.text = level.IsMaxed ? $"Lv {level.Level} MAX" : $"Lv {level.Level}";
        }

        void OnMaxed() => OnXpChanged(level.RequiredXp, level.RequiredXp);
    }
}
