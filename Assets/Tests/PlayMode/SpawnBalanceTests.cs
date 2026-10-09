using System.Collections;
using Game.Enemies;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// [시연용 스폰 증가 7-D절 2] 조정 목표 측정: 매화검기 Lv1만 가진 채 제자리에 서 있을 때, 2분 시점 화면 안 적 수가
    /// 화면의 약 1/5(약 35마리 ※)인지 본다. 측정값은 콘솔에 "[SpawnBalance]"로 남겨 작업정리에 옮긴다.
    /// 무적 + 자석 끄기로 "죽거나 레벨업해서 조건이 바뀌는" 일을 막는다.
    /// </summary>
    public class SpawnBalanceTests
    {
        const float MeasureAt = 120f;
        const int Target = 35;
        const int Tolerance = 8; // ※ 무작위 스폰 편차 허용
        const float Speed = 4f;   // 게임 시간 가속 (실제 대기 시간 단축)

        readonly InGameScene scene = new();

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator EnemiesOnScreenAtTwoMinutes_IsAboutOneFifthOfScreen()
        {
            yield return scene.Load();
            Object.FindFirstObjectByType<PlayerHealth>().Invincible = true;
            scene.Magnet.enabled = false; // XP를 먹지 않아 레벨업(무기 강화)이 일어나지 않게
            Time.timeScale = Speed;

            int samples = 0;
            float nextLog = 30f;
            while (scene.Spawner.Elapsed < MeasureAt)
            {
                if (scene.Spawner.Elapsed >= nextLog)
                {
                    Debug.Log($"[SpawnBalance] {nextLog:F0}s 화면 안 {CountOnScreen()} / 활성 {EnemyRegistry.Active.Count} / 간격 {scene.Spawner.CurrentInterval:F2}s");
                    nextLog += 30f;
                }
                samples++;
                yield return null;
            }

            int onScreen = CountOnScreen();
            Debug.Log($"[SpawnBalance] {MeasureAt:F0}s 화면 안 {onScreen} (목표 약 {Target}) / 활성 {EnemyRegistry.Active.Count} / 간격 {scene.Spawner.CurrentInterval:F2}s / 프레임 {samples}");
            Assert.AreEqual(1, scene.Inventory.Owned.Count, "무기 Lv1 하나로 측정");
            Assert.AreEqual(1, scene.Weapon.Level);
            Assert.That(onScreen, Is.InRange(Target - Tolerance, Target + Tolerance), "2분 시점 화면의 약 1/5이 적");
        }

        int CountOnScreen()
        {
            int count = 0;
            foreach (var enemy in EnemyRegistry.Active)
            {
                var v = scene.Camera.WorldToViewportPoint(enemy.transform.position);
                if (v.x is >= 0f and <= 1f && v.y is >= 0f and <= 1f)
                    count++;
            }
            return count;
        }
    }
}
