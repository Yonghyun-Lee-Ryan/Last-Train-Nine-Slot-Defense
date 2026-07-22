using System.Collections.Generic;
using System.IO;
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

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            {
                errors.Add("Android scripting backend must be IL2CPP.");
            }

            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                errors.Add("Android target architecture must be ARM64 only.");
            }

            if (!EditorUserBuildSettings.buildAppBundle)
            {
                errors.Add("EditorUserBuildSettings.buildAppBundle must be enabled.");
            }

            if (!PlayerSettings.Android.splitApplicationBinary)
            {
                errors.Add("Android App Bundle (splitApplicationBinary) must be enabled.");
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
                    warnings.Add("Privacy policy URL is still a placeholder.");
                }
            }

            if (strictRelease && !PlayerSettings.Android.useCustomKeystore)
            {
                errors.Add("Release build requires a custom keystore (keystore must stay outside the repo).");
            }

            if (strictRelease && EditorUserBuildSettings.development)
            {
                warnings.Add("Development Build is enabled.");
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
    }
}
