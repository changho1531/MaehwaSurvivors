using Game.Enemies;
using Game.Enemies.Boss;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 보스 등장 · 페이즈 전환 알림 (설계서 8-C 연출): 상단 중앙 "혈각마존 출현" 2초(웨이브 문구와 같은 자리) +
    /// 화면 가장자리 붉은 번쩍임. 페이즈 전환 때도 가장자리가 번쩍인다. 이벤트만 구독한다 (Observer).
    /// 번쩍임은 게임 시간 기준이라 일시정지 중에는 멈춘다.
    /// </summary>
    public class BossAlertView : MonoBehaviour
    {
        [SerializeField] BossSpawner spawner;
        [SerializeField] WaveBanner banner;
        [Header("※ 예시값")]
        [Tooltip("{0} = 보스 이름")]
        [SerializeField] string appearFormat = "{0} 출현";
        [SerializeField, Min(0.1f)] float bannerSeconds = 2f;
        [SerializeField, Min(0.05f)] float flashSeconds = 0.6f;
        [SerializeField] Color flashColor = new(0.9f, 0.05f, 0.05f, 0.75f);

        Image flash;
        float flashRemaining;
        BossBehaviour behaviour;

        public bool IsFlashing => flashRemaining > 0f;

        void Awake()
        {
            var go = new GameObject("EdgeFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling(); // HUD 글자 · 바보다 뒤
            flash = go.GetComponent<Image>();
            flash.sprite = UiSprites.EdgeGlow;
            flash.raycastTarget = false;
            SetFlashAlpha(0f);
        }

        void OnEnable()
        {
            if (spawner != null)
                spawner.BossSpawned += OnBossSpawned;
        }

        void OnDisable()
        {
            if (spawner != null)
                spawner.BossSpawned -= OnBossSpawned;
            if (behaviour != null)
                behaviour.PhaseTransitionStarted -= Flash;
            behaviour = null;
        }

        void OnBossSpawned(Enemy boss)
        {
            behaviour = boss.GetComponent<BossBehaviour>();
            string name = behaviour != null && behaviour.Data != null ? behaviour.Data.displayName : boss.name;
            if (banner != null)
                banner.ShowText(string.Format(appearFormat, name), bannerSeconds);
            if (behaviour != null)
                behaviour.PhaseTransitionStarted += Flash;
            Flash();
        }

        public void Flash() => flashRemaining = flashSeconds;

        void Update()
        {
            if (flashRemaining <= 0f)
                return;
            flashRemaining = Mathf.Max(0f, flashRemaining - Time.deltaTime);
            // 빠르게 켜졌다 천천히 사라짐
            float t = 1f - flashRemaining / flashSeconds;
            SetFlashAlpha(t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f);
        }

        void SetFlashAlpha(float k)
        {
            var c = flashColor;
            c.a *= Mathf.Clamp01(k);
            flash.color = c;
        }
    }
}
