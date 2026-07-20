using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// Game Scene의 GameBattleBootstrap에 GameDatabase와 웨이브 진행 설정을 연결한다.
    /// </summary>
    public static class Unit7WaveSceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 7 웨이브 시스템 연결 (Game Scene)")]
        public static void WireWaveSystems()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 7 웨이브 시스템 연결",
                    "GameBattleBootstrap에 GameDatabase를 연결하고 웨이브 자동 시작을 설정합니다.\n계속할까요?",
                    "연결",
                    "취소"))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            BattleManager battleManager = Object.FindAnyObjectByType<BattleManager>();
            GridManager gridManager = Object.FindAnyObjectByType<GridManager>();
            GameBattleBootstrap bootstrap = Object.FindAnyObjectByType<GameBattleBootstrap>();

            if (battleManager == null || gridManager == null)
            {
                EditorUtility.DisplayDialog("오류", "BattleManager 또는 GridManager가 없습니다. 단위 5·6을 먼저 적용하세요.", "확인");
                return;
            }

            if (bootstrap == null)
            {
                var bootstrapGo = new GameObject("GameBattleBootstrap");
                bootstrapGo.transform.SetParent(battleManager.transform.parent, false);
                bootstrap = bootstrapGo.AddComponent<GameBattleBootstrap>();
            }

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            var so = new SerializedObject(bootstrap);
            so.FindProperty("battleManager").objectReferenceValue = battleManager;
            so.FindProperty("gridManager").objectReferenceValue = gridManager;
            so.FindProperty("gameDatabase").objectReferenceValue = database;
            so.FindProperty("autoStartFirstWave").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeObject = bootstrap.gameObject;
            EditorUtility.DisplayDialog("완료", "Game Scene에 웨이브 시스템이 연결되었습니다.", "확인");
        }
    }
}
