using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.Data
{
    /// <summary>VisualDatabase 로드 헬퍼.</summary>
    public static class VisualDatabaseLocator
    {
        public const string AssetPath = "Assets/Data/Visual/VisualDatabase.asset";
        public const string ResourcesName = "VisualDatabase";

        public static VisualDatabase Load()
        {
            VisualDatabase database = Resources.Load<VisualDatabase>(ResourcesName);
            if (database != null)
            {
                return database;
            }

#if UNITY_EDITOR
            database = AssetDatabase.LoadAssetAtPath<VisualDatabase>(AssetPath);
#endif
            return database;
        }
    }
}
