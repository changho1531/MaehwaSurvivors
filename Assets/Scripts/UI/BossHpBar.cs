using DG.Tweening;
using Game.Enemies;
using Game.Enemies.Boss;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 보스 HP 바 (설계서 8-C 연출): 상단 중앙, 플레이어 HP · XP 바 아래. 이름 + 50% 표시선, 2페이즈에서 더 짙은 붉은색.
    /// BossSpawner.BossSpawned로 나타나고, 그 뒤로는 보스의 Damaged · PhaseChanged만 듣는다 (Observer).
    /// 부품은 코드로 만든다 (한 번만 쓰는 단순한 막대라 씬에 손으로 두지 않음).
    /// </summary>
    public class BossHpBar : MonoBehaviour
    {
        [SerializeField] BossSpawner spawner;
        [Header("※ 예시값")]
        [SerializeField] Vector2 size = new(720f, 22f);
        [SerializeField] Color phase1Color = new(0.75f, 0.12f, 0.12f, 1f);
        [SerializeField] Color phase2Color = new(0.45f, 0.02f, 0.05f, 1f);
        [Tooltip("비우면 기본 글꼴")]
        [SerializeField] Font font;

        GameObject root;
        Image fill;
        Text nameText;
        Enemy boss;
        BossBehaviour behaviour;

        public bool IsShown => root != null && root.activeSelf;
        public float FillAmount => fill.fillAmount;
        public string NameLabel => nameText.text;
        public Color FillColor => fill.color;

        void Awake()
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            root.SetActive(false);
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
            Unbind();
        }

        void OnBossSpawned(Enemy spawned)
        {
            Unbind();
            boss = spawned;
            behaviour = spawned.GetComponent<BossBehaviour>();
            boss.Damaged += OnDamaged;
            if (behaviour != null)
                behaviour.PhaseChanged += OnPhaseChanged;
            nameText.text = behaviour != null && behaviour.Data != null ? behaviour.Data.displayName : spawned.name;
            fill.color = phase1Color;
            Refresh();
            root.SetActive(true);
            // 가로로 펼쳐지며 등장 (게임 시간)
            root.transform.DOKill();
            root.transform.localScale = new Vector3(0f, 1f, 1f);
            root.transform.DOScaleX(1f, 0.4f).SetEase(Ease.OutCubic).SetLink(root, LinkBehaviour.KillOnDestroy);
        }

        void Unbind()
        {
            if (boss != null)
                boss.Damaged -= OnDamaged;
            if (behaviour != null)
                behaviour.PhaseChanged -= OnPhaseChanged;
            boss = null;
            behaviour = null;
        }

        void OnDamaged(float amount) => Refresh();

        void OnPhaseChanged(int phase) => fill.color = phase >= 2 ? phase2Color : phase1Color;

        void Refresh()
        {
            if (boss == null || boss.Data == null)
                return;
            fill.fillAmount = Mathf.Clamp01(boss.CurrentHp / boss.Data.maxHp);
        }

        void Build()
        {
            root = new GameObject("BossHp", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -150f); // 상단 HP · XP 바 · 웨이브 문구 아래
            rect.sizeDelta = size;

            var bg = Child<Image>(rect, "Background");
            bg.sprite = UiSprites.Solid;
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            Stretch(bg.rectTransform, -3f);

            fill = Child<Image>(rect, "Fill");
            fill.sprite = UiSprites.Solid;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            Stretch(fill.rectTransform, 0f);

            // 50% 표시선 (페이즈 전환 지점)
            var marker = Child<Image>(rect, "Half");
            marker.sprite = UiSprites.Solid;
            marker.color = new Color(1f, 0.9f, 0.7f, 0.9f);
            var m = marker.rectTransform;
            m.anchorMin = new Vector2(0.5f, 0f);
            m.anchorMax = new Vector2(0.5f, 1f);
            m.sizeDelta = new Vector2(3f, 6f);

            nameText = Child<Text>(rect, "Name");
            nameText.font = font;
            nameText.fontSize = 26;
            nameText.alignment = TextAnchor.LowerCenter;
            nameText.color = Color.white;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var n = nameText.rectTransform;
            n.anchorMin = new Vector2(0f, 1f);
            n.anchorMax = new Vector2(1f, 1f);
            n.pivot = new Vector2(0.5f, 0f);
            n.anchoredPosition = new Vector2(0f, 4f);
            n.sizeDelta = new Vector2(0f, 34f);
            nameText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        }

        static T Child<T>(Transform parent, string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
            go.transform.SetParent(parent, false);
            var g = go.GetComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
