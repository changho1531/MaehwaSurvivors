using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 저주 상태 (설계서 8-B절). 걸려 있는 동안 PlayerMovement의 감속 배율을 낮춘다.
    /// 중첩 없음: 다시 걸리면 남은 시간만 되돌린다 — 저주술사가 여럿이어도 감속은 하나라 움직일 수 없게 되는 일이 없다.
    /// 장판은 안에 있는 동안 매 프레임 Apply를 부르므로 "안에 있는 동안 + 벗어난 뒤 N초"가 따로 셀 필요 없이 만들어진다.
    /// 게임 시간 기준이라 일시정지 · 카드 선택 중에는 남은 시간도 멈춘다.
    /// </summary>
    public class PlayerCurse : MonoBehaviour
    {
        [Tooltip("발밑 저주 기운 색 (어두운 보라 — 자하호신강의 밝은 보라 막과 구분) ※")]
        [SerializeField] Color auraColor = new(0.32f, 0.12f, 0.42f, 0.85f);
        [Tooltip("파티클 재질 (Sprites/Default 계열). 비우면 연출 없음")]
        [SerializeField] Material auraMaterial;
        [Tooltip("(선택) 발밑에서 반복 재생하는 그림 (자식, 처음엔 꺼 둠). 있으면 파티클 대신 이것을 켠다")]
        [SerializeField] GameObject auraArt;

        PlayerMovement movement;
        PlayerHealth health;
        ParticleSystem aura;
        float remaining;

        /// <summary>(걸림 여부) — 연출, (이후) AI 대사</summary>
        public event Action<bool> Changed;

        public bool IsCursed { get; private set; }
        public float Remaining => IsCursed ? remaining : 0f;
        public float SlowRatio { get; private set; }

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            health = GetComponent<PlayerHealth>();
            if (auraArt == null && auraMaterial != null)
                aura = BuildAura();
        }

        void OnEnable()
        {
            if (health != null)
                health.Died += Clear;
        }

        void OnDisable()
        {
            if (health != null)
                health.Died -= Clear;
        }

        /// <summary>저주를 건다. 이미 걸려 있으면 남은 시간만 seconds로 되돌린다 (중첩 없음).</summary>
        public void Apply(float slowRatio, float seconds)
        {
            if (health != null && health.IsDead)
                return;
            remaining = seconds;
            if (IsCursed)
                return;

            IsCursed = true;
            SlowRatio = Mathf.Clamp01(slowRatio);
            if (movement != null)
                movement.SlowMultiplier = 1f - SlowRatio;
            if (aura != null)
                aura.Play();
            if (auraArt != null)
                auraArt.SetActive(true);
            Changed?.Invoke(true);
        }

        public void Clear()
        {
            if (!IsCursed)
                return;
            IsCursed = false;
            remaining = 0f;
            if (movement != null)
                movement.SlowMultiplier = 1f;
            if (aura != null)
                aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (auraArt != null)
                auraArt.SetActive(false);
            Changed?.Invoke(false);
        }

        void Update()
        {
            if (!IsCursed)
                return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
                Clear();
        }

        // 기본 파티클: 발밑 원 안에서 천천히 피어오르는 어두운 보라 기운 (8-B 연출)
        ParticleSystem BuildAura()
        {
            var go = new GameObject("CurseAura");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.7f;
            main.startSpeed = 0.35f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = auraColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f;

            var emission = ps.emission;
            emission.rateOverTime = 22f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;
            shape.rotation = new Vector3(0f, 0f, 0f);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = auraMaterial;
            renderer.material.mainTexture = Game.Weapons.ProceduralCircle.Disk.texture; // 둥근 입자 (매화보 꽃잎과 같은 원)
            renderer.sortingOrder = 4; // 저주 장판(2 · 3) 위, 적(5) · 캐릭터 아래
            return ps;
        }
    }
}
