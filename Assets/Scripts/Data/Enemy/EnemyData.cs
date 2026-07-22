using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 적 정적 데이터. ScriptableObject 원본은 런타임에서 수정하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "Last Train/Enemy Data")]
    public class EnemyData : ScriptableObject, IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Stats")]
        [SerializeField] private float baseHealth = 50f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float trainDamage = 5f;
        [SerializeField] private float defense;
        [SerializeField] private EnemyType enemyType = EnemyType.Normal;

        [Header("Reward")]
        [SerializeField] private int coinReward = 3;

        [Header("Special")]
        [SerializeField] private string abilityId;
        [SerializeField] private string splitMinionId;
        [SerializeField] private BossPhaseThresholds bossPhaseThresholds;

        public string Id => id;
        public string DisplayName => displayName;
        public float BaseHealth => baseHealth;
        public float MoveSpeed => moveSpeed;
        public float TrainDamage => trainDamage;
        public float Defense => defense;
        public EnemyType EnemyType => enemyType;
        public int CoinReward => coinReward;
        public string AbilityId => abilityId;
        public string SplitMinionId => splitMinionId ?? string.Empty;
        public BossPhaseThresholds BossPhaseThresholds => bossPhaseThresholds;

        /// <summary>난도 계수를 적용한 최종 체력.</summary>
        public float GetScaledHealth(float stationDifficulty, float lineDifficulty = 1f)
        {
            return DataValidationUtility.CalculateEnemyHealth(baseHealth, stationDifficulty, lineDifficulty);
        }

        private void OnValidate()
        {
            if (!DataValidationUtility.IsValidId(id))
            {
                Debug.LogWarning($"[EnemyData] '{name}' ID가 비어 있습니다.", this);
            }

            if (!DataValidationUtility.IsPositive(baseHealth))
            {
                Debug.LogWarning($"[EnemyData] '{id}' baseHealth는 0보다 커야 합니다.", this);
            }

            if (!DataValidationUtility.IsPositive(moveSpeed))
            {
                Debug.LogWarning($"[EnemyData] '{id}' moveSpeed는 0보다 커야 합니다.", this);
            }

            if (!DataValidationUtility.IsNonNegative(trainDamage))
            {
                Debug.LogWarning($"[EnemyData] '{id}' trainDamage는 0 이상이어야 합니다.", this);
            }

            if (!DataValidationUtility.IsNonNegative(defense))
            {
                Debug.LogWarning($"[EnemyData] '{id}' defense는 0 이상이어야 합니다.", this);
            }

            if (!DataValidationUtility.IsNonNegative(coinReward))
            {
                Debug.LogWarning($"[EnemyData] '{id}' coinReward는 0 이상이어야 합니다.", this);
            }
        }
    }
}
