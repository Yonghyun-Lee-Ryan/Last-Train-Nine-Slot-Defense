using LastTrain.Battle;
using LastTrain.Grid;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// Game Scene UI가 Grid와 겹치지 않도록 하단 액션바 / 상단 HUD로 재배치한다.
    /// </summary>
    public static class Unit10UiLayoutFixer
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/막차 생존/개발 단위 10 UI 겹침 수정 (Game Scene)")]
        public static void FixLayout()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            SceneBuilderCleanup.CleanupGeneratedDuplicates(scene);

            Canvas canvas = SceneBuilderCleanup.FindFirstInScene<Canvas>(scene);
            Transform safeArea = canvas?.transform.Find("SafeArea") ?? canvas?.transform;
            if (safeArea == null)
            {
                EditorUtility.DisplayDialog("오류", "SafeArea/Canvas를 찾지 못했습니다.", "확인");
                return;
            }

            GridManager grid = SceneBuilderCleanup.FindFirstInScene<GridManager>(scene);
            if (grid != null)
            {
                var gridRect = grid.GetComponent<RectTransform>();
                gridRect.anchorMin = new Vector2(0.5f, 0f);
                gridRect.anchorMax = new Vector2(0.5f, 0f);
                gridRect.pivot = new Vector2(0.5f, 0f);
                gridRect.anchoredPosition = BattleConstants.GridAnchoredPosition;
                gridRect.sizeDelta = BattleConstants.GridSize;

                var layout = grid.GetComponent<GridLayoutGroup>();
                if (layout != null)
                {
                    layout.cellSize = BattleConstants.GridCellSize;
                    layout.spacing = BattleConstants.GridSpacing;
                }
            }

            SummonPanelController summon =
                SceneBuilderCleanup.FindFirstInScene<SummonPanelController>(scene);
            if (summon != null)
            {
                var panelRect = summon.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(1f, 0f);
                panelRect.pivot = new Vector2(0.5f, 0f);
                panelRect.anchoredPosition = new Vector2(0f, 10f);
                panelRect.sizeDelta = new Vector2(0f, 200f);

                PlaceBottom(panelRect, "CoinLabel", new Vector2(-320f, 140f), new Vector2(280f, 40f));
                PlaceBottom(panelRect, "CostLabel", new Vector2(320f, 140f), new Vector2(280f, 40f));
                PlaceBottom(panelRect, "StatusLabel", new Vector2(0f, 140f), new Vector2(400f, 40f));
                PlaceBottom(panelRect, "SummonButton", new Vector2(0f, 45f), new Vector2(240f, 80f));

                Transform sell = panelRect.Find("SellButton");
                if (sell != null)
                {
                    Object.DestroyImmediate(sell.gameObject);
                }
            }

            BattleHudController hud =
                SceneBuilderCleanup.FindFirstInScene<BattleHudController>(scene);
            if (hud != null)
            {
                var hudRect = hud.GetComponent<RectTransform>();
                PlaceBottom(hudRect, "ReadyButton", new Vector2(-190f, 220f), new Vector2(160f, 78f));
                PlaceBottom(hudRect, "SpeedButton", new Vector2(0f, 220f), new Vector2(160f, 78f));
                PlaceBottom(hudRect, "PauseButton", new Vector2(190f, 220f), new Vector2(160f, 78f));

                PlaceTop(hudRect, "TrainHpLabel", new Vector2(-300f, -40f), new Vector2(360f, 40f));
                PlaceTop(hudRect, "TrainHpSlider", new Vector2(-300f, -78f), new Vector2(360f, 24f));
                PlaceTop(hudRect, "CoinLabel", new Vector2(300f, -40f), new Vector2(280f, 40f));
                PlaceTop(hudRect, "StationLabel", new Vector2(-300f, -120f), new Vector2(280f, 36f));
                PlaceTop(hudRect, "WaveLabel", new Vector2(0f, -120f), new Vector2(280f, 36f));
                PlaceTop(hudRect, "PhaseLabel", new Vector2(300f, -120f), new Vector2(280f, 36f));
                PlaceTop(hudRect, "StatusLabel", new Vector2(0f, -165f), new Vector2(900f, 36f));
                PlaceTop(hudRect, "BossHpRoot", new Vector2(0f, -255f), new Vector2(720f, 90f));
            }

            PlaceCenter(safeArea, "SpawnPoint", BattleConstants.SpawnAnchoredPosition, new Vector2(40f, 40f));
            for (int i = 0; i < BattleConstants.EnemyWaypointAnchoredPositions.Length; i++)
            {
                PlaceCenter(
                    safeArea,
                    $"EnemyWaypoint{i}",
                    BattleConstants.EnemyWaypointAnchoredPositions[i],
                    new Vector2(40f, 40f));
            }

            PlaceCenter(safeArea, "TrainTarget", BattleConstants.TrainTargetAnchoredPosition, new Vector2(160f, 140f));
            for (int i = 0; i < BattleConstants.LegacyEnemyLaneDecorNames.Length; i++)
            {
                Transform legacy = FindDeepChild(safeArea, BattleConstants.LegacyEnemyLaneDecorNames[i]);
                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy.gameObject);
                }
            }

            for (int i = 0; i < BattleConstants.EnemyLaneDecors.Length; i++)
            {
                BattleConstants.LaneDecorSpec decor = BattleConstants.EnemyLaneDecors[i];
                PlaceCenter(safeArea, decor.Name, decor.AnchoredPosition, decor.Size);
            }

            var safeRect = safeArea as RectTransform;

            EnemyPathDirectionView.Ensure(safeRect);
            PassengerRangeOverlay.Ensure(safeRect);
            PlaceTopLeft(
                safeArea,
                "SynergyListLabel",
                new Vector2(CombatTopHudLayout.SynergyLeftX, CombatTopHudLayout.SynergyTopNoThreat),
                new Vector2(CombatTopHudLayout.SynergyWidth, CombatTopHudLayout.SynergyMaxHeight));

            foreach (Transform child in safeArea)
            {
                Text text = child.GetComponent<Text>();
                if (text != null && text.text != null && text.text.Contains("GAME"))
                {
                    child.gameObject.SetActive(false);
                    break;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog(
                "완료",
                "UI 버튼·적 지그재그 경로·방향 화살표·사거리 오버레이를 재배치했습니다.",
                "확인");
        }

        private static void PlaceBottom(Transform parent, string childName, Vector2 anchoredPos, Vector2 size)
        {
            Transform child = FindDeepChild(parent, childName);
            if (child == null)
            {
                return;
            }

            var rect = child as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static void PlaceTopLeft(Transform parent, string childName, Vector2 anchoredPos, Vector2 size)
        {
            Transform child = FindDeepChild(parent, childName);
            if (child == null)
            {
                return;
            }

            var rect = child as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static void PlaceTop(Transform parent, string childName, Vector2 anchoredPos, Vector2 size)
        {
            Transform child = FindDeepChild(parent, childName);
            if (child == null)
            {
                return;
            }

            var rect = child as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static void PlaceCenter(Transform parent, string childName, Vector2 anchoredPos, Vector2 size)
        {
            Transform child = FindDeepChild(parent, childName);
            if (child == null)
            {
                return;
            }

            var rect = child as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            Transform direct = parent.Find(childName);
            if (direct != null)
            {
                return direct;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
