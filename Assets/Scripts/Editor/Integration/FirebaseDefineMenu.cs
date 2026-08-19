using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 36: google-services.json / Firebase 어셈블리 확인 후 LASTTRAIN_FIREBASE define 토글.</summary>
    public static class FirebaseDefineMenu
    {
        private const string Define = "LASTTRAIN_FIREBASE";

        [MenuItem("Tools/막차 생존/Integration/Enable LASTTRAIN_FIREBASE")]
        public static void EnableDefine()
        {
            if (!HasGoogleServicesJson())
            {
                EditorUtility.DisplayDialog(
                    "Firebase Define",
                    "Assets/google-services.json 이 없습니다.\n" +
                    "Firebase Console에서 Android 앱 설정 파일을 받은 뒤 Enable 하세요.\n" +
                    "(미설정 상태로 define만 켜면 Release 빌드가 깨질 수 있습니다.)",
                    "확인");
                return;
            }

            if (!HasFirebaseAssemblies())
            {
                EditorUtility.DisplayDialog(
                    "Firebase Define",
                    "Firebase Unity SDK가 보이지 않습니다.\n" +
                    "Docs/ANDROID_INTEGRATION_SETUP.md 의 Firebase Import 절차를 먼저 진행하세요.",
                    "확인");
                return;
            }

            if (SetDefine(NamedBuildTarget.Android, enabled: true)
                | SetDefine(NamedBuildTarget.Standalone, enabled: true))
            {
                Debug.Log("[FirebaseDefineMenu] Enabled " + Define);
            }
            else
            {
                Debug.Log("[FirebaseDefineMenu] " + Define + " already enabled.");
            }
        }

        [MenuItem("Tools/막차 생존/Integration/Disable LASTTRAIN_FIREBASE")]
        public static void DisableDefine()
        {
            if (SetDefine(NamedBuildTarget.Android, enabled: false)
                | SetDefine(NamedBuildTarget.Standalone, enabled: false))
            {
                Debug.Log("[FirebaseDefineMenu] Disabled " + Define);
            }
        }

        private static bool HasGoogleServicesJson()
        {
            return File.Exists("Assets/google-services.json")
                   || File.Exists("Assets/Plugins/Android/google-services.json");
        }

        private static bool HasFirebaseAssemblies()
        {
            if (Type.GetType("Firebase.FirebaseApp, Firebase.App") != null)
            {
                return true;
            }

            if (Directory.Exists("Assets/Firebase"))
            {
                return true;
            }

            try
            {
                if (!Directory.Exists("Library/PackageCache"))
                {
                    return false;
                }

                foreach (string dir in Directory.GetDirectories("Library/PackageCache"))
                {
                    string name = Path.GetFileName(dir);
                    if (name != null && name.StartsWith("com.google.firebase.", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
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
