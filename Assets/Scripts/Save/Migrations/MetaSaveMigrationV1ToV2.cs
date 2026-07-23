using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>MetaSave v1 → v2. EnsureDefaults 후 버전을 올린다.</summary>
    public sealed class MetaSaveMigrationV1ToV2 : ISaveMigration
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
                Debug.LogWarning($"[MetaSaveMigrationV1ToV2] {ex.Message}");
                return null;
            }
        }
    }
}
