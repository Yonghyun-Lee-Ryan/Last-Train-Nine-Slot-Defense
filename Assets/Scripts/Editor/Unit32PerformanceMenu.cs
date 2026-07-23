using LastTrain.Performance;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit32PerformanceMenu
    {
        [MenuItem("Tools/막차 생존/개발 단위 32 Profiler 체크리스트 출력")]
        public static void PrintChecklist()
        {
            string report = PerformanceChecklist.BuildReport();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Unit 32 Checklist", "Console에 Profiler 체크리스트를 출력했습니다.", "확인");
        }
    }
}
