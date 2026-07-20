using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// Game Scene에 BattleManager, ProjectilePool, GameBattleBootstrap를 추가한다.
    /// </summary>
    public static class Unit5BattleSceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectiles/BasicProjectile.prefab";
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 5 전투 시스템 추가 (Game Scene)")]
        public static void BuildBattleSystems()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 5 전투 시스템 추가",
                    "Game Scene에 BattleManager, ProjectilePool, GameBattleBootstrap를 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            EnsurePrefabFolder();
            ProjectileController projectilePrefab = LoadOrCreateProjectilePrefab();

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("오류", "Game Scene에서 Canvas를 찾지 못했습니다.", "확인");
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            BattleManager existingBattle = Object.FindAnyObjectByType<BattleManager>();
            if (existingBattle != null)
            {
                Object.DestroyImmediate(existingBattle.gameObject);
            }

            GridManager gridManager = Object.FindAnyObjectByType<GridManager>();
            (BattleManager battleManager, ProjectilePool pool) = CreateBattleHierarchy(parent, canvas, projectilePrefab, gridManager);
            AddBattleBootstrap(battleManager, gridManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeObject = battleManager.gameObject;
            EditorUtility.DisplayDialog("완료", "Game Scene에 전투 시스템이 추가되었습니다.", "확인");
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Projectiles"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }

                AssetDatabase.CreateFolder("Assets/Prefabs", "Projectiles");
            }
        }

        private static ProjectileController LoadOrCreateProjectilePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ProjectileController>(ProjectilePrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("BasicProjectile", typeof(RectTransform), typeof(Image), typeof(ProjectileController));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(20, 20);

            var image = root.GetComponent<Image>();
            image.color = new Color(1f, 0.85f, 0.2f, 1f);

            PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            Object.DestroyImmediate(root);

            return AssetDatabase.LoadAssetAtPath<ProjectileController>(ProjectilePrefabPath);
        }

        private static (BattleManager, ProjectilePool) CreateBattleHierarchy(
            Transform parent,
            Canvas canvas,
            ProjectileController projectilePrefab,
            GridManager gridManager)
        {
            var battleGo = new GameObject("BattleSystems", typeof(RectTransform), typeof(BattleManager), typeof(ProjectilePool));
            var battleRect = battleGo.GetComponent<RectTransform>();
            battleRect.SetParent(parent, false);
            battleRect.anchorMin = Vector2.zero;
            battleRect.anchorMax = Vector2.one;
            battleRect.offsetMin = Vector2.zero;
            battleRect.offsetMax = Vector2.zero;

            var poolGo = new GameObject("ProjectilePoolRoot", typeof(RectTransform));
            var poolRect = poolGo.GetComponent<RectTransform>();
            poolRect.SetParent(battleGo.transform, false);
            poolRect.anchorMin = Vector2.zero;
            poolRect.anchorMax = Vector2.one;
            poolRect.offsetMin = Vector2.zero;
            poolRect.offsetMax = Vector2.zero;

            var pool = battleGo.GetComponent<ProjectilePool>();
            var battleManager = battleGo.GetComponent<BattleManager>();

            var poolSo = new SerializedObject(pool);
            poolSo.FindProperty("prefab").objectReferenceValue = projectilePrefab;
            poolSo.FindProperty("poolRoot").objectReferenceValue = poolRect;
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            var battleSo = new SerializedObject(battleManager);
            battleSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            battleSo.FindProperty("projectilePool").objectReferenceValue = pool;
            battleSo.ApplyModifiedPropertiesWithoutUndo();

            return (battleManager, pool);
        }

        private static void AddBattleBootstrap(BattleManager battleManager, GridManager gridManager)
        {
            GameBattleBootstrap existing = Object.FindAnyObjectByType<GameBattleBootstrap>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var bootstrapGo = new GameObject("GameBattleBootstrap");
            bootstrapGo.transform.SetParent(battleManager.transform.parent, false);
            var bootstrap = bootstrapGo.AddComponent<GameBattleBootstrap>();

            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);

            var bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("battleManager").objectReferenceValue = battleManager;
            bootstrapSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            bootstrapSo.FindProperty("gameDatabase").objectReferenceValue = db;
            bootstrapSo.FindProperty("autoStartFirstWave").boolValue = true;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
