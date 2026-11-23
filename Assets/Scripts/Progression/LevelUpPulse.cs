using DG.Tweening;
using Game.Visuals;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 레벨업 순간 플레이어 주변으로 금색 고리가 퍼진다 (연출만). PlayerLevel.LeveledUp만 구독한다 (Observer).
    /// 레벨업 직후 카드가 뜨며 게임 시간이 멈추므로 실시간 기준 — 카드 뒤에서도 끝까지 퍼진다.
    /// 고리는 처음 한 번 만들어 두고 재사용한다 (레벨업마다 생성/파괴하지 않음).
    /// </summary>
    public class LevelUpPulse : MonoBehaviour
    {
        [SerializeField] PlayerLevel level;
        [Header("※ 예시값")]
        [SerializeField] Color color = new(1f, 0.85f, 0.45f, 0.9f);
        [SerializeField, Min(1)] int rings = 2;
        [SerializeField, Min(0f)] float ringInterval = 0.12f;
        [Tooltip("고리 최대 지름(칸)")]
        [SerializeField, Min(0.1f)] float endDiameter = 4f;
        [SerializeField, Min(0.05f)] float seconds = 0.55f;
        [Tooltip("고리 중심 높이(칸). 플레이어 피벗이 발이라 몸 가운데로 올린다")]
        [SerializeField] float centerHeight = 0.5f;
        [SerializeField] int sortingOrder = 12;

        SpriteRenderer[] renderers;

        public bool IsPlaying { get; private set; }

        void Awake()
        {
            if (level == null)
                level = GetComponent<PlayerLevel>();
            renderers = new SpriteRenderer[rings];
            for (int i = 0; i < rings; i++)
            {
                var go = new GameObject($"LevelUpRing{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, centerHeight, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralCircle.Ring; // 지름 1칸 링 (자하호신강 · 저주 장판과 같은 코드 그림)
                sr.sortingOrder = sortingOrder;
                sr.enabled = false;
                renderers[i] = sr;
            }
        }

        void OnEnable()
        {
            if (level != null)
                level.LeveledUp += OnLeveledUp;
        }

        void OnDisable()
        {
            if (level != null)
                level.LeveledUp -= OnLeveledUp;
        }

        void OnLeveledUp(int gained) => Play();

        public void Play()
        {
            IsPlaying = true;
            for (int i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                var t = sr.transform;
                t.DOKill();
                sr.DOKill();
                t.localScale = Vector3.one * 0.3f;
                sr.color = color;
                sr.enabled = true;
                bool last = i == renderers.Length - 1;
                float delay = i * ringInterval;
                t.DOScale(endDiameter, seconds).SetEase(Ease.OutCubic).SetDelay(delay).SetUpdate(true).SetLink(gameObject);
                sr.DOFade(0f, seconds).SetEase(Ease.InQuad).SetDelay(delay).SetUpdate(true).SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        sr.enabled = false;
                        if (last)
                            IsPlaying = false;
                    });
            }
        }
    }
}
