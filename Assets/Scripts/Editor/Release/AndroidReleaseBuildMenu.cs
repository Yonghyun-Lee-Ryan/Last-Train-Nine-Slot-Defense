using System.IO;
using System.Linq;
using LastTrain.Release;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class AndroidReleaseBuildMenu
    {
        private const string OutputFolder = "Builds/Android";

        [MenuItem("Tools/막차 생존/Release/Build Android App Bundle (Release)")]
        public static void BuildReleaseAppBundle()
        {
            if (!EditorUtility.DisplayDialog(
                    "Android Release AAB",
                    "Release App Bundle을 생성합니다.\n" +
                    "서명키는 Unity Player Settings에 미리 설정해야 합니다.",
                    "빌드",
                    "취소"))
            {
                return;
            }

            ReleaseAssetsBuilder.EnsureReleaseAssets();
            ReleaseConfigSync.SyncFromMenu();
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;

            ReleaseBuildValidator.Validate(strictRelease: true, throwOnError: true);

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
                locationPathName = Path.Combine(OutputFolder, "LastTrain.aab"),
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[AndroidReleaseBuildMenu] Build failed: {report.summary.result}");
                return;
            }

            EditorUtility.DisplayDialog(
                "Build Complete",
                $"AAB 생성 완료:\n{options.locationPathName}",
                "확인");
        }

        [MenuItem("Tools/막차 생존/Release/Build Android App Bundle (Development)")]
        public static void BuildDevelopmentAppBundle()
        {
            ReleaseAssetsBuilder.EnsureReleaseAssets();
            ReleaseConfigSync.SyncFromMenu();
            EditorUserBuildSettings.development = true;

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
