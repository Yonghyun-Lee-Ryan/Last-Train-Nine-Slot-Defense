using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 하나의 웨이브 정의. 여러 WaveSpawnData로 구성된다.
    /// </summary>
    [CreateAssetMenu(fileName = "Wave_", menuName = "Last Train/Wave Data")]
    public class WaveData : ScriptableObject, IDataWithId
    {
        [SerializeField] private string id;
        [SerializeField] private float delayBeforeStart;
        [SerializeField] private WaveSpawnData[] spawns;

        public string Id => id;
        public float DelayBeforeStart => delayBeforeStart;
        public IReadOnlyList<WaveSpawnData> Spawns => spawns;

        /// <summary>이 웨이브에서 생성될 총 적 수.</summary>
        public int GetTotalEnemyCount()
        {
            if (spawns == null || spawns.Length == 0)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < spawns.Length; i++)
            {
                total += Mathf.Max(0, spawns[i].count);
            }

            return total;
        }

        private void OnValidate()
        {
            if (!DataValidationUtility.IsValidId(id))
            {
                Debug.LogWarning($"[WaveData] '{name}' ID가 비어 있습니다.", this);
            }

            if (spawns == null || spawns.Length == 0)
            {
                Debug.LogWarning($"[WaveData] '{id}' spawns가 비어 있습니다.", this);
                return;
            }

            for (int i = 0; i < spawns.Length; i++)
            {
                WaveSpawnData spawn = spawns[i];
                if (spawn.enemy == null)
                {
                    Debug.LogWarning($"[WaveData] '{id}' spawns[{i}] enemy 참조가 비어 있습니다.", this);
                }

                if (spawn.count < 1)
                {
                    Debug.LogWarning($"[WaveData] '{id}' spawns[{i}] count는 1 이상이어야 합니다.", this);
                }

                if (spawn.spawnInterval < 0f)
                {
                    Debug.LogWarning($"[WaveData] '{id}' spawns[{i}] spawnInterval이 음수입니다.", this);
                }
            }
        }
    }
}
