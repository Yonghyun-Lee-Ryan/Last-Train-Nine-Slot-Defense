using System.IO;
using LastTrain.Balance;
using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit54SoftLaunchQaMenu
    {
        [MenuItem("Tools/막차 생존/개발 단위 54 Soft Launch QA 게이트")]
        public static void RunFromMenu()
        {
            SoftLaunchGateResult result = RunInternal();
            EditorUtility.DisplayDialog(
                "Unit 54 Soft Launch QA",
                result.Passed ? "게이트 통과\n" + result.Markdown : "게이트 실패\n" + result.Markdown,
                "확인");
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit54SoftLaunchQaMenu.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                SoftLaunchGateResult result = RunInternal();
                Debug.Log("[Unit54SoftLaunchQaMenu] " + (result.Passed ? "OK" : "FAIL") + "\n" + result.Markdown);
                if (!result.Passed)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit54SoftLaunchQaMenu] " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static SoftLaunchGateResult RunInternal()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            SoftLaunchGateResult result = SoftLaunchBalanceGate.Evaluate(database);
            string dir = Path.Combine(Application.dataPath, "../BalanceReports");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "soft_launch_gate.md"), result.Markdown);
            string recordPath = Path.Combine(Application.dataPath, "../Docs/SOFT_LAUNCH_QA_RECORD.md");
            File.WriteAllText(recordPath, result.Markdown + "\n기록일: 2026-08-14 (Unity 6000.5.4f1 EditMode)\n");
            return result;
        }
    }
}
