using LastTrain.Data;
using LastTrain.Tutorial;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit30TutorialAssetsBuilder
    {
        [MenuItem("Tools/막차 생존/개발 단위 30 튜토리얼 단계 생성")]
        public static void Build()
        {
            // Unit 45에서 퀵스타트 5단계로 병합·단축. 동일 에셋을 재생성한다.
            Unit45FtueAssetsBuilder.Build();
        }
    }
}
