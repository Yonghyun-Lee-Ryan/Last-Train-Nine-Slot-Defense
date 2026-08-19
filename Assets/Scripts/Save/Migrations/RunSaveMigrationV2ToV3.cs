using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>RunSave v2 → v3. 라이브 이벤트 회차 스냅샷(종료 후에도 이어하기 배율 유지).</summary>
    public sealed class RunSaveMigrationV2ToV3 : ISaveMigration
    {
        public int FromVersion => 2;
        public int ToVersion => 3;

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
                data.liveEventId ??= string.Empty;
                data.liveEventBoostedPassengerIds ??= System.Array.Empty<string>();
                data.liveEventRestrictedPassengerIds ??= System.Array.Empty<string>();
                if (data.liveEventBoostAttackMultiplier <= 0f)
                {
                    data.liveEventBoostAttackMultiplier = 1f;
                }

                return JsonUtility.ToJson(data, prettyPrint: false);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RunSaveMigrationV2ToV3] {ex.Message}");
                return null;
            }
        }
    }
}
