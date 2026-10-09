using System;
using System.Collections.Generic;
using Game.Enemies;
using Game.Player;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 부채꼴 연속 베기 (화산십이검, 설계서 7절 규칙). 적 유무와 관계없이 쿨타임마다 발동해
    /// 콤보 지속 시간 동안 타격을 균등 간격으로 실행한다. 매 타격 시점의 플레이어 위치 + 현재 바라보는 방향으로
    /// 새로 판정하므로, 콤보 중 움직이거나 방향을 바꾸면 부채꼴도 따라간다. 범위 안의 모든 적이 매번 맞는다.
    /// 콤보 시작 때 데미지를 잡아 두어, 콤보 중 레벨업해도 진행 중인 콤보는 이전 수치로 끝난다.
    /// </summary>
    public class SweepWeapon : Weapon<SweepWeaponData>
    {
        readonly List<Enemy> targets = new();
        PlayerFacing facing;
        SweepEffect effect;

        float elapsed;
        int hitsDone;
        float comboDamage;

        /// <summary>(타격 번호 0~, 이번 타격에 맞은 적 수)</summary>
        public event Action<int, int> Struck;

        public SweepEffect Effect => effect;

        protected override void OnInit()
        {
            facing = GetComponent<PlayerFacing>();
            if (Data.effectPrefab != null)
            {
                // 이펙트는 플레이어 자식이라 위치는 자동으로 따라가고, 회전만 매 프레임 바라보는 방향에 맞춘다.
                effect = Instantiate(Data.effectPrefab, transform);
                effect.Build(Data.radius, Data.angle);
                effect.gameObject.SetActive(false);
            }
        }

        Vector2 FacingDirection => facing != null ? facing.Direction : PlayerFacing.Initial;

        protected override bool TryStartAttack()
        {
            elapsed = 0f;
            hitsDone = 0;
            comboDamage = Data.GetDamage(Level);
            if (effect != null)
                effect.gameObject.SetActive(true);
            return true; // 적이 없어도 발동
        }

        protected override bool TickAttack(float dt)
        {
            elapsed += dt;
            float interval = Data.comboDuration / Data.hitCount;
            while (hitsDone < Data.hitCount && elapsed >= hitsDone * interval)
            {
                Strike(hitsDone);
                hitsDone++;
            }

            if (effect != null)
                effect.Face(FacingDirection);

            if (hitsDone < Data.hitCount || elapsed < Data.comboDuration)
                return false;

            if (effect != null)
                effect.gameObject.SetActive(false);
            return true; // 콤보가 끝난 뒤부터 쿨타임 시작 (베이스)
        }

        void Strike(int index)
        {
            Vector2 origin = transform.position;
            var direction = FacingDirection;

            // 처치되면 레지스트리에서 빠지므로 복사본을 돌며 판정한다.
            targets.Clear();
            targets.AddRange(EnemyRegistry.Active);

            int hitEnemies = 0;
            foreach (var enemy in targets)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;
                if (!IsInFan(origin, direction, Data.radius, Data.angle, enemy.transform.position))
                    continue;
                enemy.TakeDamage(comboDamage);
                hitEnemies++;
            }

            if (effect != null)
                effect.Flash();
            Struck?.Invoke(index, hitEnemies);
        }

        /// <summary>point가 origin에서 direction을 중심으로 한 부채꼴(반경, 전체 각도) 안에 있는가.</summary>
        public static bool IsInFan(Vector2 origin, Vector2 direction, float radius, float angle, Vector2 point)
        {
            var offset = point - origin;
            if (offset.sqrMagnitude > radius * radius)
                return false;
            if (offset.sqrMagnitude < 0.0001f)
                return true; // 플레이어와 겹친 적은 맞는다
            return Vector2.Angle(direction, offset) <= angle * 0.5f;
        }
    }
}
