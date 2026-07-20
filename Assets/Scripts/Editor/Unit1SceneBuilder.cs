using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// 개발 단위 1의 Scene 4종(Bootstrap, MainMenu, Game, Result)을 자동 생성한다.
    /// Scene 파일은 수동 편집이 위험하므로 Unity가 직접 생성하도록 한다.
    /// 각 Scene에 Canvas, Safe Area, EventSystem, 버튼, 컨트롤러를 배치하고
    /// Build Settings에 순서대로 등록한다.
    ///
    /// 사용법: 상단 메뉴 Tools > 막차 생존 > 개발 단위 1 Scene 생성
    /// </summary>
    public static class Unit1SceneBuilder
    {
        private const string SceneFolder = "Assets/Scenes";
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        [MenuItem("Tools/막차 생존/개발 단위 1 Scene 생성")]
        public static void BuildAllScenes()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 1 Scene 생성",
                    "Bootstrap, MainMenu, Game, Result Scene을 생성하고 Build Settings에 등록합니다.\n" +
                    "동일 이름 Scene이 있으면 덮어씁니다. 계속할까요?",
                    "생성", "취소"))
            {
                return;
            }

            EnsureFolder();

            string bootstrapPath = BuildBootstrapScene();
            string mainMenuPath = BuildMainMenuScene();
            string gamePath = BuildGameScene();
            string resultPath = BuildResultScene();

            RegisterBuildSettings(new[] { bootstrapPath, mainMenuPath, gamePath, resultPath });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "완료",
                "4개 Scene 생성 및 Build Settings 등록이 완료되었습니다.\n" +
                "Bootstrap Scene을 열고 Play를 눌러 흐름을 확인하세요.",
                "확인");

            EditorSceneManager.OpenScene(bootstrapPath, OpenSceneMode.Single);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }

        private static string BuildBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var appRootGo = new GameObject("AppRoot");
            appRootGo.AddComponent<AppRoot>();
            appRootGo.AddComponent<SceneLoader>();

            SceneManager.MoveGameObjectToScene(appRootGo, scene);

            string path = $"{SceneFolder}/{SceneNames.Bootstrap}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            (Canvas canvas, RectTransform safeArea) = CreateCanvasWithSafeArea();
            CreateEventSystem();

            CreateTitleLabel(safeArea, "막차 생존: 9칸 디펜스", 64, new Vector2(0, 500));

            Button startButton = CreateButton(safeArea, "StartButton", "게임 시작", new Vector2(0, -100));
            var controller = canvas.gameObject.AddComponent<MainMenuController>();
            AssignPrivateField(controller, "startButton", startButton);

            string path = $"{SceneFolder}/{SceneNames.MainMenu}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            (Canvas canvas, RectTransform safeArea) = CreateCanvasWithSafeArea();
            CreateEventSystem();

            CreateTitleLabel(safeArea, "GAME (임시)", 56, new Vector2(0, 500));

            Button exitButton = CreateButton(safeArea, "ExitToResultButton", "임시 종료 → 결과", new Vector2(0, 750));
            // 상단 우측으로 배치해 Grid와 겹치지 않게 한다.
            var exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(1f, 1f);
            exitRect.anchorMax = new Vector2(1f, 1f);
            exitRect.pivot = new Vector2(1f, 1f);
            exitRect.anchoredPosition = new Vector2(-24f, -24f);
            exitRect.sizeDelta = new Vector2(320f, 100f);
            var controller = canvas.gameObject.AddComponent<GamePlaceholderController>();
            AssignPrivateField(controller, "exitToResultButton", exitButton);

            string path = $"{SceneFolder}/{SceneNames.Game}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildResultScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            (Canvas canvas, RectTransform safeArea) = CreateCanvasWithSafeArea();
            CreateEventSystem();

            CreateTitleLabel(safeArea, "RESULT (임시)", 56, new Vector2(0, 500));

            Button retryButton = CreateButton(safeArea, "RetryButton", "다시 시작", new Vector2(0, 40));
            Button menuButton = CreateButton(safeArea, "MainMenuButton", "메인 메뉴", new Vector2(0, -160));

            var controller = canvas.gameObject.AddComponent<ResultPlaceholderController>();
            AssignPrivateField(controller, "retryButton", retryButton);
            AssignPrivateField(controller, "mainMenuButton", menuButton);

            string path = $"{SceneFolder}/{SceneNames.Result}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void CreateCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f);
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0, 0, -10);
        }

        private static (Canvas, RectTransform) CreateCanvasWithSafeArea()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var safeAreaGo = new GameObject("SafeArea", typeof(RectTransform));
            var safeAreaRect = safeAreaGo.GetComponent<RectTransform>();
            safeAreaRect.SetParent(canvasGo.transform, false);
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
            safeAreaGo.AddComponent<SafeAreaFitter>();

            return (canvas, safeAreaRect);
        }

        private static void CreateEventSystem()
        {
            var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));

            // 프로젝트가 New Input System 전용일 수도, Old Input Manager일 수도 있으므로
            // 컴파일 의존성을 만들지 않고 사용 가능한 InputModule을 찾아 붙인다.
            System.Type moduleType =
                System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (moduleType == null)
            {
                moduleType = typeof(UnityEngine.EventSystems.StandaloneInputModule);
            }

            esGo.AddComponent(moduleType);
        }

        private static void CreateTitleLabel(RectTransform parent, string text, int fontSize, Vector2 anchoredPos)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(900, 200);
            rect.anchoredPosition = anchoredPos;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.font = GetBuiltinFont();
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(600, 160);
            rect.anchoredPosition = anchoredPos;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.20f, 0.45f, 0.85f);

            var button = go.GetComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(go.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 44;
            text.color = Color.white;
            text.font = GetBuiltinFont();

            return button;
        }

        /// <summary>
        /// Unity 6에서는 Arial.ttf·LegacyRuntime.ttf 내장 리소스 API가 변경되었다.
        /// 예외 없이 OS 폰트 폴백까지 시도한다.
        /// </summary>
        private static Font GetBuiltinFont()
        {
            Font font = TryLoadBuiltinFont("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            string[] osFontCandidates =
            {
                "Malgun Gothic",
                "Segoe UI",
                "Arial",
                "Helvetica",
                "Noto Sans CJK KR"
            };

            for (int i = 0; i < osFontCandidates.Length; i++)
            {
                font = Font.CreateDynamicFontFromOSFont(osFontCandidates[i], 16);
                if (font != null)
                {
                    return font;
                }
            }

            Debug.LogWarning("[Unit1SceneBuilder] 사용 가능한 폰트를 찾지 못했습니다. Text는 기본 폰트로 표시됩니다.");
            return null;
        }

        private static Font TryLoadBuiltinFont(string resourceName)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(resourceName);
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        private static void AssignPrivateField(Object target, string fieldName, Object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                Debug.LogError($"[Unit1SceneBuilder] '{fieldName}' 필드를 {target.GetType().Name}에서 찾지 못했습니다.");
                return;
            }

            field.SetValue(target, value);
        }

        private static void RegisterBuildSettings(IReadOnlyList<string> scenePaths)
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in scenePaths)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
