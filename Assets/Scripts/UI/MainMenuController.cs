using LastTrain.Core;
using LastTrain.Run;
using LastTrain.Data;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// MainMenu Scene의 임시 컨트롤러.
    /// 게임 시작 버튼을 눌러 Game Scene으로 이동한다.
    /// 개발 단위 1 범위의 최소 구현이며, 이후 메뉴 UI로 대체된다.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;

        private void Awake()
        {
            if (startButton == null)
            {
                Debug.LogError("[MainMenuController] startButton이 연결되지 않았습니다.", this);
                return;
            }

            startButton.onClick.AddListener(OnStartClicked);

            EnsureContinueButton();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        private void EnsureContinueButton()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
                continueButton.onClick.AddListener(OnContinueClicked);
                RefreshContinueButton();
                return;
            }

            // 기존 씬에 버튼이 이미 존재하면 그걸 사용
            var found = GameObject.Find("ContinueButton");
            if (found != null)
            {
                continueButton = found.GetComponent<Button>();
                if (continueButton != null)
                {
                    continueButton.onClick.AddListener(OnContinueClicked);
                    RefreshContinueButton();
                    return;
                }
            }

            // 없으면 SafeArea 아래에 최소 UI를 런타임 생성
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            GameObject go = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 160);
            rect.anchoredPosition = new Vector2(0, -260);

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.20f, 0.45f, 0.85f);

            continueButton = go.GetComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGo.AddComponent<Text>();
            text.text = "이어하기";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 44;
            text.color = Color.white;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                text.font = font;
            }

            continueButton.onClick.AddListener(OnContinueClicked);
            RefreshContinueButton();
        }

        private void OnStartClicked()
        {
            startButton.interactable = false;

            // 새 게임 시작 시 이어하기 저장은 제거한다.
            RunSaveSystem.DeleteRunSave();

            // 새 회차로 시작한다. 이전 씬의 RunState가 남아 있어도 덮어쓴다.
            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null)
            {
                appRoot.GameSession.StartNewRun();
            }

            SceneFlow.Load(SceneNames.Game);
        }

        private void OnContinueClicked()
        {
            if (continueButton != null)
            {
                continueButton.interactable = false;
            }

            if (!RunSaveSystem.TryLoadPreparing(out RunSaveData save) || save == null)
            {
                RefreshContinueButton();
                return;
            }

            GameDatabase gameDatabase = GameDatabaseLocator.Load();
            if (gameDatabase == null)
            {
                Debug.LogError("[MainMenuController] GameDatabase를 로드하지 못했습니다.");
                if (continueButton != null)
                {
                    continueButton.interactable = true;
                }

                return;
            }

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null)
            {
                return;
            }

            var config = RunSaveMapper.CreateStartConfigFromSave(save);
            appRoot.GameSession.StartNewRun(config);
            RunSaveMapper.ApplyToRunState(appRoot.GameSession.RunState, save, gameDatabase);

            SceneFlow.Load(SceneNames.Game);
        }

        private void RefreshContinueButton()
        {
            if (continueButton == null)
            {
                return;
            }

            bool hasSave = RunSaveSystem.TryLoadPreparing(out _);
            continueButton.interactable = hasSave;
        }
    }
}
