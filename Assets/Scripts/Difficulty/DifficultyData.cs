using UnityEngine;

namespace LastTrain.Difficulty
{
    /// <summary>난이도 프로필 ScriptableObject. 코드 복제 없이 새 난이도를 추가한다.</summary>
    [CreateAssetMenu(fileName = "Difficulty_", menuName = "Last Train/Difficulty Data")]
    public sealed class DifficultyData : ScriptableObject, Data.IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id = DifficultyIds.Normal;
        [SerializeField] private string displayName = "일반 막차";
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private int sortOrder;
        [SerializeField] private DifficultyUnlockCondition unlockCondition = new DifficultyUnlockCondition();

        [Header("Combat Multipliers")]
        [SerializeField] private float enemyHealthMultiplier = 1f;
        [SerializeField] private float enemyMoveSpeedMultiplier = 1f;
        [SerializeField] private float enemyTrainDamageMultiplier = 1f;
        [SerializeField] private float enemyCountMultiplier = 1f;
        [SerializeField] private float spawnIntervalMultiplier = 1f;
        [SerializeField] private float eliteSpawnRate;
        [SerializeField] private float bossHealthMultiplier = 1f;
        [SerializeField] private int bossAbilityCount;

        [Header("Economy")]
        [SerializeField] private int startingCoins;
        [SerializeField] private int startingTrainHealth;
        [SerializeField] private float summonCostMultiplier = 1f;
        [SerializeField] private float shopPriceMultiplier = 1f;
        [SerializeField] private float rewardMultiplier = 1f;

        [Header("Pacing")]
        [SerializeField] private float preparationTime = 5f;
        [SerializeField] private DifficultyModifierData[] allowedModifiers = System.Array.Empty<DifficultyModifierData>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int SortOrder => sortOrder;
        public DifficultyUnlockCondition UnlockCondition => unlockCondition;

        public float EnemyHealthMultiplier => Mathf.Max(0.01f, enemyHealthMultiplier);
        public float EnemyMoveSpeedMultiplier => Mathf.Max(0.01f, enemyMoveSpeedMultiplier);
        public float EnemyTrainDamageMultiplier => Mathf.Max(0.01f, enemyTrainDamageMultiplier);
        public float EnemyCountMultiplier => Mathf.Max(0.01f, enemyCountMultiplier);
        public float SpawnIntervalMultiplier => Mathf.Max(0.01f, spawnIntervalMultiplier);
        public float EliteSpawnRate => Mathf.Clamp01(eliteSpawnRate);
        public float BossHealthMultiplier => Mathf.Max(0.01f, bossHealthMultiplier);
        public int BossAbilityCount => Mathf.Max(0, bossAbilityCount);

        public int StartingCoins => Mathf.Max(0, startingCoins);
        public int StartingTrainHealth => Mathf.Max(0, startingTrainHealth);
        public float SummonCostMultiplier => Mathf.Max(0.01f, summonCostMultiplier);
        public float ShopPriceMultiplier => Mathf.Max(0.01f, shopPriceMultiplier);
        public float RewardMultiplier => Mathf.Max(0f, rewardMultiplier);
        public float PreparationTime => Mathf.Max(0f, preparationTime);
        public DifficultyModifierData[] AllowedModifiers => allowedModifiers ?? System.Array.Empty<DifficultyModifierData>();
    }
}
