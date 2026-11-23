using Game.Player;
using Game.Visuals;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 자하호신강 막 연출 (설계서 7-G 연출). 판정과 무관한 순수 표시.
    /// 막 프레임 그림(fillFrames)이 있으면 그것을 재생하고, 없으면 코드로 만든 원/링 스프라이트를 쓴다 (그림만 교체, 코드 그대로).
    /// 켜질 때 링이 퍼짐 → 켜져 있는 동안 숨 쉬기 → 맞으면 흰 번쩍임 → 마지막 1초 깜빡임 → 꺼질 때 회복량 표시.
    /// 모두 게임 시간 기준이라 일시정지 중에는 그대로 멈춘다.
    /// </summary>
    public class ShieldEffect : MonoBehaviour
    {
        [Tooltip("비우면 코드로 만든 부드러운 원")]
        [SerializeField] Sprite fillSprite;
        [Tooltip("비우면 코드로 만든 링")]
        [SerializeField] Sprite ringSprite;
        [Tooltip("(선택) 막 프레임 그림. 있으면 fillSprite 대신 재생 — 앞 loopFromFrame장은 펼쳐지는 연출 1회, 이후 반복. 그림 속 막 지름이 1칸이 되게 임포트")]
        [SerializeField] Sprite[] fillFrames;
        [SerializeField, Min(0.1f)] float framesPerSecond = 12f;
        [SerializeField, Min(0)] int loopFromFrame;
        [Tooltip("(선택) 막 머티리얼. 검은 배경 그림이면 Additive")]
        [SerializeField] Material fillMaterial;

        [Header("※ 예시값")]
        [Tooltip("막 반경(월드 단위). 캐릭터를 감싸는 정도")]
        [SerializeField, Min(0.1f)] float radius = 0.8f;
        [SerializeField] Color color = new(0.62f, 0.38f, 0.92f, 0.38f);        // 보라 (자하 紫霞)
        [SerializeField] Color hitFlashColor = new(1f, 1f, 1f, 0.75f);
        [SerializeField] Color healTextColor = new(0.45f, 0.95f, 0.45f, 1f);
        [SerializeField] int sortingOrder = 14;
        [SerializeField, Min(0.05f)] float ringSeconds = 0.35f;
        [SerializeField, Min(1f)] float ringMaxScale = 2f;
        [SerializeField, Min(0f)] float breatheAmount = 0.06f;
        [SerializeField, Min(0f)] float breatheSpeed = 1.5f;
        [SerializeField, Min(0.01f)] float hitFlashSeconds = 0.12f;
        [SerializeField, Min(0f)] float blinkSeconds = 1f;
        [SerializeField, Min(1f)] float blinkFrequency = 8f;
        [SerializeField, Min(0.1f)] float healTextSeconds = 0.9f;

        SpriteRenderer fill;
        SpriteRenderer ring;
        TextMesh healText;
        PlayerHealth health;

        float duration;
        float elapsed;
        float ringElapsed = float.PositiveInfinity;
        float hitElapsed = float.PositiveInfinity;
        float healElapsed = float.PositiveInfinity;

        public bool IsVisible => fill != null && fill.enabled;
        public bool IsFlashing => hitElapsed < hitFlashSeconds;
        public bool IsShowingHeal => healText != null && healText.gameObject.activeSelf;
        public string HealLabel => healText != null ? healText.text : "";
        public Sprite FillSprite => fill != null ? fill.sprite : null;

        void Awake()
        {
            bool hasFrames = fillFrames != null && fillFrames.Length > 0;
            fill = CreateRenderer("Fill", hasFrames ? fillFrames[0] : fillSprite != null ? fillSprite : ProceduralCircle.Disk, sortingOrder);
            if (fillMaterial != null)
                fill.sharedMaterial = fillMaterial;
            ring = CreateRenderer("Ring", ringSprite != null ? ringSprite : ProceduralCircle.Ring, sortingOrder);
            healText = CreateHealText();
            fill.enabled = false;
            ring.enabled = false;
        }

        public void Bind(PlayerHealth target)
        {
            if (health != null)
                health.Damaged -= OnDamaged;
            health = target;
            if (health != null && isActiveAndEnabled)
                health.Damaged += OnDamaged;
        }

        void OnEnable()
        {
            if (health != null)
                health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        void OnDamaged(float amount)
        {
            if (IsVisible)
                hitElapsed = 0f; // 줄여서 맞았다
        }

        public void Begin(float seconds)
        {
            duration = seconds;
            elapsed = 0f;
            ringElapsed = 0f;
            hitElapsed = float.PositiveInfinity;
            fill.enabled = true;
            ring.enabled = true;
            Apply();
        }

        public void End(float healed)
        {
            fill.enabled = false;
            ring.enabled = false;
            if (healed > 0f)
            {
                healElapsed = 0f;
                healText.text = $"+{Mathf.CeilToInt(healed)}";
                healText.gameObject.SetActive(true);
            }
            Apply();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            elapsed += dt;
            ringElapsed += dt;
            hitElapsed += dt;
            healElapsed += dt;
            Apply();
        }

        void Apply()
        {
            float diameter = radius * 2f;
            if (fill.enabled)
            {
                float breathe = 1f + Mathf.Sin(elapsed * breatheSpeed * Mathf.PI * 2f) * breatheAmount;
                fill.transform.localScale = Vector3.one * (diameter * breathe);
                if (fillFrames != null && fillFrames.Length > 0)
                    fill.sprite = fillFrames[FrameAnimation.FrameIndex((int)(elapsed * framesPerSecond), fillFrames.Length, true, loopFromFrame)];

                var c = IsFlashing ? Color.Lerp(color, hitFlashColor, 1f - hitElapsed / hitFlashSeconds) : color;
                // 마지막 1초 깜빡임: 언제 꺼지는지 미리 알린다
                bool blinkOff = duration - elapsed <= blinkSeconds && Mathf.Repeat(elapsed * blinkFrequency, 1f) < 0.5f;
                if (blinkOff)
                    c.a *= 0.35f;
                fill.color = c;
            }

            if (ring.enabled)
            {
                float t = ringElapsed / ringSeconds;
                if (t >= 1f)
                    ring.enabled = false;
                else
                {
                    ring.transform.localScale = Vector3.one * (diameter * Mathf.Lerp(1f, ringMaxScale, t));
                    var c = color;
                    c.a = Mathf.Lerp(0.9f, 0f, t);
                    ring.color = c;
                }
            }

            if (healText.gameObject.activeSelf)
            {
                float t = healElapsed / healTextSeconds;
                if (t >= 1f)
                    healText.gameObject.SetActive(false);
                else
                {
                    healText.transform.localPosition = new Vector3(0f, radius + t * 0.6f, 0f);
                    var c = healTextColor;
                    c.a = 1f - t * t;
                    healText.color = c;
                }
            }
        }

        SpriteRenderer CreateRenderer(string childName, Sprite sprite, int order)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        TextMesh CreateHealText()
        {
            var go = new GameObject("HealText");
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.characterSize = 0.06f;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = text.font.material;
            meshRenderer.sortingOrder = sortingOrder + 2;
            go.SetActive(false);
            return text;
        }
    }
}
