using Game.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>[7-J절] 슬롯 상태 계산과 아이콘이 없을 때의 글자.</summary>
    public class WeaponSlotStateTests
    {
        [Test]
        public void Cover_IsOneMinusProgress_AndZeroWhileActive()
        {
            Assert.AreEqual(0.5f, new WeaponSlotState(0.5f, false, 0f, false).CooldownCover, 0.0001f);
            Assert.AreEqual(0f, new WeaponSlotState(1f, false, 0f, false).CooldownCover, 0.0001f);
            Assert.AreEqual(0f, new WeaponSlotState(0.2f, true, 0.7f, false).CooldownCover, 0.0001f, "지속 중에는 덮개 대신 게이지");
        }

        [Test]
        public void SlotLetter_DefaultsToFirstChar_OverrideWins()
        {
            var data = ScriptableObject.CreateInstance<ShieldWeaponData>();
            data.displayName = "자하호신강";
            Assert.AreEqual("자", data.SlotLetter);
            data.slotLetter = "보";
            Assert.AreEqual("보", data.SlotLetter);
            Object.DestroyImmediate(data);
        }
    }
}
