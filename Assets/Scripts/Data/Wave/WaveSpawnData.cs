using System;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 웨이브 내 단일 적 스폰 그룹 정의.
    /// README 15.1 JSON 구조(enemyId, count, spawnInterval)에 대응한다.
    /// </summary>
    [Serializable]
    public struct WaveSpawnData
    {
        [Tooltip("생성할 적 데이터")]
        public EnemyData enemy;

        [Tooltip("생성 수")]
        [Min(1)]
        public int count;

        [Tooltip("적 간 생성 간격(초)")]
        [Min(0f)]
        public float spawnInterval;

        [Tooltip("웨이브 시작 후 첫 생성까지 대기(초)")]
        [Min(0f)]
        public float spawnDelay;
    }
}
