using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>MetaSave v2 → v3. Unit 40 출석 필드 기본값.</summary>
    public sealed class MetaSaveMigrationV2ToV3 : ISaveMigration
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
                MetaSaveData data = JsonUtility.FromJson<MetaSaveData>(json);
                if (data == null)
                {
                    return null;
                }

                data.EnsureDefaults();
                data.version = ToVersion;
                return JsonUtility.ToJson(data, prettyPrint: false);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MetaSaveMigrationV2ToV3] {ex.Message}");
                return null;
            }
        }
    }
}
