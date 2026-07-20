using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Grid;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// Game Scene에 적 Pool, SpawnPoint, TrainTarget, 이동 전투 설정을 추가한다.
    /// </summary>
    public static class Unit6EnemySceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/BasicEnemy.prefab";
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 6 적 이동 시스템 추가 (Game Scene)")]
        public static void BuildEnemySystems()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 6 적 이동 시스템 추가",
                    "Game Scene에 EnemyPool, SpawnPoint, TrainTarget, 이동 전투 설정을 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            EnsurePrefabFolder();
            EnemyController enemyPrefab = LoadOrCreateEnemyPrefab();

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("오류", "Game Scene에서 Canvas를 찾지 못했습니다.", "확인");
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            BattleManager battleManager = Object.FindAnyObjectByType<BattleManager>();
            if (battleManager == null)
            {
                EditorUtility.DisplayDialog("오류", "BattleManager가 없습니다. 먼저 개발 단위 5를 적용하세요.", "확인");
                return;
            }

            GridManager gridManager = Object.FindAnyObjectByType<GridManager>();
            RectTransform spawnPoint = CreateMarker(parent, "SpawnPoint", new Vector2(540f, 1650f), new Color(0.2f, 0.9f, 0.4f, 0.5f));
            RectTransform trainTarget = CreateMarker(parent, "TrainTarget", new Vector2(540f, 260f), new Color(0.95f, 0.35f, 0.25f, 0.55f));

            EnemyPool enemyPool = SetupEnemyPool(battleManager.gameObject, enemyPrefab);
            WireBattleManager(battleManager, gridManager, enemyPool, spawnPoint, trainTarget);
            UpdateBattleBootstrap(battleManager, gridManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeObject = battleManager.gameObject;
            EditorUtility.DisplayDialog("완료", "Game Scene에 적 이동 시스템이 추가되었습니다.", "확인");
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }

                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");
            }
        }

        private static EnemyController LoadOrCreateEnemyPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyController>(EnemyPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("BasicEnemy", typeof(RectTransform), typeof(Image), typeof(EnemyController));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(72, 72);

            var image = root.GetComponent<Image>();
            image.color = new Color(0.85f, 0.25f, 0.25f, 1f);

            Text nameLabel = CreateChildText(root.transform, "NameLabel", "적", 18, new Vector2(0f, -48f));

            var controller = root.GetComponent<EnemyController>();
            var so = new SerializedObject(controller);
            so.FindProperty("bodyImage").objectReferenceValue = image;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            Object.DestroyImmediate(root);

            return AssetDatabase.LoadAssetAtPath<EnemyController>(EnemyPrefabPath);
        }

        private static RectTransform CreateMarker(Transform parent, string name, Vector2 anchoredPosition, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(40f, 40f);
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return rect;
        }

        private static EnemyPool SetupEnemyPool(GameObject battleRoot, EnemyController prefab)
        {
            EnemyPool pool = battleRoot.GetComponent<EnemyPool>();
            if (pool == null)
            {
                pool = battleRoot.AddComponent<EnemyPool>();
            }

            Transform poolRootTransform = battleRoot.transform.Find("EnemyPoolRoot");
            RectTransform poolRoot;
            if (poolRootTransform == null)
            {
                var poolGo = new GameObject("EnemyPoolRoot", typeof(RectTransform));
                poolRoot = poolGo.GetComponent<RectTransform>();
                poolRoot.SetParent(battleRoot.transform, false);
                poolRoot.anchorMin = Vector2.zero;
                poolRoot.anchorMax = Vector2.one;
                poolRoot.offsetMin = Vector2.zero;
                poolRoot.offsetMax = Vector2.zero;
            }
            else
            {
                poolRoot = poolRootTransform as RectTransform;
            }

            var poolSo = new SerializedObject(pool);
            poolSo.FindProperty("prefab").objectReferenceValue = prefab;
            poolSo.FindProperty("poolRoot").objectReferenceValue = poolRoot;
            poolSo.ApplyModifiedPropertiesWithoutUndo();
            return pool;
        }

        private static void WireBattleManager(
            BattleManager battleManager,
            GridManager gridManager,
            EnemyPool enemyPool,
            RectTransform spawnPoint,
            RectTransform trainTarget)
        {
            var so = new SerializedObject(battleManager);
            so.FindProperty("gridManager").objectReferenceValue = gridManager;
            so.FindProperty("enemyPool").objectReferenceValue = enemyPool;
            so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
            so.FindProperty("trainTarget").objectReferenceValue = trainTarget;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateBattleBootstrap(BattleManager battleManager, GridManager gridManager)
        {
            GameBattleBootstrap bootstrap = Object.FindAnyObjectByType<GameBattleBootstrap>();
            if (bootstrap == null)
            {
                var bootstrapGo = new GameObject("GameBattleBootstrap");
                bootstrapGo.transform.SetParent(battleManager.transform.parent, false);
                bootstrap = bootstrapGo.AddComponent<GameBattleBootstrap>();
            }

            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            IReadOnlyList<EnemyData> enemies = db != null ? db.Enemies : null;

            var so = new SerializedObject(bootstrap);
            so.FindProperty("battleManager").objectReferenceValue = battleManager;
            so.FindProperty("gridManager").objectReferenceValue = gridManager;

            SerializedProperty spawnEnemies = so.FindProperty("debugSpawnEnemies");
            spawnEnemies.arraySize = enemies != null && enemies.Count > 0 ? Mathf.Min(3, enemies.Count) : 0;
            for (int i = 0; i < spawnEnemies.arraySize; i++)
            {
                spawnEnemies.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            }

            so.FindProperty("spawnInterval").floatValue = 2.5f;
            so.FindProperty("maxConcurrentEnemies").intValue = 6;
            so.FindProperty("autoSpawn").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateChildText(Transform parent, string name, string text, int fontSize, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(160f, 36f);

            var label = go.GetComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return label;
        }
    }
}
