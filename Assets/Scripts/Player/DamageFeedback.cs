using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 피격 연출 (설계서 7-B 규칙 3): 데미지를 받는 동안 캐릭터 빨간 틴트 유지 + HP 바 흔들림,
    /// 데미지가 멈추면 틴트는 서서히 원래 색으로 돌아온다. 무적 깜빡임이 없으므로 "지금 맞고 있다"를 양쪽에서 알린다.
    /// PlayerHealth.Damaged만 구독하므로 HP 로직은 연출을 모른다.
    /// </summary>
    public class DamageFeedback : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] SpriteRenderer target;
        [Tooltip("흔들 HP 바 (HUD)")]
        [SerializeField] RectTransform hpBar;

        [Header("※ 예시값")]
        [SerializeField] Color tintColor = new(1f, 0.35f, 0.35f, 1f);
        [Tooltip("마지막 피격 후 이 시간 동안은 '맞는 중'으로 본다 (접촉 데미지는 물리 스텝마다 들어오므로 그 간격보다 길게)")]
        [SerializeField, Min(0.01f)] float holdSeconds = 0.1f;
        [Tooltip("맞는 중이 끝난 뒤 원래 색으로 돌아오는 시간")]
        [SerializeField, Min(0.01f)] float recoverSeconds = 0.3f;
        [Tooltip("HP 바 흔들림 세기 (UI 픽셀)")]
        [SerializeField, Min(0f)] float shakeStrength = 6f;

        float lastDamageTime = float.NegativeInfinity;
        Vector2 hpBarBasePosition;

        public bool IsBeingHit => Time.time - lastDamageTime <= holdSeconds;
        public Color CurrentTint => target != null ? target.color : Color.white;

        void Awake()
        {
            if (hpBar != null)
                hpBarBasePosition = hpBar.anchoredPosition;
        }

        void OnEnable() => health.Damaged += OnDamaged;
        void OnDisable() => health.Damaged -= OnDamaged;

        void OnDamaged(float amount) => lastDamageTime = Time.time;

        void LateUpdate()
        {
            // 게임 시간 기준이라 일시정지·GameOver 중에는 틴트가 그 상태로 멈춘다 (멈춘 화면 그대로 결과 화면 배경).
            float since = Time.time - lastDamageTime;
            float t = since <= holdSeconds ? 1f : 1f - Mathf.Clamp01((since - holdSeconds) / recoverSeconds);
            if (target != null)
                target.color = Color.Lerp(Color.white, tintColor, t);

            if (hpBar != null)
            {
                bool shaking = IsBeingHit && Time.timeScale > 0f;
                hpBar.anchoredPosition = shaking ? hpBarBasePosition + Random.insideUnitCircle * shakeStrength : hpBarBasePosition;
            }
        }
    }
}
