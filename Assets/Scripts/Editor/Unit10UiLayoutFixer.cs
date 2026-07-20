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

            Transform safeArea = Object.FindAnyObjectByType<Canvas>()?.transform.Find("SafeArea")
                                 ?? Object.FindAnyObjectByType<Canvas>()?.transform;
            if (safeArea == null)
            {
                EditorUtility.DisplayDialog("오류", "SafeArea/Canvas를 찾지 못했습니다.", "확인");
                return;
            }

            // Grid: 하단 액션바(소환/전투) 위
            GridManager grid = Object.FindAnyObjectByType<GridManager>();
            if (grid != null)
            {
                var gridRect = grid.GetComponent<RectTransform>();
                gridRect.anchorMin = new Vector2(0.5f, 0f);
                gridRect.anchorMax = new Vector2(0.5f, 0f);
                gridRect.pivot = new Vector2(0.5f, 0f);
                gridRect.anchoredPosition = new Vector2(0f, 340f);
                gridRect.sizeDelta = new Vector2(860f, 860f);

                var layout = grid.GetComponent<GridLayoutGroup>();
                if (layout != null)
                {
                    layout.cellSize = new Vector2(270f, 270f);
                    layout.spacing = new Vector2(10f, 10f);
                }
            }

            // 하단 SummonPanel
            SummonPanelController summon = Object.FindAnyObjectByType<SummonPanelController>();
            if (summon != null)
            {
                var panelRect = summon.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(1f, 0f);
                panelRect.pivot = new Vector2(0.5f, 0f);
                panelRect.anchoredPosition = new Vector2(0f, 8f);
                panelRect.sizeDelta = new Vector2(0f, 200f);

                PlaceBottom(panelRect, "CoinLabel", new Vector2(-320f, 140f), new Vector2(280f, 40f));
                PlaceBottom(panelRect, "CostLabel", new Vector2(320f, 140f), new Vector2(280f, 40f));
                PlaceBottom(panelRect, "StatusLabel", new Vector2(0f, 140f), new Vector2(400f, 40f));
                PlaceBottom(panelRect, "SummonButton", new Vector2(-220f, 45f), new Vector2(200f, 80f));
                PlaceBottom(panelRect, "SellButton", new Vector2(220f, 45f), new Vector2(200f, 80f));

                Transform sell = panelRect.Find("SellButton");
                if (sell != null)
                {
                    sell.gameObject.SetActive(true);
                    Text sellLabel = sell.GetComponentInChildren<Text>();
                    if (sellLabel != null)
                    {
                        sellLabel.text = "판매하기";
                    }
                }
            }

            // 전투 버튼: SummonPanel 위, Grid 아래
            BattleHudController hud = Object.FindAnyObjectByType<BattleHudController>();
            if (hud != null)
            {
                var hudRect = hud.GetComponent<RectTransform>();
                PlaceBottom(hudRect, "ReadyButton", new Vector2(-300f, 230f), new Vector2(200f, 80f));
                PlaceBottom(hudRect, "SpeedButton", new Vector2(-100f, 230f), new Vector2(110f, 80f));
                PlaceBottom(hudRect, "PauseButton", new Vector2(100f, 230f), new Vector2(130f, 80f));

                PlaceCenter(hudRect, "TrainHpLabel", new Vector2(-300f, 880f), new Vector2(340f, 40f));
                PlaceCenter(hudRect, "TrainHpSlider", new Vector2(-300f, 835f), new Vector2(340f, 24f));
                PlaceCenter(hudRect, "CoinLabel", new Vector2(300f, 880f), new Vector2(260f, 40f));
                PlaceCenter(hudRect, "StationLabel", new Vector2(-300f, 780f), new Vector2(260f, 36f));
                PlaceCenter(hudRect, "WaveLabel", new Vector2(0f, 780f), new Vector2(260f, 36f));
                PlaceCenter(hudRect, "PhaseLabel", new Vector2(300f, 780f), new Vector2(260f, 36f));
                PlaceCenter(hudRect, "StatusLabel", new Vector2(0f, 720f), new Vector2(900f, 36f));
            }

            // 임시 타이틀 숨김
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
            EditorUtility.DisplayDialog("완료", "UI를 하단 액션바 / 상단 HUD로 재배치하고 판매 버튼을 복구했습니다.", "확인");
        }

        private static void PlaceBottom(Transform parent, string childName, Vector2 anchoredPos, Vector2 size)
        {
            Transform child = parent.Find(childName);
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

        private static void PlaceCenter(Transform parent, string childName, Vector2 anchoredPos, Vector2 size)
        {
            Transform child = parent.Find(childName);
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
    }
}
