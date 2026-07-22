using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Integrations;
using LastTrain.Release;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>Release 빌드 최초 실행 시 광고·분석 동의를 받는다.</summary>
    public sealed class PrivacyConsentDialogController : MonoBehaviour
    {
        private GameObject _root;

        public void TryShowIfNeeded()
        {
            AppRoot appRoot = AppRoot.Instance;
            PrivacyConsentService privacy = appRoot?.Privacy;
            if (privacy == null || !privacy.NeedsConsentPrompt() || _root != null)
            {
                return;
            }

            AppReleaseConfig config = AppReleaseConfigLocator.Load();
            VisualTheme theme = VisualThemeLocator.Load();
            _root = MenuOverlayUi.CreateRoot("PrivacyConsentDialog", sortingOrder: 4500);
            DontDestroyOnLoad(_root);

            GameObject dim = MenuOverlayUi.CreatePanel(_root.transform, "Dim", new Color(0f, 0f, 0f, 0.82f));
            MenuOverlayUi.Stretch(dim.GetComponent<RectTransform>());
            if (theme?.PopupDim != null)
            {
                Image dimImage = dim.GetComponent<Image>();
                dimImage.sprite = theme.PopupDim;
                dimImage.type = Image.Type.Sliced;
                dimImage.color = Color.white;
            }

            GameObject box = MenuOverlayUi.CreatePanel(_root.transform, "Box", new Color(0.12f, 0.16f, 0.22f, 0.98f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(900, 760);
            boxRect.anchoredPosition = Vector2.zero;
            if (theme?.Panel != null)
            {
                Image boxImage = box.GetComponent<Image>();
                boxImage.sprite = theme.Panel;
                boxImage.type = Image.Type.Sliced;
                boxImage.color = Color.white;
            }

            Text title = MenuOverlayUi.CreateText(box.transform, "Title", "개인정보 및 광고 안내", 40, TextAnchor.MiddleCenter);
            title.rectTransform.anchoredPosition = new Vector2(0f, 300f);
            title.rectTransform.sizeDelta = new Vector2(820, 80);

            string body =
                "맞춤형 광고와 게임 개선을 위한 분석 데이터 수집에 동의할 수 있습니다.\n" +
                "동의하지 않아도 게임을 플레이할 수 있습니다.\n\n" +
                config.DataDeletionNotice;
            Text bodyText = MenuOverlayUi.CreateText(box.transform, "Body", body, 26, TextAnchor.UpperCenter);
            bodyText.rectTransform.anchoredPosition = new Vector2(0f, 40f);
            bodyText.rectTransform.sizeDelta = new Vector2(820, 320);

            MenuOverlayUi.CreateButton(box.transform, "Accept", "동의하고 계속", new Vector2(0f, -220f), new Vector2(760, 96), () =>
            {
                appRoot.ApplyPrivacyConsent(adsGranted: true, analyticsGranted: true);
                Close();
            });

            MenuOverlayUi.CreateButton(box.transform, "Decline", "동의하지 않음", new Vector2(0f, -330f), new Vector2(760, 96), () =>
            {
                appRoot.ApplyPrivacyConsent(adsGranted: false, analyticsGranted: false);
                Close();
            });

            MenuOverlayUi.CreateButton(box.transform, "Policy", "개인정보처리방침 보기", new Vector2(0f, -440f), new Vector2(760, 80), () =>
            {
                if (!string.IsNullOrWhiteSpace(config.PrivacyPolicyUrl))
                {
                    Application.OpenURL(config.PrivacyPolicyUrl);
                }
            });
        }

        private void Close()
        {
            if (_root == null)
            {
                return;
            }

            Destroy(_root);
            _root = null;
        }

        private void OnDestroy()
        {
            Close();
        }
    }
}
