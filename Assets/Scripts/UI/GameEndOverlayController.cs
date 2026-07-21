using System.Collections;
using LastTrain.Core;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>회차 종료 시 성공/실패 메시지를 잠시 보여준 뒤 Result Scene으로 전환한다.</summary>
    public sealed class GameEndOverlayController : MonoBehaviour
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text messageLabel;
        [SerializeField] private float displaySeconds = 1.2f;

        private Coroutine _transitionRoutine;

        private void Awake()
        {
            EnsureUi();
            SetVisible(false);
        }

        public void Show(RunResult result)
        {
            EnsureUi();
            if (overlayRoot == null)
            {
                SceneFlow.Load(SceneNames.Result);
                return;
            }

            if (messageLabel != null)
            {
                messageLabel.text = RunResultFormatter.GetOverlayMessage(result);
            }

            SetVisible(true);

            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
            }

            _transitionRoutine = StartCoroutine(TransitionToResult());
        }

        private IEnumerator TransitionToResult()
        {
            float wait = Mathf.Max(0.1f, displaySeconds);
            yield return new WaitForSecondsRealtime(wait);
            SceneFlow.Load(SceneNames.Result);
        }

        private void SetVisible(bool visible)
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(visible);
            }
        }

        private void EnsureUi()
        {
            if (overlayRoot != null && messageLabel != null)
            {
                return;
            }

            var root = new GameObject("GameEndOverlay", typeof(RectTransform), typeof(Image));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            overlayRoot = root;

            var labelGo = new GameObject("MessageLabel", typeof(RectTransform), typeof(Text));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(root.transform, false);
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(900f, 180f);

            messageLabel = labelGo.GetComponent<Text>();
            messageLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            messageLabel.fontSize = 48;
            messageLabel.alignment = TextAnchor.MiddleCenter;
            messageLabel.color = Color.white;
            messageLabel.text = string.Empty;
        }
    }
}
