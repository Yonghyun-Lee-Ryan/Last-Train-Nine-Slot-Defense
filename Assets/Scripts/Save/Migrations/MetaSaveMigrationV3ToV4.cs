using UnityEngine;

namespace LastTrain.Save
{
    /// <summary>MetaSave v3 → v4. Unit 51 Endless 마일스톤 수령 목록.</summary>
    public sealed class MetaSaveMigrationV3ToV4 : ISaveMigration
    {
        public int FromVersion => 3;
        public int ToVersion => 4;

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
                Debug.LogWarning($"[MetaSaveMigrationV3ToV4] {ex.Message}");
                return null;
            }
        }
    }
}
