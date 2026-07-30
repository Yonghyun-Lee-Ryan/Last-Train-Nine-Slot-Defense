using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class AndroidReleaseBuildMenu
    {
        private const string OutputFolder = "Builds/Android";

        [MenuItem("Tools/막차 생존/Release/Prepare For Play Internal Test", priority = 10)]
        public static void PrepareForPlayInternalTest()
        {
            ReleaseAssetsBuilder.EnsureReleaseAssets();
            ReleaseConfigSync.ApplyToPlayerSettings(ReleaseConfigSync.LoadOrCreateConfig());
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildAppBundle = true;
            AssetDatabase.SaveAssets();
            ReleaseBuildValidator.Validate(strictRelease: true, throwOnError: false);
        }

        [MenuItem("Tools/막차 생존/Release/Build Android App Bundle (Release)", priority = 20)]
        public static void BuildReleaseAppBundle()
        {
            if (!EditorUtility.DisplayDialog(
                    "Android Release AAB",
                    "Release App Bundle을 생성합니다.\n" +
                    "서명 비밀번호는 Player Settings에 미리 입력되어 있어야 합니다.\n" +
                    "비밀번호 입력+버전업이 필요하면:\n" +
                    "Tools → 막차 생존 → Release → 서명·버전업 후 Release AAB 빌드\n\n" +
                    "출력: Builds/Android/",
                    "빌드",
                    "취소"))
            {
                return;
            }

            try
            {
                ReleaseAssetsBuilder.EnsureReleaseAssets();
                ReleaseConfigSync.ApplyToPlayerSettings(ReleaseConfigSync.LoadOrCreateConfig());
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.buildAppBundle = true;

                ReleaseBuildValidator.Validate(strictRelease: true, throwOnError: true);
                string path = BuildReleaseAppBundleInternal(PlayerSettings.Android.bundleVersionCode);
                EditorUtility.DisplayDialog(
                    "Build Complete",
                    $"AAB 생성 완료:\n{path}\n\nPlay Console → 내부 테스트 트랙에 업로드하세요.",
                    "확인");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Build Failed", ex.Message, "확인");
            }
        }

        /// <summary>서명·버전 준비 후 호출. 성공 시 AAB 경로 반환.</summary>
        public static string BuildReleaseAppBundleInternal(int bundleVersionCode)
        {
            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new System.InvalidOperationException("Build Settings에 활성화된 씬이 없습니다.");
            }

            string version = PlayerSettings.bundleVersion.Replace('.', '_');
            int code = Mathf.Max(1, bundleVersionCode);
            string outputName = $"LastTrain-v{version}-b{code}.aab";
            string outputPath = Path.Combine(OutputFolder, outputName).Replace('\\', '/');

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                string detail = ExtractGradleSigningHint();
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(detail)
                        ? $"AAB 빌드 실패: {report.summary.result}"
                        : $"AAB 빌드 실패: {report.summary.result}\n\n{detail}");
            }

            string aliasPath = Path.Combine(OutputFolder, "LastTrain.aab").Replace('\\', '/');
            File.Copy(outputPath, aliasPath, overwrite: true);
            Debug.Log($"[AndroidReleaseBuildMenu] AAB OK: {outputPath}");
            return outputPath;
        }

        private static string ExtractGradleSigningHint()
        {
            string logPath = Path.Combine(
                "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/logs",
                "unity-bundleRelease-build.log");
            if (!File.Exists(logPath))
            {
                return string.Empty;
            }

            try
            {
                string text = File.ReadAllText(logPath);
                if (text.IndexOf("keystore password was incorrect", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "원인: 키스토어 비밀번호가 올바르지 않습니다.";
                }

                if (text.IndexOf("Cannot recover key", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "원인: Key Alias 비밀번호가 올바르지 않거나 Alias가 다릅니다.";
                }

                const string marker = "* What went wrong:";
                int idx = text.LastIndexOf(marker, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    int end = Math.Min(text.Length, idx + 400);
                    return text.Substring(idx, end - idx).Trim();
                }
            }
            catch
            {
                // ignore log read failures
            }

            return string.Empty;
        }

        [MenuItem("Tools/막차 생존/Release/Build Android App Bundle (Development)", priority = 30)]
        public static void BuildDevelopmentAppBundle()
        {
            ReleaseAssetsBuilder.EnsureReleaseAssets();
            ReleaseConfigSync.ApplyToPlayerSettings(ReleaseConfigSync.LoadOrCreateConfig());
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.buildAppBundle = true;

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputFolder, "LastTrain-Dev.aab"),
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[AndroidReleaseBuildMenu] Dev build result: {report.summary.result}");
        }
    }
}
