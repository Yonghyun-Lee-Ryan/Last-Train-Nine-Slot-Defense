using LastTrain.Data;
using LastTrain.Save;

namespace LastTrain.Difficulty
{
    /// <summary>난이도 조회·런타임 생성·해금 판정.</summary>
    public static class DifficultyService
    {
        public const string DefaultDifficultyId = DifficultyIds.Normal;

        public static DifficultyRuntime CreateRuntime(string difficultyId, GameDatabase database = null)
        {
            DifficultyData data = ResolveData(difficultyId, database);
            return DifficultyCalculator.CreateRuntime(data);
        }

        public static DifficultyData ResolveData(string difficultyId, GameDatabase database = null)
        {
            database ??= GameDatabaseLocator.Load();
            string resolvedId = string.IsNullOrWhiteSpace(difficultyId) ? DefaultDifficultyId : difficultyId;

            if (database != null && database.TryGetDifficulty(resolvedId, out DifficultyData data))
            {
                return data;
            }

            if (database != null
                && !string.Equals(resolvedId, DefaultDifficultyId, System.StringComparison.Ordinal)
                && database.TryGetDifficulty(DefaultDifficultyId, out DifficultyData normal))
            {
                return normal;
            }

            return null;
        }

        public static string ResolveSavedDifficultyId(string savedDifficultyId)
        {
            return string.IsNullOrWhiteSpace(savedDifficultyId) ? DefaultDifficultyId : savedDifficultyId;
        }

        public static bool IsUnlocked(DifficultyData data, MetaSaveData meta)
        {
            return DifficultyProgressService.IsUnlocked(data, meta);
        }
    }
}
