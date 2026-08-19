using LastTrain.Analytics;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Save;
using LastTrain.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Tutorial
{
    /// <summary>스킵 직후 첫 전투에서 Summon/Ready 위치를 1–2회만 안내한다.</summary>
    public sealed class PostSkipGuideDirector : MonoBehaviour
    {
        private GameObject _overlayRoot;
        private Text _bodyLabel;
        private Button _ackButton;
        private int _tipIndex = -1;
        private bool _active;
        private Image _highlightImage;
        private Color? _highlightOriginal;

        public bool IsActive => _active;
        public int CurrentTipIndex => _tipIndex;

        public void TryBegin()
        {
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (!PostSkipGuideService.ShouldShow(meta))
            {
                return;
            }

            if (PostSkipGuideService.Tips.Length == 0)
            {
                Finish(meta);
                return;
            }

            _active = true;
            _tipIndex = 0;
            EnsureOverlay();
            ShowCurrentTip();
            AppRoot.Instance?.Analytics?.Track(
                AnalyticsEventNames.TutorialPostSkipGuideShown,
                new System.Collections.Generic.Dictionary<string, object>
                {
                    ["tip_count"] = PostSkipGuideService.Tips.Length,
                });
        }

        private void EnsureOverlay()
        {
            if (_overlayRoot != null)
            {
                return;
            }

            _overlayRoot = MenuOverlayUi.CreateRoot("PostSkipGuideOverlay", sortingOrder: 4400);
            CanvasGroup rootGroup = _overlayRoot.GetComponent<CanvasGroup>();
            if (rootGroup == null)
            {
                rootGroup = _overlayRoot.AddComponent<CanvasGroup>();
            }

            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = true;

            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_overlayRoot.transform);
            GameObject box = MenuOverlayUi.CreatePanel(host, "Box", new Color(0.08f, 0.14f, 0.2f, 0.92f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.08f, 0.78f);
            boxRect.anchorMax = new Vector2(0.92f, 0.94f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;
            CanvasGroup boxGroup = box.AddComponent<CanvasGroup>();
            boxGroup.blocksRaycasts = true;
            boxGroup.interactable = true;
            boxGroup.ignoreParentGroups = true;

            _bodyLabel = MenuOverlayUi.CreateText(box.transform, "Body", string.Empty, 28, TextAnchor.MiddleCenter);
            MenuOverlayUi.Stretch(_bodyLabel.rectTransform);
            _bodyLabel.rectTransform.offsetMin = new Vector2(20f, 70f);
            _bodyLabel.rectTransform.offsetMax = new Vector2(-20f, -16f);

            _ackButton = MenuOverlayUi.CreateButton(
                box.transform,
                "AckButton",
                "확인",
                new Vector2(0f, 24f),
                new Vector2(240f, 64f),
                OnAckClicked);
            RectTransform ackRect = _ackButton.GetComponent<RectTransform>();
            ackRect.anchorMin = new Vector2(0.5f, 0f);
            ackRect.anchorMax = new Vector2(0.5f, 0f);
            ackRect.pivot = new Vector2(0.5f, 0f);
        }

        private void ShowCurrentTip()
        {
            if (!_active || _tipIndex < 0 || _tipIndex >= PostSkipGuideService.Tips.Length)
            {
                return;
            }

            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(true);
                GameFontProvider.ApplyTo(_overlayRoot);
            }

            PostSkipGuideService.Tip tip = PostSkipGuideService.Tips[_tipIndex];
            if (_bodyLabel != null)
            {
                _bodyLabel.text = tip.Message;
            }

            Highlight(tip.UiTargetId);
            Ux.UxGuidanceService.Show(tip.Message);
        }

        private void OnAckClicked()
        {
            GameAudio.PlaySfx(SfxId.UiConfirm);
            Advance();
        }

        private void Advance()
        {
            RestoreHighlight();
            _tipIndex++;
            if (_tipIndex >= PostSkipGuideService.Tips.Length)
            {
                Finish(MetaSaveSystem.LoadOrCreate());
                return;
            }

            ShowCurrentTip();
        }

        private void Finish(MetaSaveData meta)
        {
            PostSkipGuideService.MarkDone(meta);
            MetaSaveSystem.Save(meta);
            _active = false;
            RestoreHighlight();
            if (_overlayRoot != null)
            {
                GameObject root = _overlayRoot;
                _overlayRoot = null;
                MenuOverlayUi.DestroyRoot(root);
            }

            AppRoot.Instance?.Analytics?.Track(
                AnalyticsEventNames.TutorialPostSkipGuideCompleted,
                new System.Collections.Generic.Dictionary<string, object>
                {
                    ["tip_count"] = PostSkipGuideService.Tips.Length,
                });
        }

        private void Highlight(string targetId)
        {
            RestoreHighlight();
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return;
            }

            GameObject target = GameObject.Find(targetId);
            if (target == null)
            {
                return;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            _highlightImage = image;
            _highlightOriginal = image.color;
            image.color = new Color(1f, 0.92f, 0.55f, 1f);
        }

        private void RestoreHighlight()
        {
            if (_highlightImage != null && _highlightOriginal.HasValue)
            {
                _highlightImage.color = _highlightOriginal.Value;
            }

            _highlightImage = null;
            _highlightOriginal = null;
        }

        private void OnDestroy()
        {
            RestoreHighlight();
            if (_overlayRoot != null)
            {
                GameObject root = _overlayRoot;
                _overlayRoot = null;
                MenuOverlayUi.DestroyRoot(root);
            }
        }
    }
}
