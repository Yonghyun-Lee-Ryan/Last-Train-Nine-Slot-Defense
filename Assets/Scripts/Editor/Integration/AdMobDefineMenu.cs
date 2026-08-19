using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 35: AdMob SDK 패키지 확인 후 LASTTRAIN_ADMOB scripting define 토글.</summary>
    public static class AdMobDefineMenu
    {
        private const string Define = "LASTTRAIN_ADMOB";
        private const string PackageName = "com.google.ads.mobile";

        [MenuItem("Tools/막차 생존/Integration/Enable LASTTRAIN_ADMOB")]
        public static void EnableDefine()
        {
            if (!IsPackageInstalled())
            {
                EditorUtility.DisplayDialog(
                    "AdMob Define",
                    "com.google.ads.mobile 패키지가 없습니다.\n" +
                    "Packages/manifest.json OpenUPM 항목을 확인한 뒤 Unity가 패키지를 resolve할 때까지 기다리세요.",
                    "확인");
                return;
            }

            if (SetDefine(NamedBuildTarget.Android, enabled: true)
                | SetDefine(NamedBuildTarget.Standalone, enabled: true))
            {
                Debug.Log("[AdMobDefineMenu] Enabled " + Define);
            }
            else
            {
                Debug.Log("[AdMobDefineMenu] " + Define + " already enabled.");
            }
        }

        [MenuItem("Tools/막차 생존/Integration/Disable LASTTRAIN_ADMOB")]
        public static void DisableDefine()
        {
            if (SetDefine(NamedBuildTarget.Android, enabled: false)
                | SetDefine(NamedBuildTarget.Standalone, enabled: false))
            {
                Debug.Log("[AdMobDefineMenu] Disabled " + Define);
            }
        }

        private static bool IsPackageInstalled()
        {
            foreach (var pkg in PackageInfo.GetAllRegisteredPackages())
            {
                if (pkg != null && string.Equals(pkg.name, PackageName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SetDefine(NamedBuildTarget target, bool enabled)
        {
            string raw = PlayerSettings.GetScriptingDefineSymbols(target);
            var list = string.IsNullOrWhiteSpace(raw)
                ? new System.Collections.Generic.List<string>()
                : raw.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            bool has = list.Any(d => string.Equals(d, Define, StringComparison.Ordinal));
            if (enabled && !has)
            {
                list.Add(Define);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
                return true;
            }

            if (!enabled && has)
            {
                list.RemoveAll(d => string.Equals(d, Define, StringComparison.Ordinal));
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
                return true;
            }

            return false;
        }
    }
}
