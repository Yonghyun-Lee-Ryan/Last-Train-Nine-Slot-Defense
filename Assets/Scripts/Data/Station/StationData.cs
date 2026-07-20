using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 역(스테이션) 정적 데이터. 여러 웨이브와 완료 보상을 포함한다.
    /// </summary>
    [CreateAssetMenu(fileName = "Station_", menuName = "Last Train/Station Data")]
    public class StationData : ScriptableObject, IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private StationType stationType = StationType.Normal;

        [Header("Progression")]
        [SerializeField] private int stationIndex = 1;
        [SerializeField] private float difficultyMultiplier = 1f;

        [Header("Waves")]
        [SerializeField] private WaveData[] waves;

        [Header("Reward")]
        [SerializeField] private int rewardCoins = 15;
        [SerializeField] private bool grantsAbilityChoice;

        public string Id => id;
        public string DisplayName => displayName;
        public StationType StationType => stationType;
        public int StationIndex => stationIndex;
        public float DifficultyMultiplier => difficultyMultiplier;
        public IReadOnlyList<WaveData> Waves => waves;
        public int RewardCoins => rewardCoins;
        public bool GrantsAbilityChoice => grantsAbilityChoice;

        public int WaveCount => waves != null ? waves.Length : 0;

        private void OnValidate()
        {
            if (!DataValidationUtility.IsValidId(id))
            {
                Debug.LogWarning($"[StationData] '{name}' ID가 비어 있습니다.", this);
            }

            if (stationIndex < 1)
            {
                Debug.LogWarning($"[StationData] '{id}' stationIndex는 1 이상이어야 합니다.", this);
            }

            if (!DataValidationUtility.IsPositive(difficultyMultiplier))
            {
                Debug.LogWarning($"[StationData] '{id}' difficultyMultiplier는 0보다 커야 합니다.", this);
            }

            if (waves == null || waves.Length == 0)
            {
                Debug.LogWarning($"[StationData] '{id}' waves가 비어 있습니다.", this);
                return;
            }

            for (int i = 0; i < waves.Length; i++)
            {
                if (waves[i] == null)
                {
                    Debug.LogWarning($"[StationData] '{id}' waves[{i}] 참조가 비어 있습니다.", this);
                }
            }

            if (!DataValidationUtility.IsNonNegative(rewardCoins))
            {
                Debug.LogWarning($"[StationData] '{id}' rewardCoins는 0 이상이어야 합니다.", this);
            }
        }
    }
}
