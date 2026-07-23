using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>RunSave v1 → v2. 누락 필드는 JsonUtility 기본값으로 채운 뒤 버전을 올린다.</summary>
    public sealed class RunSaveMigrationV1ToV2 : ISaveMigration
    {
        public int FromVersion => 1;
        public int ToVersion => 2;

        public string Migrate(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                RunSaveData data = JsonUtility.FromJson<RunSaveData>(json);
                if (data == null)
                {
                    return null;
                }

                data.version = ToVersion;
                data.slots ??= new RunSaveData.SlotSave[Run.RunState.GridSlotCount];
                data.selectedAbilityIdsExpanded ??= System.Array.Empty<string>();
                data.shopOffers ??= System.Array.Empty<ShopOfferSave>();
                data.relicIds ??= System.Array.Empty<string>();
                if (string.IsNullOrWhiteSpace(data.difficultyId))
                {
                    data.difficultyId = Difficulty.DifficultyIds.Normal;
                }

                if (data.nextEnemyHealthMultiplier <= 0f)
                {
                    data.nextEnemyHealthMultiplier = 1f;
                }

                if (data.nextRewardCoinMultiplier <= 0f)
                {
                    data.nextRewardCoinMultiplier = 1f;
                }

                return JsonUtility.ToJson(data, prettyPrint: false);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RunSaveMigrationV1ToV2] {ex.Message}");
                return null;
            }
        }
    }
}
