using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 웨이브 표 (설계서 9-B절). 시간 구간마다 ① 적 구성 비율 ② 한 번에 내보내는 수(묶음) ③ 이동속도 배율을 정한다.
    /// 스폰 간격은 기존 SpawnCurve 그대로이고, 웨이브는 "무엇을·몇 마리씩·얼마나 빠르게"만 바꾼다.
    /// 웨이브 수·시각·비율은 전부 인스펙터에서 조정한다 (코드 수정 없이 밸런스 조정). 모든 값은 ※ 예시값.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Wave Table", fileName = "WaveTable")]
    public class WaveTable : ScriptableObject
    {
        [Serializable]
        public class Share
        {
            public Enemy prefab;
            [Range(0f, 1f)] public float ratio = 0.25f;
        }

        /// <summary>한 구간의 스폰 설정. 비율 합이 1보다 작으면 나머지는 기본 적(도병)이 채운다.</summary>
        [Serializable]
        public class Settings
        {
            public Share[] shares = Array.Empty<Share>();
            [Tooltip("스폰 간격마다 한 번에 내보내는 수 (같은 방향 근처에 무리로)")]
            [Min(1)] public int groupSize = 1;
            [Tooltip("적 기본 이동속도에 곱하는 배율 (상한 적용)")]
            [Min(0f)] public float speedMultiplier = 1f;

            /// <summary>roll(0~1 난수)로 고른 적. null이면 기본 적.</summary>
            public Enemy Pick(float roll)
            {
                float cumulative = 0f;
                foreach (var share in shares)
                {
                    cumulative += share.ratio;
                    if (roll < cumulative)
                        return share.prefab;
                }
                return null;
            }
        }

        [Serializable]
        public class Wave
        {
            [Tooltip("이 웨이브가 시작되는 경과 시간(초, 게임 시간)")]
            [Min(0f)] public float startSeconds;
            public Settings settings = new();
        }

        [Tooltip("시작 시각 순서대로. 첫 웨이브는 0초")]
        public Wave[] waves = { new() };

        [Header("보스")]
        [Tooltip("보스 자동 등장 시각(초)")]
        [Min(0f)] public float bossSeconds = 600f;
        [Tooltip("[보스 소환] 버튼이 생기는 시각(초)")]
        [Min(0f)] public float earlySummonSeconds = 360f;
        [Tooltip("보스전 중 스폰 설정 (웨이브 2와 동일)")]
        public Settings bossPhase = new();
        [Tooltip("보스전 스폰 간격 = SpawnCurve의 이 시각(초) 값으로 고정 (웨이브 2 구간)")]
        [Min(0f)] public float bossIntervalAtSeconds = 180f;

        [Header("공통")]
        [Tooltip("적 이동속도 상한 = 플레이어 기본 이동속도(매화보 적용 전) × 이 값")]
        [Range(0.1f, 1f)] public float enemySpeedCapRatio = 0.7f;
        [Tooltip("\"제 N파\" 문구 표시 시간(초)")]
        [Min(0.1f)] public float bannerSeconds = 2f;

        public int WaveCount => waves.Length;

        /// <summary>경과 시간이 속한 웨이브 번호 (1부터). 보스 단계 여부는 따지지 않는다.</summary>
        public int GetWaveNumber(float seconds)
        {
            int number = 1;
            for (int i = 0; i < waves.Length; i++)
                if (seconds >= waves[i].startSeconds)
                    number = i + 1;
            return number;
        }

        public Settings GetSettings(int waveNumber) =>
            waves.Length == 0 ? bossPhase : waves[Mathf.Clamp(waveNumber - 1, 0, waves.Length - 1)].settings;

        /// <summary>min(적 기본 속도 × 배율, 플레이어 기본 이동속도 × 상한 비율)</summary>
        public float GetEnemySpeed(float baseSpeed, float multiplier, float playerBaseSpeed) =>
            Mathf.Min(baseSpeed * multiplier, playerBaseSpeed * enemySpeedCapRatio);

        /// <summary>표에 나오는 모든 적 프리팹 (풀 미리 채우기·테스트용)</summary>
        public void CollectPrefabs(List<Enemy> result)
        {
            void Add(Settings settings)
            {
                foreach (var share in settings.shares)
                    if (share.prefab != null && !result.Contains(share.prefab))
                        result.Add(share.prefab);
            }
            foreach (var wave in waves)
                Add(wave.settings);
            Add(bossPhase);
        }
    }
}
