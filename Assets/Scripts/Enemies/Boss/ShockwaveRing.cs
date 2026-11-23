using System;
using Game.Player;
using Game.Visuals;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 지면 강타의 충격파 고리 (8-C). 0 → 최대 반경으로 퍼지며, 고리(두께)가 플레이어를 지나가는 순간 1회만 판정한다.
    /// 이미 지나간 안쪽에 서 있으면 맞지 않는다. 한 프레임에 고리가 플레이어를 건너뛰어도 놓치지 않도록
    /// "이전 반경 ~ 지금 반경" 구간 전체로 판정한다. 게임 시간 기준.
    /// </summary>
    public class ShockwaveRing : MonoBehaviour
    {
        [SerializeField] Color color = new(1f, 0.22f, 0.18f, 0.9f);
        [SerializeField] int sortingOrder = 3;

        SpriteRenderer ring;
        PlayerHealth player;
        Action<ShockwaveRing> release;
        float maxRadius;
        float duration;
        float thickness;
        float damage;
        float elapsed;
        float previousRadius;

        public bool HasHit { get; private set; }
        public float CurrentRadius { get; private set; }
        public bool IsLive { get; private set; }

        void Awake()
        {
            ring = GetComponent<SpriteRenderer>();
            if (ring == null)
                ring = gameObject.AddComponent<SpriteRenderer>();
            if (ring.sprite == null)
                ring.sprite = ProceduralCircle.Ring;
            ring.color = color;
            ring.sortingOrder = sortingOrder;
        }

        public void Begin(float radius, float seconds, float ringThickness, float hitDamage, PlayerHealth target, Action<ShockwaveRing> releaseToPool)
        {
            maxRadius = radius;
            duration = Mathf.Max(0.01f, seconds);
            thickness = ringThickness;
            damage = hitDamage;
            player = target;
            release = releaseToPool;
            elapsed = 0f;
            previousRadius = 0f;
            CurrentRadius = 0f;
            HasHit = false;
            IsLive = true;
            SetVisualRadius(0f);
        }

        void Update()
        {
            if (!IsLive)
                return;
            elapsed += Time.deltaTime;
            CurrentRadius = Mathf.Min(1f, elapsed / duration) * maxRadius;

            if (!HasHit && player != null)
            {
                float d = ((Vector2)(player.transform.position - transform.position)).magnitude;
                float half = thickness * 0.5f;
                if (d >= previousRadius - half && d <= CurrentRadius + half)
                {
                    HasHit = true;
                    player.TakeDamage(damage);
                }
            }
            previousRadius = CurrentRadius;
            SetVisualRadius(CurrentRadius);

            if (elapsed >= duration)
            {
                IsLive = false;
                if (release != null)
                    release(this);
                else
                    gameObject.SetActive(false);
            }
        }

        // 원/링 그림은 지름 1 (반경 0.5) — 판정 반경에 맞춰 키운다
        void SetVisualRadius(float radius) => transform.localScale = Vector3.one * Mathf.Max(0.01f, radius * 2f);
    }
}
