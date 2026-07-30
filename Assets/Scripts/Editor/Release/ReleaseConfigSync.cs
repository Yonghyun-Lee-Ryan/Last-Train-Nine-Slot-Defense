using LastTrain.Release;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class ReleaseConfigSync
    {
        public const string ReleaseConfigPath = AppReleaseConfigLocator.DefaultAssetPath;

        /// <summary>Play Console 제출 시 요구되는 Target API (2025+).</summary>
        public const AndroidSdkVersions RequiredTargetSdk = AndroidSdkVersions.AndroidApiLevel35;

        [MenuItem("Tools/막차 생존/Release/Sync Release Config to Player Settings")]
        public static void SyncFromMenu()
        {
            AppReleaseConfig config = LoadOrCreateConfig();
            ApplyToPlayerSettings(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[ReleaseConfigSync] Player Settings synchronized.");
            EditorUtility.DisplayDialog(
                "Release Sync",
                "Player Settings를 AppReleaseConfig 기준으로 동기화했습니다.\n" +
                "서명 Keystore는 Publishing Settings에서 로컬 경로를 확인하세요.",
                "확인");
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
            PlayerSettings.Android.targetSdkVersion = RequiredTargetSdk;

            // 세로형 모바일 게임 — 가로 회전 금지
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.102f, 0.153f, 0.267f, 1f); // CarNavy

            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.splitApplicationBinary = true;

            // Managed stripping — Release 안전 기본값
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
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
