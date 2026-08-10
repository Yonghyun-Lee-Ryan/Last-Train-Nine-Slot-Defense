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

            GameObject box = MenuOverlayUi.CreatePanel(host, "Box", new Color(0.12f, 0.16f, 0.22f, 0.98f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.06f, 0.12f);
            boxRect.anchorMax = new Vector2(0.94f, 0.88f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;
            if (theme?.Panel != null)
            {
                Image boxImage = box.GetComponent<Image>();
                boxImage.sprite = theme.Panel;
                boxImage.type = Image.Type.Sliced;
                boxImage.color = Color.white;
            }

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.SetParent(box.transform, false);
            MenuOverlayUi.Stretch(content);
            content.offsetMin = new Vector2(28f, 28f);
            content.offsetMax = new Vector2(-28f, -28f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 14f;
            layout.padding = new RectOffset(8, 8, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text title = MenuOverlayUi.CreateText(
                content,
                "Title",
                "개인정보 및 광고 안내",
                40,
                TextAnchor.MiddleCenter);
            UiLayoutUtility.EnsureLayoutElement(title.gameObject, 56f);
            UiLayoutUtility.ResetForVerticalLayout(title.rectTransform, 56f);

            string body =
                "맞춤형 광고와 게임 개선을 위한 분석 데이터 수집에 동의할 수 있습니다.\n" +
                "동의하지 않아도 게임을 플레이할 수 있습니다.\n\n" +
                config.DataDeletionNotice;
            Text bodyText = MenuOverlayUi.CreateText(content, "Body", body, 26, TextAnchor.UpperCenter);
            LayoutElement bodyLayout = UiLayoutUtility.EnsureLayoutElement(bodyText.gameObject, 200f);
            bodyLayout.flexibleHeight = 1f;
            bodyLayout.minHeight = 140f;
            UiLayoutUtility.ResetForVerticalLayout(bodyText.rectTransform, 200f);

            MenuOverlayUi.CreateLayoutButton(content, "Accept", "동의하고 계속", 80f, () =>
            {
                appRoot.ApplyPrivacyConsent(adsGranted: true, analyticsGranted: true);
                Close();
            });

            MenuOverlayUi.CreateLayoutButton(content, "Decline", "동의하지 않음", 80f, () =>
            {
                appRoot.ApplyPrivacyConsent(adsGranted: false, analyticsGranted: false);
                Close();
            });

            MenuOverlayUi.CreateLayoutButton(content, "Policy", "개인정보처리방침 보기", 72f, () =>
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
