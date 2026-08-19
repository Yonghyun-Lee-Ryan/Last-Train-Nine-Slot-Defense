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

            GameObject dim = MenuOverlayUi.CreateFullScreenDim(_root.transform, new Color(0f, 0f, 0f, 0.82f));
            if (theme?.PopupDim != null)
            {
                Image dimImage = dim.GetComponent<Image>();
                dimImage.sprite = theme.PopupDim;
                dimImage.type = Image.Type.Sliced;
                dimImage.color = Color.white;
            }

            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_root.transform);

            GameObject box = MenuOverlayUi.CreateOverlayBox(host, MenuOverlayUi.OverlaySizeCompact);
            MenuOverlayUi.CreateOverlayTitle(box.transform, "개인정보 및 광고 안내");

            const float footerHeight = 280f;
            RectTransform footer = MenuOverlayUi.PinOverlayFooter(box.transform, footerHeight);
            var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            footerLayout.spacing = 8f;
            footerLayout.padding = new RectOffset(4, 4, 4, 4);
            footerLayout.childAlignment = TextAnchor.LowerCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;

            MenuOverlayUi.CreateLayoutButton(
                footer,
                "Accept",
                "동의하고 계속",
                80f,
                () =>
                {
                    appRoot.ApplyPrivacyConsent(adsGranted: true, analyticsGranted: true);
                    Close();
                },
                30,
                UiButtonStyler.OverlayActionWidth);

            MenuOverlayUi.CreateLayoutButton(
                footer,
                "Decline",
                "동의하지 않음",
                80f,
                () =>
                {
                    appRoot.ApplyPrivacyConsent(adsGranted: false, analyticsGranted: false);
                    Close();
                },
                30,
                UiButtonStyler.OverlayActionWidth);

            MenuOverlayUi.CreateLayoutButton(
                footer,
                "Policy",
                "개인정보처리방침 보기",
                64f,
                () =>
                {
                    if (!string.IsNullOrWhiteSpace(config.PrivacyPolicyUrl))
                    {
                        Application.OpenURL(config.PrivacyPolicyUrl);
                    }
                },
                26,
                UiButtonStyler.OverlayActionWidth);

            MenuOverlayUi.OverlayScroll scroll = MenuOverlayUi.CreateOverlayScroll(
                box.transform,
                extraBottom: footerHeight - MenuOverlayUi.OverlayCloseHeight);
            Transform content = scroll.Content;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 14f;
            layout.padding = new RectOffset(8, 8, 12, 12);

            string body =
                "맞춤형 광고와 게임 개선을 위한 분석 데이터 수집에 동의할 수 있습니다.\n" +
                "동의하지 않아도 게임을 플레이할 수 있습니다.\n\n" +
                config.DataDeletionNotice;
            Text bodyText = MenuOverlayUi.CreateText(content, "Body", body, 26, TextAnchor.UpperCenter);
            LayoutElement bodyLayout = UiLayoutUtility.EnsureLayoutElement(bodyText.gameObject, 200f);
            bodyLayout.flexibleHeight = 1f;
            bodyLayout.minHeight = 140f;
            UiLayoutUtility.ResetForVerticalLayout(bodyText.rectTransform, 200f);
        }

        private void Close()
        {
            Dismiss(notifyAttendance: true);
        }

        private void OnDestroy()
        {
            Dismiss(notifyAttendance: false);
        }

        private void Dismiss(bool notifyAttendance)
        {
            if (_root == null)
            {
                return;
            }

            GameObject root = _root;
            _root = null;
            MenuOverlayUi.DestroyRoot(root);
            if (!notifyAttendance)
            {
                return;
            }

            MainMenuController menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            if (menu != null && menu.isActiveAndEnabled)
            {
                menu.TryShowQueuedAttendance();
            }
        }
    }
}
