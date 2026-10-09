using Game.Player;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[무기 구조 7절] 레벨별 수치표 조회, 바라보는 방향 규칙.</summary>
    public class WeaponDataTests
    {
        [Test]
        public void LevelTable_IndexesByLevel_AndClampsToLastValue()
        {
            var table = new[] { 10f, 12f, 14f };
            Assert.AreEqual(10f, LevelTable.Get(table, 1));
            Assert.AreEqual(14f, LevelTable.Get(table, 3));
            Assert.AreEqual(14f, LevelTable.Get(table, 9), "표보다 높은 레벨은 마지막 값");
            Assert.AreEqual(10f, LevelTable.Get(table, 0), "잘못된 레벨은 Lv1 값");
            Assert.AreEqual(0f, LevelTable.Get(null, 1));
        }

        [Test]
        public void ProjectileWeaponData_GettersUseLevelTables()
        {
            var data = ScriptableObject.CreateInstance<ProjectileWeaponData>();
            try
            {
                data.damagePerLevel = new[] { 10f, 12f };
                data.cooldownPerLevel = new[] { 1f, 0.95f };
                data.projectileSpeedPerLevel = new[] { 12f, 13f };

                Assert.AreEqual(12f, data.GetDamage(2));
                Assert.AreEqual(0.95f, data.GetCooldown(2), 1e-5f);
                Assert.AreEqual(13f, data.GetProjectileSpeed(2));

                data.cooldownPerLevel = new[] { 0f };
                Assert.Greater(data.GetCooldown(1), 0f, "쿨타임 0으로 매 프레임 발사되는 사고 방지");
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Facing_FollowsLastNonZeroInput_IncludingDiagonal()
        {
            var facing = PlayerFacing.Initial;
            Assert.AreEqual(Vector2.up, facing, "시작 시 위쪽");

            facing = PlayerFacing.Resolve(facing, Vector2.zero);
            Assert.AreEqual(Vector2.up, facing, "입력이 없으면 유지");

            facing = PlayerFacing.Resolve(facing, new Vector2(1f, -1f).normalized);
            Assert.That(Vector2.Distance(facing, new Vector2(0.7071f, -0.7071f)), Is.LessThan(0.001f), "대각선 그대로");

            facing = PlayerFacing.Resolve(facing, Vector2.zero);
            Assert.That(Vector2.Distance(facing, new Vector2(0.7071f, -0.7071f)), Is.LessThan(0.001f), "멈춰도 마지막 방향 유지");
        }
    }
}
