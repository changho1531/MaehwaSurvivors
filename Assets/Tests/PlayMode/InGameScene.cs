using System.Collections;
using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.PlayMode
{
    /// <summary>InGame 씬을 로드하고 테스트에 필요한 핵심 오브젝트를 찾아 두는 헬퍼.</summary>
    public class InGameScene
    {
        public PlayerMovement Player { get; private set; }
        public EnemySpawner Spawner { get; private set; }
        public AutoWeapon Weapon { get; private set; }
        public Camera Camera { get; private set; }

        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.InGame, LoadSceneMode.Single);
            yield return null;

            Player = Object.FindFirstObjectByType<PlayerMovement>();
            Spawner = Object.FindFirstObjectByType<EnemySpawner>();
            Weapon = Object.FindFirstObjectByType<AutoWeapon>();
            Camera = Camera.main;

            Assert.IsNotNull(Player, "InGame 씬에 PlayerMovement가 없다");
        }

        /// <summary>자동 스폰을 멈추고, 현재 활성인 적을 모두 풀로 돌려 깨끗한 상태로 만든다.</summary>
        public void StopSpawningAndClear()
        {
            if (Spawner == null)
                return;
            Spawner.enabled = false;
            foreach (var enemy in EnemyRegistry.Active.ToArrayCopy())
                Spawner.Pool.Release(enemy);
        }

        /// <summary>풀에서 적을 꺼내 플레이어 기준 offset 위치에 둔다. chase=false면 제자리에 고정.</summary>
        public Enemy SpawnAt(Vector2 offset, bool chase)
        {
            var enemy = Spawner.Spawn();
            Assert.IsNotNull(enemy);
            var position = (Vector2)Player.transform.position + offset;
            enemy.transform.position = position;
            enemy.GetComponent<Rigidbody2D>().position = position;
            enemy.Init(chase ? Player.transform : null, Spawner.Pool.Release);
            return enemy;
        }
    }

    static class ListExtensions
    {
        public static T[] ToArrayCopy<T>(this System.Collections.Generic.IReadOnlyList<T> list)
        {
            var copy = new T[list.Count];
            for (int i = 0; i < list.Count; i++)
                copy[i] = list[i];
            return copy;
        }
    }
}
