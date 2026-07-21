using LastTrain.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.Data
{
    /// <summary>
    /// GameDatabase 로드 헬퍼.
    /// Resources 우선, 에디터에서는 Assets/Data 경로로 폴백한다.
    /// </summary>
    public static class GameDatabaseLocator
    {
        public const string AssetPath = "Assets/Data/GameDatabase.asset";
        public const string ResourcesName = "GameDatabase";

        public static GameDatabase Load()
        {
            GameDatabase database = Resources.Load<GameDatabase>(ResourcesName);
            if (database != null)
            {
                return database;
            }

#if UNITY_EDITOR
            database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetPath);
#endif
            return database;
        }
    }
}
