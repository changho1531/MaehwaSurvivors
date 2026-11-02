using System;
using System.Collections.Generic;
using Game.Enemies;
using Game.Pooling;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 암기 비 (만천화우, 설계서 7-F절). 쿨타임마다 화면 안의 무작위 적 자리에 DamageZone을 놓고 공격을 끝낸다(즉발).
    /// 가장 가까운 적이 아니라 무작위라서 근접 스킬이 닿지 않는 화면 곳곳을 정리한다. 이후의 판정은 범위가 혼자 맡는다.
    /// </summary>
    public class RainWeapon : Weapon<RainWeaponData>
    {
        readonly List<Enemy> visible = new();
        ComponentPool<DamageZone> zonePool;

        /// <summary>(범위 중심, 적을 골랐는지)</summary>
        public event Action<Vector2, bool> Cast;

        public ComponentPool<DamageZone> ZonePool => zonePool;

        protected override void OnInit()
        {
            var root = new GameObject($"[Pool] {Data.zonePrefab.name}").transform;
            zonePool = new ComponentPool<DamageZone>(Data.zonePrefab, root, Data.zonePrewarmCount);
        }

        protected override bool TryStartAttack()
        {
            var view = ViewRect(Camera.main, Origin);

            visible.Clear();
            foreach (var enemy in EnemyRegistry.Active)
                if (enemy != null && enemy.IsAlive && view.Contains(enemy.transform.position))
                    visible.Add(enemy);

            bool picked = visible.Count > 0;
            Vector2 center = picked
                ? visible[UnityEngine.Random.Range(0, visible.Count)].transform.position
                : new Vector2(UnityEngine.Random.Range(view.xMin, view.xMax), UnityEngine.Random.Range(view.yMin, view.yMax)); // 헛방이어도 리듬은 일정

            float explosion = Level >= Data.maxLevel ? Data.explosionDamage : 0f;
            zonePool.Get(center).Begin(Data.GetRadius(Level), Data.GetDuration(Level), Data.GetDamage(Level), zonePool.Release, explosion);
            Cast?.Invoke(center, picked);
            return true;
        }

        /// <summary>직교 카메라의 시야 사각형. 카메라가 없으면 fallback 한 점.</summary>
        public static Rect ViewRect(Camera camera, Vector2 fallback)
        {
            if (camera == null)
                return new Rect(fallback, Vector2.zero);
            float height = camera.orthographicSize * 2f;
            var size = new Vector2(height * camera.aspect, height);
            return new Rect((Vector2)camera.transform.position - size * 0.5f, size);
        }
    }
}
