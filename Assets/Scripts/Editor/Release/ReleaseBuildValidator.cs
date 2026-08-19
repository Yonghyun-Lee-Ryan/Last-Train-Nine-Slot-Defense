using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LastTrain.Data;
using LastTrain.Integrations;
using LastTrain.Release;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public sealed class ReleaseBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        [MenuItem("Tools/막차 생존/Release/Validate Release Build")]
        public static void ValidateFromMenu()
        {
            Validate(strictRelease: !EditorUserBuildSettings.development, throwOnError: false);
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.ReleaseBuildValidator.ValidateStrictReleaseBatch</summary>
        public static void ValidateStrictReleaseBatch()
        {
            try
            {
                Validate(strictRelease: true, throwOnError: true);
                Debug.Log("[ReleaseBuildValidator] Batch ValidateStrictRelease OK");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ReleaseBuildValidator] Batch failed: " + ex.Message);
                EditorApplication.Exit(1);
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
            {
                return;
            }

            bool strictRelease = !EditorUserBuildSettings.development;
            Validate(strictRelease, throwOnError: true);
        }

        public static void Validate(bool strictRelease, bool throwOnError)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            AppReleaseConfig config = AssetDatabase.LoadAssetAtPath<AppReleaseConfig>(ReleaseConfigSync.ReleaseConfigPath);
            if (config == null)
            {
                errors.Add($"Missing AppReleaseConfig at {ReleaseConfigSync.ReleaseConfigPath}");
            }

            if (AssetDatabase.LoadAssetAtPath<AdUnitConfig>("Assets/Data/Integration/AdUnitConfig.asset") == null)
            {
                errors.Add("Missing AdUnitConfig asset.");
            }

            if (AssetDatabase.LoadAssetAtPath<RemoteConfigDefaults>("Assets/Data/Integration/RemoteConfigDefaults.asset") == null)
            {
                errors.Add("Missing RemoteConfigDefaults asset.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset") == null)
            {
                errors.Add("Missing GameDatabase asset.");
            }

            if (AssetDatabase.LoadAssetAtPath<AppReleaseConfig>("Assets/Resources/AppReleaseConfig.asset") == null)
            {
                warnings.Add("Resources/AppReleaseConfig.asset 없음 — Setup Release Assets 실행 권장.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Resources/GameDatabase.asset") == null)
            {
                errors.Add("Resources/GameDatabase.asset 없음 — Release에서 튜토리얼/메뉴 DB 로드 실패. Setup Release Assets 실행.");
            }

            string[] requiredScenes =
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Game.unity",
                "Assets/Scenes/Result.unity",
            };

            for (int i = 0; i < requiredScenes.Length; i++)
            {
                if (!File.Exists(requiredScenes[i]))
                {
                    errors.Add($"Missing scene: {requiredScenes[i]}");
                }
            }

            ValidateGameCombatPools(errors);

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < requiredScenes.Length; i++)
            {
                string path = requiredScenes[i];
                bool enabled = buildScenes.Any(s => s.enabled && s.path == path);
                if (!enabled)
                {
                    errors.Add($"Scene not enabled in Build Settings: {path}");
                }
            }

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            {
                errors.Add("Android scripting backend must be IL2CPP.");
            }

            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                errors.Add("Android target architecture must be ARM64 only.");
            }

            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
            {
                errors.Add("Android minSdkVersion must be API 26+.");
            }

            if (PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevelAuto
                && (int)PlayerSettings.Android.targetSdkVersion < (int)ReleaseConfigSync.RequiredTargetSdk)
            {
                errors.Add($"Android targetSdkVersion must be >= API {(int)ReleaseConfigSync.RequiredTargetSdk} (Play Console).");
            }

            if (!EditorUserBuildSettings.buildAppBundle)
            {
                errors.Add("EditorUserBuildSettings.buildAppBundle must be enabled.");
            }

            if (!PlayerSettings.Android.splitApplicationBinary)
            {
                errors.Add("Android App Bundle (splitApplicationBinary) must be enabled.");
            }

            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                warnings.Add("defaultInterfaceOrientation is not Portrait.");
            }

            if (PlayerSettings.allowedAutorotateToLandscapeLeft || PlayerSettings.allowedAutorotateToLandscapeRight)
            {
                warnings.Add("Landscape autorotation is enabled — Sync Release Config 권장.");
            }

            Texture2D[] legacyIcons = PlayerSettings.GetIcons(NamedBuildTarget.Android, IconKind.Application);
            bool hasIcon = legacyIcons != null && legacyIcons.Length > 0 && legacyIcons[0] != null;
            if (!hasIcon)
            {
                PlatformIconKind[] kinds = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android);
                for (int k = 0; k < kinds.Length && !hasIcon; k++)
                {
                    PlatformIcon[] platformIcons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kinds[k]);
                    for (int i = 0; i < platformIcons.Length; i++)
                    {
                        Texture2D[] textures = platformIcons[i].GetTextures();
                        if (textures != null && textures.Length > 0 && textures[0] != null)
                        {
                            hasIcon = true;
                            break;
                        }
                    }
                }
            }

            if (!hasIcon)
            {
                errors.Add("Android application icons are empty — run Setup Release Assets.");
            }

            if (config != null)
            {
                if (PlayerSettings.bundleVersion != config.VersionName)
                {
                    warnings.Add("PlayerSettings.bundleVersion differs from AppReleaseConfig.");
                }

                if (PlayerSettings.Android.bundleVersionCode != config.AndroidBundleVersionCode)
                {
                    warnings.Add("Android bundleVersionCode differs from AppReleaseConfig.");
                }

                if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != config.AndroidPackageName)
                {
                    warnings.Add("Android package name differs from AppReleaseConfig.");
                }

                if (string.IsNullOrWhiteSpace(config.PrivacyPolicyUrl)
                    || config.PrivacyPolicyUrl.StartsWith("https://example.com", System.StringComparison.Ordinal))
                {
                    errors.Add(
                        "Privacy policy URL이 비어 있거나 example.com placeholder입니다. Soft Launch 전 실제 공개 URL이 필요합니다.");
                }
                else if (!config.PrivacyPolicyUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Privacy policy URL must use https://");
                }
            }

            if (strictRelease && !AdMobPrelaunchGuard.TryValidateTrackedAssets(out string adMobError))
            {
                errors.Add(adMobError);
            }

            AppendAdMobSdkChecks(warnings, errors, strictRelease);
            AppendFirebaseSdkChecks(warnings, errors, strictRelease);

            if (strictRelease && !PlayerSettings.Android.useCustomKeystore)
            {
                errors.Add("Release build requires a custom keystore (keystore must stay outside the repo).");
            }

            if (strictRelease && PlayerSettings.Android.useCustomKeystore
                && string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName))
            {
                errors.Add("Custom keystore enabled but keystore path is empty.");
            }

            if (strictRelease && EditorUserBuildSettings.development)
            {
                errors.Add("Development Build must be OFF for Release AAB.");
            }

            var log = new StringBuilder();
            log.AppendLine("[ReleaseBuildValidator]");

            for (int i = 0; i < warnings.Count; i++)
            {
                log.AppendLine("WARN: " + warnings[i]);
            }

            for (int i = 0; i < errors.Count; i++)
            {
                log.AppendLine("ERROR: " + errors[i]);
            }

            if (warnings.Count == 0 && errors.Count == 0)
            {
                log.AppendLine("OK: Release build validation passed.");
            }

            if (errors.Count > 0)
            {
                Debug.LogError(log.ToString());
                if (throwOnError)
                {
                    throw new BuildFailedException("Release build validation failed. See Console for details.");
                }

                EditorUtility.DisplayDialog("Release Validation Failed", log.ToString(), "확인");
                return;
            }

            Debug.Log(log.ToString());
            if (!throwOnError)
            {
                EditorUtility.DisplayDialog("Release Validation", log.ToString(), "확인");
            }
        }

        private static void ValidateGameCombatPools(List<string> errors)
        {
            const string gameScenePath = "Assets/Scenes/Game.unity";
            if (!File.Exists(gameScenePath))
            {
                return;
            }

            // 씬 YAML에 직렬화된 풀 프리팹이 비어 있으면 Editor AssetDatabase 폴백만 동작해
            // Release/AAB에서 웨이브 스폰이 통째로 깨진다.
            string[] lines = File.ReadAllLines(gameScenePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.IndexOf("LastTrain.Enemy.EnemyPool", StringComparison.Ordinal) < 0
                    && line.IndexOf("LastTrain.Battle.ProjectilePool", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                string poolName = line.Contains("EnemyPool") ? "EnemyPool" : "ProjectilePool";
                for (int j = i + 1; j < Math.Min(i + 8, lines.Length); j++)
                {
                    string candidate = lines[j].Trim();
                    if (candidate.StartsWith("prefab:", StringComparison.Ordinal))
                    {
                        if (candidate.Contains("{fileID: 0}") && !candidate.Contains("guid:"))
                        {
                            errors.Add(
                                $"Game.unity {poolName}.prefab is null — Release에서 웨이브가 시작되지 않습니다. " +
                                "BasicEnemy/BasicProjectile을 연결하세요.");
                        }

                        break;
                    }
                }
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Combat/BasicEnemy.prefab") == null)
            {
                errors.Add("Missing Resources/Combat/BasicEnemy.prefab (EnemyPool runtime fallback).");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Combat/BasicProjectile.prefab") == null)
            {
                errors.Add("Missing Resources/Combat/BasicProjectile.prefab (ProjectilePool runtime fallback).");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Combat/PassengerView.prefab") == null)
            {
                errors.Add("Missing Resources/Combat/PassengerView.prefab (GridManager runtime fallback).");
            }

            if (AssetDatabase.LoadAssetAtPath<SummonEconomyConfig>("Assets/Resources/SummonEconomyConfig.asset") == null)
            {
                errors.Add("Missing Resources/SummonEconomyConfig.asset — Release에서 소환 패널 DB 폴백 실패 가능.");
            }
        }

        private static void AppendAdMobSdkChecks(List<string> warnings, List<string> errors, bool strictRelease)
        {
            if (warnings == null || errors == null)
            {
                return;
            }

            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            bool hasDefine = !string.IsNullOrEmpty(defines)
                             && defines.Split(';').Any(d =>
                                 string.Equals(d.Trim(), "LASTTRAIN_ADMOB", StringComparison.Ordinal));

            bool packagePresent = false;
            foreach (UnityEditor.PackageManager.PackageInfo pkg in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (pkg != null && string.Equals(pkg.name, "com.google.ads.mobile", StringComparison.Ordinal))
                {
                    packagePresent = true;
                    break;
                }
            }

            bool settingsPresent = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                                       "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset") != null;

            if (!packagePresent)
            {
                warnings.Add(
                    "Google Mobile Ads 패키지(com.google.ads.mobile)가 없습니다. " +
                    "현재 Release는 NoOp로 플레이 가능합니다. Soft Launch 전 OpenUPM에 패키지를 넣고 App ID를 설정하세요.");
            }
            else if (!settingsPresent)
            {
                errors.Add(
                    "com.google.ads.mobile이 설치됐지만 GoogleMobileAdsSettings.asset이 없습니다. " +
                    "Android AAB가 Gradle 실패하거나 기동 크래시할 수 있습니다. App ID를 설정하거나 패키지를 제거하세요.");
            }

            if (hasDefine && !packagePresent)
            {
                errors.Add(
                    "LASTTRAIN_ADMOB가 켜져 있으나 AdMob 패키지가 없어 Release 컴파일이 실패합니다.");
            }
            else if (strictRelease && !hasDefine)
            {
                warnings.Add(
                    "LASTTRAIN_ADMOB define이 없습니다. Soft Launch 전 패키지·App ID 준비 후 Enable LASTTRAIN_ADMOB를 실행하세요.");
            }
        }

        private static void AppendFirebaseSdkChecks(List<string> warnings, List<string> errors, bool strictRelease)
        {
            if (warnings == null || errors == null)
            {
                return;
            }

            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            bool hasDefine = !string.IsNullOrEmpty(defines)
                             && defines.Split(';').Any(d =>
                                 string.Equals(d.Trim(), "LASTTRAIN_FIREBASE", StringComparison.Ordinal));

            bool hasGoogleServices = File.Exists("Assets/google-services.json")
                                     || File.Exists("Assets/Plugins/Android/google-services.json");
            bool hasFirebaseAsm = Type.GetType("Firebase.FirebaseApp, Firebase.App") != null
                                  || Directory.Exists("Assets/Firebase");

            if (!hasFirebaseAsm)
            {
                warnings.Add(
                    "Firebase Unity SDK가 없습니다. 현재 Release는 Debug/NoOp Analytics·Crashlytics와 RemoteConfigDefaults로 플레이 가능합니다.");
            }

            if (!hasGoogleServices)
            {
                warnings.Add(
                    "Assets/google-services.json이 없습니다. Soft Launch 전 Firebase Console에서 내려받아 배치하세요.");
            }

            if (hasDefine && !hasGoogleServices)
            {
                errors.Add(
                    "LASTTRAIN_FIREBASE가 켜져 있으나 google-services.json이 없습니다. Release AAB가 실패하거나 기동이 깨질 수 있습니다.");
            }
            else if (hasDefine && !hasFirebaseAsm)
            {
                errors.Add(
                    "LASTTRAIN_FIREBASE가 켜져 있으나 Firebase SDK가 없어 Release 컴파일이 실패합니다.");
            }
            else if (strictRelease && !hasDefine)
            {
                warnings.Add(
                    "LASTTRAIN_FIREBASE define이 없습니다. Soft Launch 전 SDK·google-services.json 준비 후 Enable LASTTRAIN_FIREBASE를 실행하세요.");
            }
        }
    }
}
