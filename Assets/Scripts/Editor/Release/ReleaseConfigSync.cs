using LastTrain.Integrations;
using LastTrain.Release;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class ReleaseConfigSync
    {
        public const string ReleaseConfigPath = AppReleaseConfigLocator.DefaultAssetPath;

        [MenuItem("Tools/막차 생존/Release/Sync Release Config to Player Settings")]
        public static void SyncFromMenu()
        {
            AppReleaseConfig config = LoadOrCreateConfig();
            ApplyToPlayerSettings(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[ReleaseConfigSync] Player Settings synchronized.");
        }

        public static AppReleaseConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<AppReleaseConfig>(ReleaseConfigPath);
            if (config != null)
            {
                return config;
            }

            EnsureFolder("Assets/Data", "Release");
            config = ScriptableObject.CreateInstance<AppReleaseConfig>();
            AssetDatabase.CreateAsset(config, ReleaseConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        public static void ApplyToPlayerSettings(AppReleaseConfig config)
        {
            if (config == null)
            {
                return;
            }

            PlayerSettings.companyName = "LastTrainStudio";
            PlayerSettings.productName = config.DisplayName;
            PlayerSettings.bundleVersion = config.VersionName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, config.AndroidPackageName);
            PlayerSettings.Android.bundleVersionCode = config.AndroidBundleVersionCode;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.splitApplicationBinary = true;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
