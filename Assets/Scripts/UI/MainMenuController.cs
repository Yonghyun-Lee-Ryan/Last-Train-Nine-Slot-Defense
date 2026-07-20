using LastTrain.Core;
using LastTrain.Run;
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

        private void Awake()
        {
            if (startButton == null)
            {
                Debug.LogError("[MainMenuController] startButton이 연결되지 않았습니다.", this);
                return;
            }

            startButton.onClick.AddListener(OnStartClicked);
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }
        }

        private void OnStartClicked()
        {
            startButton.interactable = false;

            // 새 회차로 시작한다. 이전 씬의 RunState가 남아 있어도 덮어쓴다.
            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null)
            {
                appRoot.GameSession.StartNewRun();
            }

            SceneFlow.Load(SceneNames.Game);
        }
    }
}
