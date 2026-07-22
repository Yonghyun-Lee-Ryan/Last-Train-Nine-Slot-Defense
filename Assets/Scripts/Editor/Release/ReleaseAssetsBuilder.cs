using System.IO;
using LastTrain.Release;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class ReleaseAssetsBuilder
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string ResourcesConfigPath = ResourcesFolder + "/AppReleaseConfig.asset";
        private const string IconSourcePath = "Assets/Art/Sprites/UI/main_menu_title.png";

        [MenuItem("Tools/막차 생존/Release/Setup Release Assets")]
        public static void EnsureReleaseAssetsFromMenu()
        {
            EnsureReleaseAssets();
            EditorUtility.DisplayDialog("Release Assets", "Release 에셋 설정을 완료했습니다.", "확인");
        }

        public static void EnsureReleaseAssets()
        {
            AppReleaseConfig config = ReleaseConfigSync.LoadOrCreateConfig();
            EnsureResourcesCopy(config);
            TryAssignAndroidIcon();
            ReleaseConfigSync.ApplyToPlayerSettings(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureResourcesCopy(AppReleaseConfig config)
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            AppReleaseConfig existing = AssetDatabase.LoadAssetAtPath<AppReleaseConfig>(ResourcesConfigPath);
            if (existing == null)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(config), ResourcesConfigPath);
            }
        }

        private static void TryAssignAndroidIcon()
        {
            if (!File.Exists(IconSourcePath))
            {
                Debug.LogWarning("[ReleaseAssetsBuilder] Icon source not found; skipping icon assignment.");
                return;
            }

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconSourcePath);
            if (icon == null)
            {
                return;
            }

            var icons = new Texture2D[] { icon };
            PlayerSettings.SetIcons(NamedBuildTarget.Android, icons, IconKind.Application);
        }
    }
}
