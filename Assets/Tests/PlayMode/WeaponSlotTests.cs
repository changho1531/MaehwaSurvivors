using System.Collections;
using Game.Progression;
using Game.UI;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [HUD 무기 슬롯 7-J절] 시작 1칸(매화검기), 얻은 순서로 채움, 레벨 1 → 2 → MAX, 쿨타임 덮개 = 1 − 진행률,
    /// 자하호신강 지속 중 게이지 감소 → 꺼지면 덮개, 매화보 덮개 없음 · 가속 중 강조, 아이콘 없으면 첫 글자,
    /// 일시정지 중 표시 정지, 레벨업 카드 아이콘. 주기를 짧게 하려고 무기 데이터는 복제해 수치만 바꾼다.
    /// </summary>
    public class WeaponSlotTests
    {
        readonly InGameScene scene = new();
        WeaponSlotBar bar;
        ShieldWeaponData shieldData;
        MovementPassiveData stepData;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            scene.StopSpawningAndClear();
            bar = Object.FindFirstObjectByType<WeaponSlotBar>();
            Assert.IsNotNull(bar, "HUD에 WeaponSlotBar가 없다");

            foreach (var weapon in scene.Inventory.Catalog.weapons)
            {
                if (weapon is ShieldWeaponData s)
                    shieldData = Object.Instantiate(s);
                if (weapon is MovementPassiveData m)
                    stepData = Object.Instantiate(m);
            }
            shieldData.durationPerLevel = new[] { 0.5f, 0.5f, 0.5f };
            shieldData.cooldownPerLevel = new[] { 2f, 2f, 2f };
            stepData.boostDelaySeconds = 0.3f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            Object.Destroy(shieldData);
            Object.Destroy(stepData);
            yield return null;
        }

        WeaponSlotView ViewOf(Weapon weapon)
        {
            foreach (var view in bar.Views)
                if (view.Weapon == weapon)
                    return view;
            Assert.Fail($"{weapon.Data.displayName} 칸이 없다");
            return null;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
        }

        [Test]
        public void S1_Start_OnlyFirstSlotIsStartingWeapon()
        {
            Assert.AreEqual(6, bar.Views.Count);
            Assert.AreSame(scene.Inventory.Owned[0], bar.Views[0].Weapon, "첫 칸 = 시작 무기");
            Assert.AreEqual("매화검기", bar.Views[0].Weapon.Data.displayName);
            for (int i = 1; i < 6; i++)
                Assert.IsTrue(bar.Views[i].IsEmpty, $"{i}번 칸은 비어 있어야 한다");
            Assert.IsTrue(bar.Views[0].HasIcon, "매화검기 아이콘 연결");
        }

        [Test]
        public void S2_FillsInAcquireOrder_LevelLabel_1_2_Max()
        {
            Assert.IsTrue(scene.Inventory.Acquire(stepData));
            Assert.IsTrue(scene.Inventory.Acquire(shieldData));
            Assert.AreSame(stepData, bar.Views[1].Weapon.Data, "두 번째로 얻은 것이 2번 칸");
            Assert.AreSame(shieldData, bar.Views[2].Weapon.Data);
            Assert.IsTrue(bar.Views[1].IsFlashing, "얻으면 반짝임");

            var view = bar.Views[2];
            Assert.AreEqual("1", view.LevelLabel);
            scene.Inventory.Upgrade(shieldData);
            Assert.AreEqual("2", view.LevelLabel);
            scene.Inventory.Upgrade(shieldData);
            Assert.AreEqual("MAX", view.LevelLabel);
        }

        [UnityTest]
        public IEnumerator S3_Shield_GaugeWhileActive_ThenCoverMatchesCooldown()
        {
            Assert.IsTrue(scene.Inventory.Acquire(shieldData));
            var shield = (ShieldWeapon)scene.Inventory.Find(shieldData);
            var view = ViewOf(shield);
            yield return WaitUntil(() => shield.IsShieldActive, 2f);
            yield return null;
            yield return null;

            Assert.IsTrue(view.IsGaugeShown, "막이 켜진 동안 지속 중 표시");
            Assert.AreEqual(0f, view.CoverAmount, 0.001f, "지속 중에는 덮개 없음");
            float first = view.GaugeAmount;
            yield return new WaitForSeconds(0.2f);
            Assert.Less(view.GaugeAmount, first, "게이지가 줄어든다");

            yield return WaitUntil(() => !shield.IsShieldActive, 2f);
            float endedAt = Time.time;
            yield return null;
            Assert.IsFalse(view.IsGaugeShown, "꺼지면 게이지 숨김");
            Assert.Greater(view.CoverAmount, 0.9f, "꺼지면 덮개가 다시 시작");

            // 쿨타임 2초의 절반 → 덮개 절반
            yield return WaitUntil(() => Time.time - endedAt >= 1f, 3f);
            Assert.AreEqual(0.5f, view.CoverAmount, 0.06f);
            Assert.AreEqual(shield.GetSlotState().CooldownCover, view.CoverAmount, 0.03f, "덮개 = 1 − 쿨타임 진행률");
        }

        [UnityTest]
        public IEnumerator S4_Step_NoCover_HighlightWhileBoosted()
        {
            Assert.IsTrue(scene.Inventory.Acquire(stepData));
            scene.Inventory.Upgrade(stepData);
            scene.Inventory.Upgrade(stepData);
            var step = (MovementPassive)scene.Inventory.Find(stepData);
            var view = ViewOf(step);
            yield return null;
            Assert.AreEqual(0f, view.CoverAmount, 0.001f, "매화보는 덮개 없음");
            Assert.IsFalse(view.IsHighlighted);

            yield return WaitUntil(() => step.IsBoosted, 2f);
            yield return null;
            Assert.IsTrue(step.IsBoosted);
            Assert.IsTrue(view.IsHighlighted, "가속 중 분홍 강조");
            Assert.AreEqual(0f, view.CoverAmount, 0.001f);
        }

        [Test]
        public void S5_NoIcon_ShowsLetter()
        {
            shieldData.icon = null;
            stepData.icon = null;
            scene.Inventory.Acquire(shieldData);
            scene.Inventory.Acquire(stepData);
            var shieldView = ViewOf(scene.Inventory.Find(shieldData));
            var stepView = ViewOf(scene.Inventory.Find(stepData));
            Assert.IsFalse(shieldView.HasIcon);
            Assert.AreEqual("자", shieldView.LetterLabel);
            Assert.AreEqual("보", stepView.LetterLabel, "매화보는 매화검기와 겹치지 않게 \"보\"");
        }

        [UnityTest]
        public IEnumerator S6_Paused_DisplayFrozen()
        {
            Assert.IsTrue(scene.Inventory.Acquire(shieldData));
            var shield = (ShieldWeapon)scene.Inventory.Find(shieldData);
            var view = ViewOf(shield);
            yield return WaitUntil(() => !shield.IsAttacking && view.CoverAmount > 0f, 3f);
            yield return null;

            Time.timeScale = 0f;
            yield return null;
            float cover = view.CoverAmount;
            for (int i = 0; i < 10; i++)
                yield return null;
            Assert.AreEqual(cover, view.CoverAmount, 0.0001f, "일시정지 중 덮개가 움직이지 않는다");
            Assert.Greater(cover, 0f);
        }

        [UnityTest]
        public IEnumerator S7_LevelUpCard_ShowsIcon()
        {
            var cardView = Object.FindFirstObjectByType<LevelUpCardView>();
            Assert.IsNotNull(cardView);
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;
            yield return null;
            Assert.IsTrue(cardView.Panel.activeSelf, "카드가 떠야 한다");
            Assert.IsTrue(cardView.Slots[0].IsIconShown, "카드에도 같은 아이콘");
        }
    }
}
