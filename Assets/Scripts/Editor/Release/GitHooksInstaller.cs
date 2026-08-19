using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>.git/hooks/pre-commit에 실 AdMob ID 차단 훅을 복사한다. git config는 변경하지 않는다.</summary>
    public static class GitHooksInstaller
    {
        [MenuItem("Tools/막차 생존/Release/Install Git Pre-commit Hook")]
        public static void InstallFromMenu()
        {
            if (TryInstallPreCommitHook())
            {
                EditorUtility.DisplayDialog(
                    "Git Hook",
                    "pre-commit 훅을 설치했습니다.\n운영 AdMob ID가 staged 파일에 있으면 커밋이 거부됩니다.",
                    "확인");
                return;
            }

            EditorUtility.DisplayDialog(
                "Git Hook",
                "훅을 설치하지 못했습니다. .githooks/pre-commit 과 .git/hooks 경로를 확인하세요.",
                "확인");
        }

        public static bool TryInstallPreCommitHook()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                return false;
            }

            string source = Path.Combine(projectRoot, ".githooks", "pre-commit");
            string gitDir = Path.Combine(projectRoot, ".git");
            if (!File.Exists(source) || !Directory.Exists(gitDir))
            {
                return false;
            }

            string hooksDir = Path.Combine(gitDir, "hooks");
            Directory.CreateDirectory(hooksDir);
            string dest = Path.Combine(hooksDir, "pre-commit");
            File.Copy(source, dest, overwrite: true);
            return true;
        }
    }
}
