using LastTrain.Release;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 55: Soft Launch 버전 범프와 Release 동기화.</summary>
    public static class Unit55SoftLaunchReleaseMenu
    {
        public const string SoftLaunchVersionName = "0.5.0";
        public const int SoftLaunchMinBundleCode = 7;

        [MenuItem("Tools/막차 생존/개발 단위 55 Soft Launch 버전 동기화")]
        public static void SyncFromMenu()
        {
            ApplyVersionAndSync();
            ReleaseBuildValidator.Validate(strictRelease: true, throwOnError: false);
            EditorUtility.DisplayDialog(
                "Unit 55",
                $"Version {SoftLaunchVersionName} / bundle ≥ {SoftLaunchMinBundleCode} 로 동기화했습니다.\n" +
                "AAB는 Tools → 막차 생존 → Release → 서명·버전업 후 Release AAB 빌드 를 사용하세요.",
                "확인");
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit55SoftLaunchReleaseMenu.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                ApplyVersionAndSync();
                ReleaseBuildValidator.Validate(strictRelease: true, throwOnError: true);
                Debug.Log("[Unit55SoftLaunchReleaseMenu] OK " + SoftLaunchVersionName);
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit55SoftLaunchReleaseMenu] " + ex);
                EditorApplication.Exit(1);
            }
        }

        public static void ApplyVersionAndSync()
        {
            AppReleaseConfig config = ReleaseConfigSync.LoadOrCreateConfig();
            WriteVersion(config, SoftLaunchVersionName, SoftLaunchMinBundleCode);

            AppReleaseConfig resources = AssetDatabase.LoadAssetAtPath<AppReleaseConfig>(
                "Assets/Resources/AppReleaseConfig.asset");
            if (resources != null && resources != config)
            {
                WriteVersion(resources, SoftLaunchVersionName, SoftLaunchMinBundleCode);
            }

            ReleaseAssetsBuilder.EnsureReleaseAssets();
            ReleaseConfigSync.ApplyToPlayerSettings(ReleaseConfigSync.LoadOrCreateConfig());
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.buildAppBundle = true;
            AssetDatabase.SaveAssets();
        }

        private static void WriteVersion(AppReleaseConfig config, string versionName, int minBundleCode)
        {
            if (config == null)
            {
                return;
            }

            var so = new SerializedObject(config);
            SerializedProperty nameProp = so.FindProperty("versionName");
            SerializedProperty codeProp = so.FindProperty("androidBundleVersionCode");
            if (nameProp != null)
            {
                nameProp.stringValue = versionName;
            }

            if (codeProp != null)
            {
                codeProp.intValue = Mathf.Max(minBundleCode, codeProp.intValue);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
