using UnityEngine;

namespace LastTrain.Release
{
    public static class AppReleaseConfigLocator
    {
        public const string DefaultAssetPath = "Assets/Data/Release/AppReleaseConfig.asset";
        private const string ResourcesName = "AppReleaseConfig";

        public static AppReleaseConfig Load()
        {
            AppReleaseConfig config = Resources.Load<AppReleaseConfig>(ResourcesName);
            return config != null ? config : ScriptableObject.CreateInstance<AppReleaseConfig>();
        }
    }
}
