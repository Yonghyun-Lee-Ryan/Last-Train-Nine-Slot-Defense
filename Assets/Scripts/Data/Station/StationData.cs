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

        [Header("Briefing")]
        [TextArea(2, 4)]
        [SerializeField] private string bossPatternHint;

        public string Id => id;
        public string DisplayName => displayName;
        public StationType StationType => stationType;
        public int StationIndex => stationIndex;
        public float DifficultyMultiplier => difficultyMultiplier;
        public IReadOnlyList<WaveData> Waves => waves;
        public int RewardCoins => rewardCoins;
        public bool GrantsAbilityChoice => grantsAbilityChoice;
        public string BossPatternHint => bossPatternHint ?? string.Empty;

        public bool RequiresWaves => StationTypeRules.RequiresWaves(stationType);

        public int WaveCount => waves != null ? waves.Length : 0;

        /// <summary>무한 모드 등에서 템플릿을 복제해 역 번호·난이도만 덮어쓴다. 원본 에셋은 수정하지 않는다.</summary>
        public static StationData CreateRuntimeClone(
            StationData template,
            int runtimeStationIndex,
            float runtimeDifficultyMultiplier,
            StationType runtimeType,
            string runtimeDisplayName)
        {
            if (template == null)
            {
                return null;
            }

            StationData clone = CreateInstance<StationData>();
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.id = $"{template.id}_r{runtimeStationIndex}";
            clone.displayName = string.IsNullOrWhiteSpace(runtimeDisplayName)
                ? $"{runtimeStationIndex}번째 역"
                : runtimeDisplayName;
            clone.stationType = runtimeType;
            clone.stationIndex = Mathf.Max(1, runtimeStationIndex);
            clone.difficultyMultiplier = runtimeDifficultyMultiplier > 0f ? runtimeDifficultyMultiplier : 1f;
            clone.waves = template.waves;
            clone.rewardCoins = template.rewardCoins;
            clone.grantsAbilityChoice = template.grantsAbilityChoice
                || runtimeType == StationType.Boss;
            clone.bossPatternHint = template.bossPatternHint;
            return clone;
        }

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

            if (RequiresWaves && (waves == null || waves.Length == 0))
            {
                // CreateInstance 테스트 인스턴스는 웨이브 없이 역 메타만 검증하는 경우가 많다.
                if (DataValidationUtility.IsPersistedProjectAsset(this))
                {
                    Debug.LogWarning($"[StationData] '{id}' waves가 비어 있습니다.", this);
                }

                return;
            }

            if (waves == null)
            {
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
