using System;
using Game.Player;
using Game.Pooling;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 저주술사 (설계서 8-B절). 유지 거리보다 멀면 다가오고 안이면 멈춘다(물러나지 않음 — 다가가면 잡힌다).
    /// 주기마다 그 순간의 플레이어 위치에 저주 장판(경고 → 장판)을 깔고, 주기마다 주변에 도병을 부른다 (둘 다 화면 안에 있을 때만).
    /// 장판은 공통 DamageZone을 "플레이어 대상"으로 쓰고, 소환은 자기를 꺼낸 스포너(IEnemySource)에 맡긴다 —
    /// 그래서 저주술사가 죽어도 이미 깔린 장판과 불려 나온 도병은 각자의 풀에서 끝까지 진행된다 (규칙 8).
    /// </summary>
    public class SorcererBehaviour : EnemyBehaviour
    {
        [Tooltip("떨림 연출로 흔들 그림(자식). 몸통(물리)은 흔들지 않는다")]
        [SerializeField] Transform art;
        [Tooltip("(선택) 장판을 쏠 때 몸 위에서 1회 재생하는 시전 이펙트 (자식)")]
        [SerializeField] BurstEffect castEffect;

        SorcererEnemyData data;
        Transform cachedTarget;
        PlayerHealth targetHealth;
        float castTimer;
        float summonTimer;
        float shakeRemaining;
        float shakeTime;

        /// <summary>(장판 위치) — 테스트 · 연출용</summary>
        public event Action<Vector2> ZoneCast;
        /// <summary>(실제로 불러낸 수)</summary>
        public event Action<int> Summoned;

        public SorcererEnemyData Data => data;

        protected override void Awake()
        {
            base.Awake();
            data = Owner.Data as SorcererEnemyData;
        }

        public override void ResetState()
        {
            castTimer = 0f;
            summonTimer = 0f;
            shakeRemaining = 0f;
            SetShake(0f);
            if (castEffect != null)
                castEffect.gameObject.SetActive(false); // 풀에서 다시 꺼낼 때 지난 시전이 남아 있지 않게
        }

        public override void Tick(Transform target, float dt)
        {
            if (data == null)
                return;
            CacheTarget(target);

            // 이동: 멀면 다가오고, 유지 거리 안이면 멈춘다 (뒤로 물러나지 않음, 규칙 1)
            var toTarget = (Vector2)target.position - Body.position;
            Body.linearVelocity = toTarget.sqrMagnitude > data.keepDistance * data.keepDistance
                ? toTarget.normalized * Owner.MoveSpeed
                : Vector2.zero;

            // 장판: 주기가 찼어도 화면 밖이면 쏘지 않고 기다린다 (규칙 3) → 화면에 들어오는 순간 쏜다
            castTimer += dt;
            if (castTimer >= data.castInterval && ScreenCheck.IsVisible(Body.position))
            {
                castTimer = 0f;
                CastZone(target.position);
            }

            // 소환도 화면 밖이면 기다렸다가 화면에 들어오는 순간 (보이지 않는 곳에서 생기면 소환인지 알 수 없다)
            summonTimer += dt;
            if (summonTimer >= data.summonInterval && ScreenCheck.IsVisible(Body.position))
            {
                summonTimer = 0f;
                Summon();
            }

            UpdateShake(dt);
        }

        /// <summary>그 순간의 플레이어 위치에 고정 (플레이어가 움직여도 따라가지 않음, 규칙 2).</summary>
        public void CastZone(Vector2 position)
        {
            if (data.zonePrefab != null)
            {
                var pool = PrefabPools.For(data.zonePrefab, 4);
                var zone = pool.Get(position);
                zone.BeginOnPlayer(targetHealth, data.zoneRadius, data.zoneWarningSeconds, data.zoneSeconds,
                    data.zoneDamagePerSecond, data.curseSlowRatio, data.curseSeconds, pool.Release);
            }
            shakeRemaining = data.castShakeSeconds;
            shakeTime = 0f;
            if (castEffect != null)
                castEffect.Play(castEffect.transform.position); // 자식이라 몸을 따라다닌다
            ZoneCast?.Invoke(position);
        }

        /// <summary>주변에 도병. 동시 활성 상한에 닿으면 들어가는 만큼만 (규칙 6).</summary>
        public int Summon()
        {
            int count = 0;
            if (Owner.Source != null && data.summonPrefab != null)
                for (int i = 0; i < data.summonCount; i++)
                {
                    var summoned = Owner.Source.SpawnNear(data.summonPrefab, Body.position, data.summonRadius);
                    if (summoned == null)
                        continue;
                    count++;
                    PlaySummonSmoke(summoned.transform.position);
                }
            Summoned?.Invoke(count);
            return count;
        }

        void PlaySummonSmoke(Vector2 position)
        {
            if (data.summonEffectPrefab == null)
                return;
            var pool = PrefabPools.For(data.summonEffectPrefab, 2);
            pool.Get(position).Play(position, pool.Release);
        }

        void UpdateShake(float dt)
        {
            if (shakeRemaining <= 0f)
                return;
            shakeRemaining -= dt;
            shakeTime += dt;
            SetShake(shakeRemaining > 0f ? Mathf.Sin(shakeTime * data.shakeFrequency * Mathf.PI * 2f) * data.shakeAmplitude : 0f);
        }

        void CacheTarget(Transform target)
        {
            if (cachedTarget == target)
                return;
            cachedTarget = target;
            targetHealth = target.GetComponent<PlayerHealth>();
        }

        void SetShake(float x)
        {
            if (art != null)
                art.localPosition = new Vector3(x, art.localPosition.y, 0f);
        }
    }
}
