using Game.Player;
using Game.Progression;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 최소 플레이 HUD (MU-B-001): 상단 HP·XP 바, 레벨, 우측 상단 생존 시간.
    /// HP·XP는 PlayerHealth/PlayerLevel 이벤트를 구독해 바뀔 때만 갱신하고 (Observer),
    /// 시간은 RunStats 값을 초가 바뀔 때만 다시 쓴다.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] PlayerLevel level;
        [SerializeField, FormerlySerializedAs("clock")] RunStats stats;
        [SerializeField] UiBar hpBar;
        [SerializeField] UiBar xpBar;
        [SerializeField] Text levelText;
        [SerializeField] Text timeText;

        int shownSecond = -1;

        public UiBar HpBar => hpBar;
        public UiBar XpBar => xpBar;
        public Text LevelText => levelText;
        public Text TimeText => timeText;

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

        void Update()
        {
            int second = Mathf.FloorToInt(stats.Elapsed);
            if (second == shownSecond)
                return;
            shownSecond = second;
            timeText.text = RunStats.Format(stats.Elapsed);
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
