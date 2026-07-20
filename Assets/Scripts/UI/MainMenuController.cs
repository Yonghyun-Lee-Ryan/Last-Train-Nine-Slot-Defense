using LastTrain.Core;
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
            SceneFlow.Load(SceneNames.Game);
        }
    }
}
