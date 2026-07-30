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
                    warnings.Add("Privacy policy URL is still a placeholder (Play Console 데이터 안전/정책에 실제 URL 필요).");
                }
            }

            AdUnitConfig ads = AssetDatabase.LoadAssetAtPath<AdUnitConfig>("Assets/Data/Integration/AdUnitConfig.asset");
            if (strictRelease && ads != null)
            {
                // Production ID empty면 테스트 ID fallback — 내부테스트는 허용, 경고만
                string rewarded = ads.GetRewardedUnitId(useTestIds: false);
                if (rewarded.Contains("3940256099942544"))
                {
                    warnings.Add("Release AdUnit이 Google 테스트 ID입니다. 내부테스트 가능, 프로덕션 전 AdMob에 운영 ID를 넣으세요.");
                }
            }

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
    }
}
