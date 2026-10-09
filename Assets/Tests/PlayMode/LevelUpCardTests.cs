using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>[레벨업 카드 UI, MU-B-003] 레벨업 시 카드 표시, 강화/신규 표기, 클릭 → 무기 반영 → Play 복귀 (설계서 6절).</summary>
    public class LevelUpCardTests
    {
        readonly InGameScene scene = new();
        LevelUpCardView view;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return scene.Load();
            view = Object.FindFirstObjectByType<LevelUpCardView>(FindObjectsInactive.Include);
            Assert.IsNotNull(view, "InGame 씬에 LevelUpCardView가 없다");
            scene.StopSpawningAndClear();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        GameState State => GameManager.Instance.CurrentState;

        List<LevelUpCardSlot> VisibleSlots()
        {
            var result = new List<LevelUpCardSlot>();
            foreach (var slot in view.Slots)
                if (slot.gameObject.activeInHierarchy)
                    result.Add(slot);
            return result;
        }

        int CandidateCount()
        {
            var list = new List<Game.Weapons.WeaponData>();
            scene.Inventory.GetCandidates(list);
            return list.Count;
        }

        static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator C1_Hidden_DuringPlay()
        {
            yield return null;
            Assert.IsFalse(view.Panel.activeInHierarchy, "평소에는 카드 화면이 보이지 않는다");
        }

        [UnityTest]
        public IEnumerator C2_LevelUp_ShowsUpToTwoCards_LabelledUpgradeOrNew()
        {
            int expected = Mathf.Min(2, CandidateCount());
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;

            Assert.AreEqual(GameState.LevelUp, State);
            Assert.IsTrue(view.Panel.activeInHierarchy, "레벨업 시 카드 화면 표시");
            var visible = VisibleSlots();
            Assert.AreEqual(expected, visible.Count, "후보가 2개 이상이면 2장, 1개면 1장");

            var cards = scene.LevelUpFlow.CurrentCards;
            for (int i = 0; i < visible.Count; i++)
            {
                Assert.AreEqual(cards[i].Weapon.displayName, visible[i].NameLabel);
                bool owned = scene.Inventory.Find(cards[i].Weapon) != null;
                StringAssert.Contains(owned ? "강화" : "신규 획득", visible[i].KindLabel, "보유 무기는 강화, 미보유는 신규 획득으로 표기");
            }
            if (cards.Count == 2)
                Assert.AreNotSame(cards[0].Weapon, cards[1].Weapon, "같은 무기가 두 장 나오지 않는다");
        }

        [UnityTest]
        public IEnumerator C3_Cards_AreOnScreen_NotOverlapping_AndClickable()
        {
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;
            Canvas.ForceUpdateCanvases();

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "InGame 씬에 EventSystem이 없다");
            Assert.IsNotNull(eventSystem.GetComponent<InputSystemUIInputModule>());

            var screen = new Rect(0, 0, Screen.width, Screen.height);
            var rects = new List<Rect>();
            foreach (var slot in VisibleSlots())
            {
                var rect = ScreenRect((RectTransform)slot.transform);
                Assert.IsTrue(screen.Contains(rect.min) && screen.Contains(rect.max), $"{slot.name}이 화면 밖으로 나감 {rect}");
                foreach (var other in rects)
                    Assert.IsFalse(rect.Overlaps(other), "카드끼리 겹치면 안 된다");
                rects.Add(rect);

                var data = new PointerEventData(eventSystem) { position = rect.center };
                var hits = new List<RaycastResult>();
                eventSystem.RaycastAll(data, hits);
                Assert.IsNotEmpty(hits, $"{slot.name} 위치에 레이캐스트가 닿지 않는다");
                Assert.AreSame(slot.Button, hits[0].gameObject.GetComponentInParent<Button>(), $"{slot.name}이 최상단 클릭 대상이 아니다");
            }
        }

        [UnityTest]
        public IEnumerator C4_ClickingCard_AppliesWeapon_AndReturnsToPlay()
        {
            scene.Level.AddXp(scene.Level.RequiredXp);
            yield return null;
            var card = scene.LevelUpFlow.CurrentCards[0];
            int levelBefore = scene.Inventory.GetLevel(card.Weapon);

            VisibleSlots()[0].Button.onClick.Invoke();
            Assert.AreEqual(levelBefore, scene.Inventory.GetLevel(card.Weapon), "카드가 뜬 직후 클릭은 무시 (입력 잠금)");

            yield return new WaitForSecondsRealtime(scene.XpDropper.Settings.cardInputLockSeconds + 0.05f);
            VisibleSlots()[0].Button.onClick.Invoke();

            Assert.AreEqual(levelBefore + 1, scene.Inventory.GetLevel(card.Weapon), "강화면 +1, 신규면 Lv1 획득");
            Assert.AreEqual(GameState.Play, State);
            Assert.IsFalse(view.Panel.activeInHierarchy, "선택 후 카드 화면 닫힘");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator C5_GrowthCompleteWhileCardsOpen_DiscardsPending_AndReturnsToPlay()
        {
            scene.Level.AddXp(scene.Level.RequiredXp);
            scene.Level.AddXp(scene.Level.RequiredXp);
            Assert.AreEqual(2, scene.Level.PendingLevelUps);

            // 카드가 떠 있는 동안 모든 무기가 최대 레벨이 된 상황 → 남은 대기 폐기 (6-A절 규칙 8)
            foreach (var data in scene.Inventory.Catalog.weapons)
            {
                scene.Inventory.Acquire(data);
                while (scene.Inventory.Upgrade(data)) { }
            }
            Assert.AreEqual(0, CandidateCount());
            Assert.AreEqual(0, scene.Level.PendingLevelUps);

            yield return new WaitForSecondsRealtime(scene.XpDropper.Settings.cardInputLockSeconds + 0.05f);
            Assert.IsTrue(scene.LevelUpFlow.Choose(0));
            Assert.AreEqual(GameState.Play, State, "더 고를 카드가 없으므로 Play 복귀");
            Assert.IsFalse(view.Panel.activeInHierarchy);
        }
    }
}
