using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Integrations;
using LastTrain.Release;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>사운드·진동·알림·동의·데이터 삭제 설정 패널.</summary>
    public sealed class SettingsPanelController : MonoBehaviour
    {
        private GameObject _root;
        private GameSettingsService _settings;
        private PrivacyConsentService _privacy;
        private Text _bgmVolumeLabel;
        private Text _sfxVolumeLabel;

        public bool IsOpen => _root != null;

        public void Show()
        {
            if (_root != null)
            {
                return;
            }

            AppRoot appRoot = AppRoot.Instance;
            _settings = appRoot?.GameSettings;
            _privacy = appRoot?.Privacy;
            AppReleaseConfig config = AppReleaseConfigLocator.Load();
            VisualTheme theme = VisualThemeLocator.Load();

            GameAudio.PlaySfx(SfxId.UiOpen);

            _root = MenuOverlayUi.CreateRoot("SettingsPanel", sortingOrder: 4200);

            GameObject dim = MenuOverlayUi.CreatePanel(_root.transform, "Dim", new Color(0f, 0f, 0f, 0.72f));
            MenuOverlayUi.Stretch(dim.GetComponent<RectTransform>());
            if (theme?.PopupDim != null)
            {
                Image dimImage = dim.GetComponent<Image>();
                dimImage.sprite = theme.PopupDim;
                dimImage.type = Image.Type.Sliced;
                dimImage.color = Color.white;
            }

            Button dimButton = dim.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Hide);

            GameObject box = MenuOverlayUi.CreatePanel(_root.transform, "Box", new Color(0.12f, 0.16f, 0.22f, 0.98f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.08f, 0.06f);
            boxRect.anchorMax = new Vector2(0.92f, 0.94f);
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
            content.offsetMin = new Vector2(36f, 36f);
            content.offsetMax = new Vector2(-36f, -36f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text title = MenuOverlayUi.CreateText(content, "Title", "설정", 40, TextAnchor.MiddleCenter);
            UiLayoutUtility.EnsureLayoutElement(title.gameObject, 64f);
            UiLayoutUtility.ResetForVerticalLayout(title.rectTransform, 64f);

            AddFlexibleSpacer(content, "SpacerTop", 0.25f);

            if (_settings != null)
            {
                AddToggleRow(content, "배경음", _settings.BgmEnabled, enabled =>
                {
                    _settings.SetBgmEnabled(enabled);
                    _settings.Persist();
                    GameAudio.PlaySfx(SfxId.UiToggle);
                });
                _bgmVolumeLabel = AddVolumeSlider(content, "배경음 볼륨", _settings.BgmVolume, v =>
                {
                    _settings.SetBgmVolume(v);
                    RefreshVolumeLabels();
                });

                AddToggleRow(content, "효과음", _settings.SfxEnabled, enabled =>
                {
                    _settings.SetSfxEnabled(enabled);
                    _settings.Persist();
                    GameAudio.PlaySfx(SfxId.UiToggle);
                });
                _sfxVolumeLabel = AddVolumeSlider(content, "효과음 볼륨", _settings.SfxVolume, v =>
                {
                    _settings.SetSfxVolume(v);
                    RefreshVolumeLabels();
                    GameAudio.PlaySfx(SfxId.UiClick);
                });

                AddToggleRow(content, "진동", _settings.VibrationEnabled, enabled =>
                {
                    _settings.SetVibrationEnabled(enabled);
                    _settings.Persist();
                });
                AddToggleRow(content, "알림", _settings.NotificationsEnabled, enabled =>
                {
                    _settings.SetNotificationsEnabled(enabled);
                    _settings.Persist();
                });
                RefreshVolumeLabels();
            }

            if (_privacy != null)
            {
                AddToggleRow(content, "맞춤형 광고", _privacy.HasAdsConsent, granted =>
                {
                    appRoot.ApplyPrivacyConsent(granted, _privacy.HasAnalyticsConsent);
                });
                AddToggleRow(content, "게임 분석", _privacy.HasAnalyticsConsent, granted =>
                {
                    appRoot.ApplyPrivacyConsent(_privacy.HasAdsConsent, granted);
                });
            }

            AddFlexibleSpacer(content, "SpacerMid", 0.45f);

            AddActionButton(content, "Policy", "개인정보처리방침", () =>
            {
                if (!string.IsNullOrWhiteSpace(config.PrivacyPolicyUrl))
                {
                    Application.OpenURL(config.PrivacyPolicyUrl);
                }
            });

            AddActionButton(content, "DeleteData", "앱 데이터 삭제", () =>
            {
                PlayerDataDeletionService.DeleteAllLocalData(_privacy, _settings);
                appRoot.ApplyPrivacyConsent(adsGranted: false, analyticsGranted: false);
                Hide();
            });

            AddActionButton(content, "Close", "닫기", Hide);
            AddFlexibleSpacer(content, "SpacerBottom", 0.2f);
            UiLayoutUtility.ForceRebuild(content);
        }

        private void RefreshVolumeLabels()
        {
            if (_settings == null)
            {
                return;
            }

            if (_bgmVolumeLabel != null)
            {
                _bgmVolumeLabel.text = $"배경음 볼륨  {Mathf.RoundToInt(_settings.BgmVolume * 100f)}%";
            }

            if (_sfxVolumeLabel != null)
            {
                _sfxVolumeLabel.text = $"효과음 볼륨  {Mathf.RoundToInt(_settings.SfxVolume * 100f)}%";
            }
        }

        private static void AddFlexibleSpacer(Transform parent, string name, float flexible)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 0f;
            layout.preferredHeight = 0f;
            layout.flexibleHeight = flexible;
            layout.flexibleWidth = 1f;
        }

        private static void AddToggleRow(
            Transform parent,
            string label,
            bool value,
            System.Action<bool> onChanged)
        {
            Toggle toggle = MenuOverlayUi.CreateLayoutToggle(parent, label, value, onChanged);
            UiLayoutUtility.EnsureLayoutElement(toggle.transform.parent.gameObject, 70f);
        }

        private static Text AddVolumeSlider(
            Transform parent,
            string label,
            float value,
            System.Action<float> onChanged)
        {
            var row = new GameObject(label + "SliderRow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 110f;
            rowLayout.minHeight = 110f;
            rowLayout.flexibleWidth = 1f;

            Image bg = row.GetComponent<Image>();
            bg.raycastTarget = false;
            VisualTheme theme = VisualThemeLocator.Load();
            if (theme?.Panel != null)
            {
                bg.sprite = theme.Panel;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(1f, 1f, 1f, 0.45f);
            }
            else
            {
                bg.color = new Color(0.16f, 0.22f, 0.3f, 0.9f);
            }

            VerticalLayoutGroup vlg = row.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 8, 8);
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Text title = MenuOverlayUi.CreateText(row.transform, "Label", label, 26, TextAnchor.MiddleLeft);
            UiLayoutUtility.EnsureLayoutElement(title.gameObject, 32f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(Image), typeof(LayoutElement));
            sliderGo.transform.SetParent(row.transform, false);
            LayoutElement sliderLayout = sliderGo.GetComponent<LayoutElement>();
            sliderLayout.preferredHeight = 48f;
            sliderLayout.minHeight = 48f;
            sliderLayout.flexibleWidth = 1f;

            Image sliderBg = sliderGo.GetComponent<Image>();
            sliderBg.raycastTarget = true;
            sliderBg.color = new Color(0.1f, 0.14f, 0.2f, 1f);
            if (theme?.HpBarBackground != null)
            {
                sliderBg.sprite = theme.HpBarBackground;
                sliderBg.type = Image.Type.Sliced;
                sliderBg.color = Color.white;
            }

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.2f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.8f);
            fillAreaRect.offsetMin = new Vector2(10f, 0f);
            fillAreaRect.offsetMax = new Vector2(-10f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.raycastTarget = false;
            fillImage.color = new Color(0.18f, 0.78f, 0.72f, 1f);
            if (theme?.HpBarFill != null)
            {
                fillImage.sprite = theme.HpBarFill;
                fillImage.type = Image.Type.Sliced;
                fillImage.color = Color.white;
            }

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(36f, 36f);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.raycastTarget = true;
            handleImage.color = Color.white;
            if (theme?.ButtonNormal != null)
            {
                handleImage.sprite = theme.ButtonNormal;
                handleImage.type = Image.Type.Sliced;
            }

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.interactable = true;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));

            return title;
        }

        private static void AddActionButton(Transform parent, string name, string label, System.Action onClick)
        {
            Button button = MenuOverlayUi.CreateLayoutButton(parent, name, label, 88f, () =>
            {
                GameAudio.PlaySfx(SfxId.UiConfirm);
                onClick?.Invoke();
            }, fontSize: 30);
            UiButtonStyler.ApplyStandardTheme(button);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
            }
        }

        public void Hide()
        {
            if (_root == null)
            {
                return;
            }

            _settings?.Persist();
            GameAudio.PlaySfx(SfxId.UiClose);
            Destroy(_root);
            _root = null;
            _bgmVolumeLabel = null;
            _sfxVolumeLabel = null;
        }

        private void OnDestroy()
        {
            Hide();
        }
    }
}
