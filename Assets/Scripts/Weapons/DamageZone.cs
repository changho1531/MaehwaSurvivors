using System;
using System.Collections.Generic;
using Game.Enemies;
using Game.Player;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 지속 데미지 범위 공통 컴포넌트 (설계서 7-E 장판, 7-F 만천화우, 8-B 저주 장판). 생성 위치에 고정되어 지속 시간 동안
    /// 원 안의 대상에게 시간에 비례한 데미지를 주고 풀로 돌아간다. 대상은 적(플레이어 스킬) 또는 플레이어(적의 장판) —
    /// 지속 · 경고 · 끝 처리 흐름은 같고 "누구를 때리나"만 다르므로 컴포넌트 하나를 대상 설정만 바꿔 쓴다.
    /// 생성 때 받은 수치로 끝까지 가므로 지속 중 레벨업의 영향을 받지 않는다. 장판끼리는 각자 따로 데미지를 준다(겹치면 합산).
    /// </summary>
    public class DamageZone : MonoBehaviour
    {
        public enum Target { Enemies, Player }

        [SerializeField] Transform art;
        [Tooltip("아트 스케일 1일 때 그림 속 장판 반경(월드 단위). 판정 반경에 맞춰 스케일한다")]
        [SerializeField, Min(0.01f)] float artRadiusAtScale1 = 2.45f;
        [Tooltip("끝 폭발 연출. 범위가 풀로 돌아간 뒤에도 재생되도록 범위 밖에 따로 둔다")]
        [SerializeField] BurstEffect burstPrefab;

        [Header("경고 표시 (선택, 8-B — 그림이 없으면 코드로 만든 원/링)")]
        [Tooltip("장판 안쪽 채움. 경고 중에는 숨긴다")]
        [SerializeField] SpriteRenderer fillRenderer;
        [Tooltip("테두리 선. 경고 중에는 깜빡이고, 장판이 깔리면 그대로 — \"보이는 범위 = 맞는 범위\"")]
        [SerializeField] SpriteRenderer ringRenderer;
        [SerializeField, Min(0.1f)] float warningBlinksPerSecond = 4f;

        readonly List<Enemy> targets = new();
        BurstEffect burst;
        float radius;
        float remaining;
        float warningRemaining;
        float damagePerSecond;
        float explosionDamage;
        Action<DamageZone> release;

        Target target;
        PlayerHealth player;
        PlayerCurse curse;
        float curseSlow;
        float curseSeconds;
        float ringAlpha = 1f;

        /// <summary>(폭발 여부, 폭발에 맞은 적 수)</summary>
        public event Action<bool, int> Ended;

        public Target TargetKind => target;
        public bool IsWarning => warningRemaining > 0f;
        public float Radius => radius;

        void Awake()
        {
            if (fillRenderer != null && fillRenderer.sprite == null)
                fillRenderer.sprite = ProceduralCircle.Disk;
            if (ringRenderer != null)
            {
                if (ringRenderer.sprite == null)
                    ringRenderer.sprite = ProceduralCircle.Ring;
                ringAlpha = ringRenderer.color.a;
            }
        }

        /// <param name="endExplosionDamage">0보다 크면 끝나는 순간 원 안 적 전부에 1회 데미지</param>
        public void Begin(float zoneRadius, float seconds, float dps, Action<DamageZone> releaseToPool, float endExplosionDamage = 0f)
        {
            target = Target.Enemies;
            player = null;
            curse = null;
            warningRemaining = 0f;
            Setup(zoneRadius, seconds, dps, releaseToPool, endExplosionDamage);
        }

        /// <summary>
        /// 플레이어를 대상으로 깐다 (저주술사, 8-B). 경고 동안은 판정 없음, 이후 원 안에 있으면 초당 데미지 + 저주(curseSlow 감속).
        /// 데미지는 PlayerHealth 진입점을 지나므로 자하호신강 감소가 그대로 적용된다.
        /// </summary>
        public void BeginOnPlayer(PlayerHealth targetPlayer, float zoneRadius, float warningSeconds, float seconds, float dps,
            float slowRatio, float slowSeconds, Action<DamageZone> releaseToPool)
        {
            target = Target.Player;
            player = targetPlayer;
            curse = targetPlayer != null ? targetPlayer.GetComponent<PlayerCurse>() : null;
            curseSlow = slowRatio;
            curseSeconds = slowSeconds;
            warningRemaining = warningSeconds;
            Setup(zoneRadius, seconds, dps, releaseToPool, 0f);
        }

        void Setup(float zoneRadius, float seconds, float dps, Action<DamageZone> releaseToPool, float endExplosionDamage)
        {
            radius = zoneRadius;
            remaining = seconds;
            damagePerSecond = dps;
            explosionDamage = endExplosionDamage;
            release = releaseToPool;
            if (art != null)
                art.localScale = Vector3.one * (radius / artRadiusAtScale1);
            UpdateVisual(0f);
        }

        void Update()
        {
            float frame = Time.deltaTime;
            if (warningRemaining > 0f)
            {
                // 경고 중: 판정 없음. 남은 프레임 시간은 이번 프레임에 넘기지 않는다 (경고 1초 + 지속 3초가 정확히)
                warningRemaining -= frame;
                UpdateVisual(frame);
                return;
            }

            float dt = Mathf.Min(frame, remaining); // 마지막 프레임이 길어도 총 데미지가 지속 시간 × 초당 데미지를 넘지 않게
            DamageTargets(damagePerSecond * dt);
            UpdateVisual(frame);

            remaining -= dt;
            if (remaining <= 0f)
                Finish();
        }

        /// <summary>이번 프레임 원 안의 대상에게 데미지. 대상(적 / 플레이어)이 늘어도 지속 · 폭발 흐름은 그대로 두고 여기만 바뀐다.</summary>
        void DamageTargets(float amount)
        {
            if (target == Target.Player)
            {
                if (player == null || !IsInside(player.transform.position))
                    return;
                if (amount > 0f)
                    player.TakeDamage(amount);
                // 안에 있는 동안 매 프레임 남은 시간을 되돌린다 → 벗어난 순간부터 저주 시간이 줄어든다
                if (curse != null && Time.deltaTime > 0f)
                    curse.Apply(curseSlow, curseSeconds);
                return;
            }

            EnemyRegistry.CollectInRadius(transform.position, radius, targets);
            foreach (var enemy in targets)
                enemy.TakeDamage(amount);
        }

        public bool IsInside(Vector2 point) => (point - (Vector2)transform.position).sqrMagnitude <= radius * radius;

        float blinkTime;

        void UpdateVisual(float dt)
        {
            if (fillRenderer != null)
                fillRenderer.enabled = !IsWarning;
            if (ringRenderer == null)
                return;
            var c = ringRenderer.color;
            if (IsWarning)
            {
                blinkTime += dt;
                c.a = ringAlpha * (0.35f + 0.65f * Mathf.Abs(Mathf.Cos(blinkTime * warningBlinksPerSecond * Mathf.PI)));
            }
            else
            {
                blinkTime = 0f;
                c.a = ringAlpha;
            }
            ringRenderer.color = c;
        }

        void Finish()
        {
            bool explodes = explosionDamage > 0f;
            int hits = 0;
            if (explodes)
            {
                EnemyRegistry.CollectInRadius(transform.position, radius, targets);
                hits = targets.Count;
                foreach (var enemy in targets)
                    enemy.TakeDamage(explosionDamage);
                if (burstPrefab != null)
                {
                    if (burst == null)
                        burst = Instantiate(burstPrefab);
                    burst.Play(transform.position, radius);
                }
            }
            Ended?.Invoke(explodes, hits);
            release?.Invoke(this);
        }
    }
}
