using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Integrations;
using LastTrain.Release;
using LastTrain.Save;
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
            GameObject dim = MenuOverlayUi.CreateFullScreenDim(
                _root.transform,
                new Color(0f, 0f, 0f, 0.72f),
                Hide);
            if (theme?.PopupDim != null)
            {
                Image dimImage = dim.GetComponent<Image>();
                dimImage.sprite = theme.PopupDim;
                dimImage.type = Image.Type.Sliced;
                dimImage.color = Color.white;
            }

            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_root.transform);

            GameObject box = MenuOverlayUi.CreateOverlayBox(host, MenuOverlayUi.OverlaySizeStandard);
            MenuOverlayUi.CreateOverlayTitle(box.transform, "설정", 40);
            MenuOverlayUi.CreateOverlayClose(box.transform, Hide);

            MenuOverlayUi.OverlayScroll scroll = MenuOverlayUi.CreateOverlayScroll(box.transform);
            Transform content = scroll.Content;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(4, 4, 4, 4);

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
                AddToggleRow(content, "화면 흔들림", _settings.ScreenShakeEnabled, enabled =>
                {
                    _settings.SetScreenShakeEnabled(enabled);
                    _settings.Persist();
                });
                AddToggleRow(content, "피해 숫자", _settings.DamageNumbersEnabled, enabled =>
                {
                    _settings.SetDamageNumbersEnabled(enabled);
                    _settings.Persist();
                });
                AddToggleRow(content, "코인 숫자", _settings.CoinNumbersEnabled, enabled =>
                {
                    _settings.SetCoinNumbersEnabled(enabled);
                    _settings.Persist();
                });
                AddToggleRow(content, "저사양 이펙트", _settings.LowFxMode, enabled =>
                {
                    _settings.SetLowFxMode(enabled);
                    _settings.Persist();
                });
                AddBattleSpeedRow(content, _settings);
                AddToggleRow(content, "알림", _settings.NotificationsEnabled, enabled =>
                {
                    _settings.SetNotificationsEnabled(enabled);
                    _settings.Persist();
                });

                MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
                if (Tutorial.TutorialProgressService.CanRestart(meta)
                    || Tutorial.TutorialProgressService.ShouldOfferTutorial(meta))
                {
                    MenuOverlayUi.CreateLayoutButton(content, "RestartTutorial", "튜토리얼 다시 보기", 72f, () =>
                    {
                        Tutorial.TutorialProgressService.ResetProgress(meta);
                        MetaSaveSystem.Save(meta);
                        GameAudio.PlaySfx(SfxId.UiConfirm);
                        Hide();
                    }, fontSize: 26);
                }

                RefreshVolumeLabels();
            }

            if (_privacy != null)
            {
                AddToggleRow(content, "맞춤형 광고", _privacy.HasAdsConsent, granted =>
                {
                    PrivacyConsentService privacy = AppRoot.Instance?.Privacy ?? _privacy;
                    bool analytics = privacy != null && privacy.HasAnalyticsConsent;
                    AppRoot.Instance?.ApplyPrivacyConsent(granted, analytics);
                    _privacy = AppRoot.Instance?.Privacy ?? privacy;
                });
                AddToggleRow(content, "게임 분석", _privacy.HasAnalyticsConsent, granted =>
                {
                    PrivacyConsentService privacy = AppRoot.Instance?.Privacy ?? _privacy;
                    bool ads = privacy != null && privacy.HasAdsConsent;
                    AppRoot.Instance?.ApplyPrivacyConsent(ads, granted);
                    _privacy = AppRoot.Instance?.Privacy ?? privacy;
                });
            }

            AddFlexibleSpacer(content, "SpacerMid", 0.1f);

            AddActionButton(content, "Policy", "개인정보처리방침", () =>
            {
                if (!string.IsNullOrWhiteSpace(config.PrivacyPolicyUrl))
                {
                    Application.OpenURL(config.PrivacyPolicyUrl);
                }
            });

            AddActionButton(content, "DeleteData", "앱 데이터 삭제", () =>
            {
                string notice = string.IsNullOrWhiteSpace(config.DataDeletionNotice)
                    ? "앱 데이터 삭제 시 진행도, 메타 보상, 설정이 기기에서 제거됩니다."
                    : config.DataDeletionNotice;
                ShowDeleteConfirm(notice, () =>
                {
                    PlayerDataDeletionService.DeleteAllLocalData(_privacy, _settings);
                    appRoot.ApplyPrivacyConsent(adsGranted: false, analyticsGranted: false);
                    Hide();
                });
            });

            GameFontProvider.ApplyTo(_root);
            UiLayoutUtility.ForceRebuild(scroll.Content);
        }

        private void ShowDeleteConfirm(string notice, System.Action onConfirm)
        {
            if (_root == null || onConfirm == null)
            {
                return;
            }

            Transform existing = _root.transform.Find("DeleteConfirm");
            if (existing != null)
            {
                DestroyOverlay(existing.gameObject);
            }

            GameObject overlay = null;
            overlay = MenuOverlayUi.CreateFullScreenDim(
                _root.transform,
                new Color(0f, 0f, 0f, 0.45f),
                () => DestroyOverlay(overlay),
                placeBehindContent: false);
            overlay.name = "DeleteConfirm";

            VisualTheme theme = VisualThemeLocator.Load();
            GameObject box = MenuOverlayUi.CreateCenteredPanel(
                overlay.transform,
                "ConfirmBox",
                new Vector2(680f, 360f),
                new Color(0.16f, 0.12f, 0.14f, 0.98f));
            MenuOverlayUi.EnableClipping(box);
            if (theme?.Panel != null)
            {
                Image boxImage = box.GetComponent<Image>();
                boxImage.sprite = theme.Panel;
                boxImage.type = Image.Type.Sliced;
                boxImage.color = Color.white;
            }

            Text title = MenuOverlayUi.CreateText(box.transform, "Title", "앱 데이터 삭제", 32, TextAnchor.MiddleCenter);
            title.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-40f, 48f);
            titleRect.anchoredPosition = new Vector2(0f, -16f);

            Text body = MenuOverlayUi.CreateText(
                box.transform,
                "Body",
                notice + "\n\n이 작업은 되돌릴 수 없습니다.",
                22,
                TextAnchor.MiddleCenter);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(24f, 84f);
            bodyRect.offsetMax = new Vector2(-24f, -68f);

            var rowGo = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(box.transform, false);
            RectTransform rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.sizeDelta = new Vector2(-40f, 56f);
            rowRect.anchoredPosition = new Vector2(0f, 20f);
            HorizontalLayoutGroup row = rowGo.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.padding = new RectOffset(8, 8, 0, 0);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = false;

            const float confirmButtonHeight = 56f;
            MenuOverlayUi.CreateFixedSizeButton(
                rowGo.transform,
                "Cancel",
                "취소",
                new Vector2(260f, confirmButtonHeight),
                () => DestroyOverlay(overlay),
                fontSize: 24);
            MenuOverlayUi.CreateFixedSizeButton(
                rowGo.transform,
                "Confirm",
                "삭제",
                new Vector2(260f, confirmButtonHeight),
                () =>
                {
                    DestroyOverlay(overlay);
                    onConfirm();
                },
                fontSize: 24);

            GameFontProvider.ApplyTo(overlay);
            overlay.transform.SetAsLastSibling();
            UiLayoutUtility.ForceRebuild(box.GetComponent<RectTransform>());
        }

        private static void DestroyOverlay(GameObject overlay)
        {
            MenuOverlayUi.DestroyRoot(overlay);
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

        private static void AddBattleSpeedRow(Transform parent, GameSettingsService settings)
        {
            if (settings == null)
            {
                return;
            }

            Button button = MenuOverlayUi.CreateLayoutButton(
                parent,
                "BattleSpeed",
                SpeedLabel(settings.BattleSpeed),
                72f,
                null,
                fontSize: 26);
            UiLayoutUtility.EnsureLayoutElement(button.gameObject, 72f);
            button.onClick.AddListener(() =>
            {
                int next = LastTrain.Battle.BattleSpeedPreset.Cycle(settings.BattleSpeed);
                settings.SetBattleSpeed(next);
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = SpeedLabel(next);
                }

                GameAudio.PlaySfx(SfxId.Switch);
            });
        }

        private static string SpeedLabel(int preset)
        {
            return $"전투 속도  {LastTrain.Battle.BattleSpeedPreset.Clamp(preset)}x";
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
            GameObject root = _root;
            _root = null;
            _bgmVolumeLabel = null;
            _sfxVolumeLabel = null;
            MenuOverlayUi.DestroyRoot(root);
        }

        private void OnDestroy()
        {
            Hide();
        }
    }
}
