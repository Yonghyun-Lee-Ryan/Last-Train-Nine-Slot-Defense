using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LastTrain.UI;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// 에디터 생성 툴 실행 전 기존 생성물을 정리한다.
    /// Resources.FindObjectsOfTypeAll을 사용해 비활성 오브젝트도 포함한다.
    /// </summary>
    public static class SceneBuilderCleanup
    {
        private const string AutoCleanupSessionKey = "LastTrain.SafeAreaDuplicatesCleaned";

        [InitializeOnLoadMethod]
        private static void ScheduleCurrentGameSceneCleanup()
        {
            if (SessionState.GetBool(AutoCleanupSessionKey, false))
            {
                return;
            }

            EditorApplication.delayCall += CleanupOpenGameSceneOnce;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem("Tools/막차 생존/중복 생성물 정리 (현재 Game Scene)")]
        public static void CleanupCurrentGameScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Game")
            {
                EditorUtility.DisplayDialog("오류", "Game Scene을 먼저 열어주세요.", "확인");
                return;
            }

            int removed = CleanupGeneratedDuplicates(scene);
            int migrated = MigrateLegacyAbilityPanelHierarchy(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog(
                "완료",
                $"SafeArea 중복 생성물 {removed}개를 정리했습니다.\nAbilityPanel 계층 마이그레이션 {migrated}건.",
                "확인");
        }

        public static T FindFirstInScene<T>(Scene scene) where T : Component
        {
            List<T> all = FindAllInScene<T>(scene);
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].gameObject.activeInHierarchy)
                {
                    return all[i];
                }
            }

            return all.Count > 0 ? all[0] : null;
        }

        public static List<T> FindAllInScene<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            T[] all = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < all.Length; i++)
            {
                T component = all[i];
                if (component == null
                    || EditorUtility.IsPersistent(component)
                    || component.gameObject.scene != scene)
                {
                    continue;
                }

                result.Add(component);
            }

            return result;
        }

        public static int DestroyAllComponents<T>(Scene scene) where T : Component
        {
            List<T> all = FindAllInScene<T>(scene);
            var roots = new HashSet<GameObject>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null)
                {
                    roots.Add(all[i].gameObject);
                }
            }

            foreach (GameObject root in roots)
            {
                Object.DestroyImmediate(root);
            }

            return roots.Count;
        }

        public static int DestroyAllNamed(Scene scene, string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return 0;
            }

            int removed = 0;
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = all.Length - 1; i >= 0; i--)
            {
                GameObject go = all[i];
                if (go == null
                    || EditorUtility.IsPersistent(go)
                    || go.scene != scene
                    || go.name != objectName)
                {
                    continue;
                }

                Object.DestroyImmediate(go);
                removed++;
            }

            return removed;
        }

        /// <summary>알려진 단일 생성 루트는 활성 오브젝트를 우선해 하나만 남긴다.</summary>
        public static int CleanupGeneratedDuplicates(Scene scene)
        {
            string[] uniqueRootNames =
            {
                "PassengerGrid",
                "BattleSystems",
                "GameBattleBootstrap",
                "SpawnPoint",
                "EnemyWaypoint0",
                "EnemyWaypoint1",
                "EnemyWaypoint2",
                "EnemyWaypoint3",
                "EnemyWaypoint4",
                "EnemyWaypoint5",
                "TrainTarget",
                "SpawnLaneDecor0",
                "SpawnLaneDecor1",
                "SpawnLaneDecor2",
                "SpawnLaneDecor3",
                "SpawnLaneDecorJoin0",
                "SpawnLaneDecorJoin1",
                "SpawnLaneDecorJoin2",
                "EnemyPathDirectionView",
                "PassengerRangeOverlay",
                "SummonPanel",
                "BattleHud",
                "SynergyHud",
                "SynergyListLabel",
                "BossHpRoot"
            };

            int removed = CleanupAbilityPanelDuplicates(scene);
            removed += MigrateLegacyAbilityPanelHierarchy(scene);
            for (int i = 0; i < uniqueRootNames.Length; i++)
            {
                removed += KeepSingleNamed(scene, uniqueRootNames[i]);
            }

            return removed;
        }

        /// <summary>
        /// SafeArea 직계 자식으로 남아 있는 owned label을 패널 아래로 옮기고,
        /// 선택 UI를 SelectionOverlay 자식으로 분리한다.
        /// </summary>
        public static int MigrateLegacyAbilityPanelHierarchy(Scene scene)
        {
            List<AbilityPanelController> panels = FindAllInScene<AbilityPanelController>(scene);
            if (panels.Count == 0)
            {
                return 0;
            }

            AbilityPanelController panel = panels[0];
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].gameObject.activeInHierarchy)
                {
                    panel = panels[i];
                    break;
                }
            }

            int migrated = 0;
            Transform panelTransform = panel.transform;
            SerializedObject serializedPanel = new SerializedObject(panel);
            SerializedProperty ownedProperty = serializedPanel.FindProperty("ownedListLabel");
            SerializedProperty rootProperty = serializedPanel.FindProperty("root");

            if (ownedProperty?.objectReferenceValue is Text ownedLabel
                && ownedLabel.transform.parent != panelTransform)
            {
                ownedLabel.transform.SetParent(panelTransform, false);
                migrated++;
            }

            Transform overlay = panelTransform.Find("SelectionOverlay");
            if (overlay != null)
            {
                if (rootProperty != null && rootProperty.objectReferenceValue == null)
                {
                    rootProperty.objectReferenceValue = overlay.gameObject;
                    serializedPanel.ApplyModifiedPropertiesWithoutUndo();
                    migrated++;
                }

                overlay.gameObject.SetActive(false);
                return migrated;
            }

            var overlayObject = new GameObject("SelectionOverlay", typeof(RectTransform), typeof(Image));
            overlay = overlayObject.transform;
            overlay.SetParent(panelTransform, false);
            StretchFull(overlayObject.GetComponent<RectTransform>());

            Image overlayImage = overlayObject.GetComponent<Image>();
            Image panelImage = panel.GetComponent<Image>();
            overlayImage.color = panelImage != null
                ? panelImage.color
                : new Color(0f, 0f, 0f, 0.72f);
            overlayObject.SetActive(false);

            if (panelImage != null)
            {
                Object.DestroyImmediate(panelImage);
            }

            var childrenToMove = new List<Transform>();
            for (int i = 0; i < panelTransform.childCount; i++)
            {
                Transform child = panelTransform.GetChild(i);
                if (child == overlay || child.name == "AbilityOwnedListLabel")
                {
                    continue;
                }

                childrenToMove.Add(child);
            }

            for (int i = 0; i < childrenToMove.Count; i++)
            {
                childrenToMove[i].SetParent(overlay, false);
            }

            if (rootProperty != null)
            {
                rootProperty.objectReferenceValue = overlayObject;
            }

            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
            return migrated + 1;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static int CleanupAbilityPanelDuplicates(Scene scene)
        {
            List<AbilityPanelController> panels = FindAllInScene<AbilityPanelController>(scene);
            if (panels.Count == 0)
            {
                return KeepSingleNamed(scene, "AbilityOwnedListLabel");
            }

            AbilityPanelController keep = panels[0];
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].gameObject.activeInHierarchy)
                {
                    keep = panels[i];
                    break;
                }
            }

            GameObject keepOwnedLabel = null;
            var serialized = new SerializedObject(keep);
            Object ownedReference = serialized.FindProperty("ownedListLabel")?.objectReferenceValue;
            if (ownedReference is Component ownedComponent)
            {
                keepOwnedLabel = ownedComponent.gameObject;
            }

            int removed = 0;
            for (int i = panels.Count - 1; i >= 0; i--)
            {
                AbilityPanelController panel = panels[i];
                if (panel == null || panel == keep)
                {
                    continue;
                }

                Object.DestroyImmediate(panel.gameObject);
                removed++;
            }

            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = all.Length - 1; i >= 0; i--)
            {
                GameObject go = all[i];
                if (go == null
                    || go == keepOwnedLabel
                    || EditorUtility.IsPersistent(go)
                    || go.scene != scene
                    || go.name != "AbilityOwnedListLabel")
                {
                    continue;
                }

                Object.DestroyImmediate(go);
                removed++;
            }

            return removed;
        }

        private static int KeepSingleNamed(Scene scene, string objectName)
        {
            var matches = new List<GameObject>();
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (go != null
                    && !EditorUtility.IsPersistent(go)
                    && go.scene == scene
                    && go.name == objectName)
                {
                    matches.Add(go);
                }
            }

            if (matches.Count <= 1)
            {
                return 0;
            }

            GameObject keep = matches[0];
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].activeInHierarchy)
                {
                    keep = matches[i];
                    break;
                }
            }

            int removed = 0;
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                GameObject go = matches[i];
                if (go == null || go == keep)
                {
                    continue;
                }

                Object.DestroyImmediate(go);
                removed++;
            }

            return removed;
        }

        private static void CleanupOpenGameSceneOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Game")
            {
                return;
            }

            SessionState.SetBool(AutoCleanupSessionKey, true);
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            int removed = CleanupGeneratedDuplicates(scene);
            if (removed <= 0)
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneBuilderCleanup] SafeArea 중복 생성물 {removed}개를 자동 정리했습니다.");
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += CleanupOpenGameSceneOnce;
            }
        }
    }
}
