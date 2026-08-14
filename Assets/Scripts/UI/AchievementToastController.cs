using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>업적 해금 토스트. Result 씬에서 즉시 피드백을 보여준다.</summary>
    public sealed class AchievementToastController : MonoBehaviour
    {
        private GameObject _root;
        private Text _label;

        public bool IsShowing => _root != null && _root.activeSelf;

        public void ShowUnlocks(System.Collections.Generic.IReadOnlyList<string> achievementIds)
        {
            if (achievementIds == null || achievementIds.Count == 0)
            {
                return;
            }

            EnsureRoot();
            string first = achievementIds[0];
            string name = AchievementCatalog.GetDisplayNameOrId(first);
            int extra = achievementIds.Count - 1;
            _label.text = extra > 0
                ? $"업적 해금! {name} 외 {extra}개"
                : $"업적 해금! {name}";
            _root.SetActive(true);
            CancelInvoke(nameof(Hide));
            if (Application.isPlaying)
            {
                Invoke(nameof(Hide), 2.4f);
            }
        }

        public void ShowMessage(string message, float duration = 2.2f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnsureRoot();
            _label.text = message;
            _root.SetActive(true);
            CancelInvoke(nameof(Hide));
            if (Application.isPlaying)
            {
                Invoke(nameof(Hide), duration);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = MenuOverlayUi.CreateRoot("AchievementToast", sortingOrder: 5200);
            GameObject box = MenuOverlayUi.CreatePanel(_root.transform, "Box", new Color(0.12f, 0.18f, 0.12f, 0.94f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.1f, 0.935f);
            boxRect.anchorMax = new Vector2(0.9f, 0.985f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;

            _label = MenuOverlayUi.CreateText(box.transform, "Label", string.Empty, 28, TextAnchor.MiddleCenter);
            MenuOverlayUi.Stretch(_label.rectTransform);
            _label.rectTransform.offsetMin = new Vector2(16f, 8f);
            _label.rectTransform.offsetMax = new Vector2(-16f, -8f);
        }

        private void OnDestroy()
        {
            CancelInvoke();
            if (_root != null)
            {
                Destroy(_root);
                _root = null;
            }
        }
    }
}
