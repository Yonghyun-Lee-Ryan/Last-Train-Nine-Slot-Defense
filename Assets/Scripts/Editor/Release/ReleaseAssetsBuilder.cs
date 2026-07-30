using System.IO;
using LastTrain.Data;
using LastTrain.Release;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>아이콘·스플래시·Resources 복사 등 Release 빌드 자산 준비.</summary>
    public static class ReleaseAssetsBuilder
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string ResourcesConfigPath = ResourcesFolder + "/AppReleaseConfig.asset";
        private const string IconPath = "Assets/Art/Sprites/UI/app_icon_512.png";
        private const string SplashPath = "Assets/Art/Sprites/UI/splash_portrait.png";

        [MenuItem("Tools/막차 생존/Release/Setup Release Assets")]
        public static void EnsureReleaseAssetsFromMenu()
        {
            EnsureReleaseAssets();
            EditorUtility.DisplayDialog(
                "Release Assets",
                "아이콘·스플래시·AppReleaseConfig Resources 복사와 Player Settings 동기화를 완료했습니다.",
                "확인");
        }

        public static void EnsureReleaseAssets()
        {
            AppReleaseConfig config = ReleaseConfigSync.LoadOrCreateConfig();
            EnsureResourcesCopy(config);
            EnsureGameDatabaseResources();
            EnsureSummonEconomyResources();
            EnsureCombatPrefabResources();
            EnsureAppIconAsset();
            EnsureSplashAsset();
            AssignAndroidIcons();
            AssignSplash();
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

            string source = AssetDatabase.GetAssetPath(config);
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            AppReleaseConfig existing = AssetDatabase.LoadAssetAtPath<AppReleaseConfig>(ResourcesConfigPath);
            if (existing == null)
            {
                AssetDatabase.CopyAsset(source, ResourcesConfigPath);
            }
            else
            {
                EditorUtility.CopySerialized(config, existing);
                EditorUtility.SetDirty(existing);
            }
        }

        /// <summary>
        /// Release 플레이어는 AssetDatabase를 쓸 수 없다.
        /// MainMenu/Tutorial 등이 GameDatabaseLocator(Resources)에 의존하므로 반드시 복사한다.
        /// </summary>
        private static void EnsureGameDatabaseResources()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            const string sourcePath = "Assets/Data/GameDatabase.asset";
            const string resourcesPath = ResourcesFolder + "/GameDatabase.asset";
            if (AssetDatabase.LoadAssetAtPath<GameDatabase>(sourcePath) == null)
            {
                Debug.LogError("[ReleaseAssetsBuilder] Assets/Data/GameDatabase.asset 없음.");
                return;
            }

            GameDatabase existing = AssetDatabase.LoadAssetAtPath<GameDatabase>(resourcesPath);
            if (existing == null)
            {
                if (!AssetDatabase.CopyAsset(sourcePath, resourcesPath))
                {
                    Debug.LogError("[ReleaseAssetsBuilder] GameDatabase Resources 복사 실패.");
                }

                return;
            }

            EditorUtility.CopySerialized(
                AssetDatabase.LoadAssetAtPath<GameDatabase>(sourcePath),
                existing);
            EditorUtility.SetDirty(existing);
        }

        private static void EnsureSummonEconomyResources()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            const string sourcePath = "Assets/Data/SummonEconomyConfig.asset";
            const string resourcesPath = ResourcesFolder + "/SummonEconomyConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<SummonEconomyConfig>(sourcePath) == null)
            {
                Debug.LogWarning("[ReleaseAssetsBuilder] SummonEconomyConfig source missing.");
                return;
            }

            SummonEconomyConfig existing = AssetDatabase.LoadAssetAtPath<SummonEconomyConfig>(resourcesPath);
            if (existing == null)
            {
                AssetDatabase.CopyAsset(sourcePath, resourcesPath);
                return;
            }

            EditorUtility.CopySerialized(
                AssetDatabase.LoadAssetAtPath<SummonEconomyConfig>(sourcePath),
                existing);
            EditorUtility.SetDirty(existing);
        }

        private static void EnsureCombatPrefabResources()
        {
            EnsureFolder("Assets/Resources", "Combat");
            CopyPrefabIfNeeded(
                "Assets/Prefabs/Enemies/BasicEnemy.prefab",
                "Assets/Resources/Combat/BasicEnemy.prefab");
            CopyPrefabIfNeeded(
                "Assets/Prefabs/Projectiles/BasicProjectile.prefab",
                "Assets/Resources/Combat/BasicProjectile.prefab");
            CopyPrefabIfNeeded(
                "Assets/Prefabs/UI/PassengerView.prefab",
                "Assets/Resources/Combat/PassengerView.prefab");
        }

        private static void CopyPrefabIfNeeded(string sourcePath, string destPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
            {
                Debug.LogWarning($"[ReleaseAssetsBuilder] Missing prefab: {sourcePath}");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(destPath) != null)
            {
                AssetDatabase.DeleteAsset(destPath);
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destPath))
            {
                Debug.LogWarning($"[ReleaseAssetsBuilder] Failed to copy {sourcePath} → {destPath}");
            }
        }

        private static void EnsureAppIconAsset()
        {
            EnsureFolder("Assets/Art/Sprites", "UI");
            Texture2D icon = FlatVectorDrawUtility.Create(512, 512);
            FlatVectorDrawUtility.FillRect(icon, new RectInt(0, 0, 512, 512), VisualThemePalette.CarNavy);
            FlatVectorDrawUtility.FillRoundedRect(
                icon,
                new RectInt(48, 48, 416, 416),
                64,
                VisualThemePalette.CarNavyLight,
                VisualThemePalette.FluorescentTeal);
            FlatVectorDrawUtility.FillRoundedRect(
                icon,
                new RectInt(140, 180, 232, 160),
                28,
                VisualThemePalette.WindowGlow,
                VisualThemePalette.Outline);
            FlatVectorDrawUtility.FillRect(icon, new RectInt(200, 100, 112, 36), VisualThemePalette.FluorescentTeal);
            FlatVectorDrawUtility.DrawOutlineCircle(icon, new Vector2(256f, 390f), 36f, VisualThemePalette.SeatFrame, 6);
            FlatVectorDrawUtility.SavePng(icon, IconPath);
            Object.DestroyImmediate(icon);
            AssetDatabase.ImportAsset(IconPath);
            ConfigureTextureAsDefault(IconPath);
        }

        private static void EnsureSplashAsset()
        {
            EnsureFolder("Assets/Art/Sprites", "UI");
            Texture2D splash = FlatVectorDrawUtility.Create(1080, 1920);
            FlatVectorDrawUtility.FillRect(splash, new RectInt(0, 0, 1080, 1920), VisualThemePalette.CarNavy);
            FlatVectorDrawUtility.FillRect(splash, new RectInt(0, 0, 1080, 160), VisualThemePalette.PanelDark);
            FlatVectorDrawUtility.FillRect(splash, new RectInt(0, 1760, 1080, 160), VisualThemePalette.PanelDark);
            FlatVectorDrawUtility.FillRect(splash, new RectInt(0, 1880, 1080, 40), VisualThemePalette.FluorescentTealDim);
            FlatVectorDrawUtility.FillRoundedRect(
                splash,
                new RectInt(140, 760, 800, 280),
                36,
                VisualThemePalette.PanelDark,
                VisualThemePalette.FluorescentTeal);
            FlatVectorDrawUtility.FillRect(splash, new RectInt(220, 860, 640, 80), VisualThemePalette.FluorescentTealDim);
            FlatVectorDrawUtility.SavePng(splash, SplashPath);
            Object.DestroyImmediate(splash);
            AssetDatabase.ImportAsset(SplashPath);
            ConfigureTextureAsSprite(SplashPath);
        }

        private static void AssignAndroidIcons()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogWarning("[ReleaseAssetsBuilder] app_icon_512.png 로드 실패.");
                return;
            }

            PlatformIconKind[] kinds = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android);
            for (int k = 0; k < kinds.Length; k++)
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kinds[k]);
                for (int i = 0; i < icons.Length; i++)
                {
                    int layers = Mathf.Max(1, icons[i].maxLayerCount);
                    var textures = new Texture2D[layers];
                    for (int layer = 0; layer < layers; layer++)
                    {
                        textures[layer] = icon;
                    }

                    icons[i].SetTextures(textures);
                }

                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kinds[k], icons);
            }

            // Legacy fallback
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Application);
        }

        private static void AssignSplash()
        {
            ConfigureTextureAsSprite(SplashPath);
            Sprite splash = AssetDatabase.LoadAssetAtPath<Sprite>(SplashPath);
            if (splash == null)
            {
                return;
            }

            PlayerSettings.SplashScreen.background = splash;
            PlayerSettings.SplashScreen.backgroundPortrait = splash;
            PlayerSettings.SplashScreen.backgroundColor = VisualThemePalette.CarNavy;
            PlayerSettings.SplashScreen.overlayOpacity = 0.4f;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }

        private static void ConfigureTextureAsDefault(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void ConfigureTextureAsSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
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
