using Game.Core;
using Game.Enemies;
using Game.Enemies.Boss;
using Game.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 에디터 전용 개발자 모드 창 (Tools/Project/개발자 모드). 플레이 중 버튼으로 DebugKeys 기능과 보스 확인용 조작을 쓴다.
    /// DebugKeys처럼 기존 시스템의 공개 진입점만 호출한다 — 디버그 전용 우회 경로를 만들지 않는다.
    /// 에디터 폴더라 빌드에는 들어가지 않는다.
    /// </summary>
    public class DevModeWindow : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Tools/Project/개발자 모드")]
        static void Open() => GetWindow<DevModeWindow>("개발자 모드");

        void OnInspectorUpdate() => Repaint(); // 플레이 중 보스 HP · 패턴 표시 갱신

        void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }
            var keys = FindFirstObjectByType<DebugKeys>();
            if (keys == null)
            {
                EditorGUILayout.HelpBox("InGame 씬에서 사용할 수 있습니다 (DebugKeys 없음).", MessageType.Info);
                return;
            }

            // 카드 선택 · 일시정지 · 결과 화면 중에는 상태가 꼬이지 않게 플레이 중에만 (DebugKeys와 같은 규칙)
            bool playing = GameManager.IsIn(GameState.Play);
            if (!playing)
                EditorGUILayout.HelpBox("게임 진행 중(Play 상태)에만 누를 수 있습니다.", MessageType.None);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (new EditorGUI.DisabledScope(!playing))
            {
                DrawPlayer(keys);
                EditorGUILayout.Space();
                DrawBoss();
            }
            EditorGUILayout.EndScrollView();
        }

        static void DrawPlayer(DebugKeys keys)
        {
            EditorGUILayout.LabelField("플레이어 · 무기 (F1~F5)", EditorStyles.boldLabel);
            if (GUILayout.Button("레벨업 (F1)")) keys.LevelUpNow();
            if (GUILayout.Button("무적 켜기/끄기 (F2)")) keys.ToggleInvincible();
            if (GUILayout.Button("보유 무기 최대 레벨 (F4)")) keys.MaxOwnedWeapons();
            if (GUILayout.Button("스폰 최대 켜기/끄기 (F5)")) keys.ToggleFastestSpawn();
            DrawWeaponPicker(keys.Inventory);
        }

        // 카탈로그(레벨업 카드에 나오는 무기 목록)에서 골라 바로 획득. 이미 보유한 무기는 레벨만 보여 주고 잠근다.
        static void DrawWeaponPicker(Game.Weapons.WeaponInventory inventory)
        {
            EditorGUILayout.LabelField("무기 획득 (F3 = 화산십이검)");
            if (inventory == null || inventory.Catalog == null)
            {
                EditorGUILayout.HelpBox("WeaponInventory 또는 카탈로그가 연결되지 않았습니다.", MessageType.Warning);
                return;
            }
            foreach (var data in inventory.Catalog.weapons)
            {
                if (data == null) continue;
                int level = inventory.GetLevel(data);
                using (new EditorGUI.DisabledScope(level > 0))
                    if (GUILayout.Button(level > 0 ? $"{data.displayName}  (보유 Lv{level})" : data.displayName))
                        inventory.Acquire(data);
            }
        }

        static void DrawBoss()
        {
            EditorGUILayout.LabelField("보스", EditorStyles.boldLabel);
            var waves = FindFirstObjectByType<WaveDirector>();
            var spawner = FindFirstObjectByType<BossSpawner>();
            var boss = spawner != null ? spawner.Boss : null;
            var behaviour = spawner != null ? spawner.Behaviour : null;

            if (boss == null || !boss.IsAlive)
            {
                using (new EditorGUI.DisabledScope(waves == null || waves.Table == null || waves.IsBossPhase))
                    if (GUILayout.Button("보스 단계로 건너뛰기 (10분 도달과 같은 흐름)"))
                        waves.Advance(waves.Table.bossSeconds);
                return;
            }

            EditorGUILayout.LabelField($"HP {boss.CurrentHp:0} / {behaviour.Data.maxHp:0}   {behaviour.Phase}페이즈   패턴: {(behaviour.Current != null ? behaviour.Current.Name : "-")}");

            float phase2Hp = behaviour.Data.maxHp * behaviour.Data.phase2HpRatio;
            using (new EditorGUI.DisabledScope(behaviour.Phase >= 2 || boss.CurrentHp <= phase2Hp))
                if (GUILayout.Button("2페이즈로 (HP를 전환선 아래로)"))
                    boss.TakeDamage(boss.CurrentHp - phase2Hp + 1f); // 평소 피격 경로 → 전환 연출도 그대로

            EditorGUILayout.LabelField("다음 패턴 지정 (쿨타임 · 거리 무시)");
            foreach (var pattern in behaviour.Patterns)
                if (GUILayout.Button($"{pattern.Name}  (쿨 {pattern.CooldownRemaining:0.0}초)"))
                    behaviour.RequestPattern(pattern);

            if (GUILayout.Button("보스 처치"))
                boss.TakeDamage(boss.CurrentHp);
        }
    }
}
