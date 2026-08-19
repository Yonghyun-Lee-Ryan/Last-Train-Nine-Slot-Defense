using LastTrain.Ads;
using LastTrain.Attendance;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>7일 출석 보상 패널. Growth 탭·자동 팝업 진입.</summary>
    public sealed class AttendancePanelController : MonoBehaviour
    {
        private const float DayRowHeight = 76f;
        private const float DayRowSpacing = 10f;
        private const float FooterHeight = 264f;

        private GameObject _root;
        private Transform _dayList;
        private Text _statusLabel;
        private Button _claimButton;
        private Button _adBonusButton;
        private Text _adBonusLabel;

        public bool IsOpen => _root != null;

        public void TryShowIfClaimable()
        {
            if (MenuOverlayUi.IsOverlayOpen("PrivacyConsentDialog")
                || MenuOverlayUi.IsOverlayOpen("DifficultyUnlockPopup"))
            {
                return;
            }

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (!AttendanceService.CanClaimToday(meta) || IsOpen)
            {
                return;
            }

            Show();
        }

        public void Show()
        {
            if (_root != null)
            {
                RefreshContent();
                return;
            }

            GameAudio.PlaySfx(SfxId.UiOpen);
            _root = MenuOverlayUi.CreateRoot("AttendancePanel", sortingOrder: 4150);
            MenuOverlayUi.CreateFullScreenDim(
                _root.transform,
                new Color(0f, 0f, 0f, 0.72f),
                Hide);
            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_root.transform);

            VisualTheme theme = VisualThemeLocator.Load();
            GameObject box = MenuOverlayUi.CreateOverlayBox(host, MenuOverlayUi.OverlaySizeCompact);
            if (theme?.Panel != null)
            {
                Image boxImage = box.GetComponent<Image>();
                boxImage.sprite = null;
                boxImage.color = MenuOverlayUi.OverlayFill;
            }

            Text title = MenuOverlayUi.CreateOverlayTitle(box.transform, "7일 출석");

            RectTransform footer = MenuOverlayUi.PinOverlayFooter(box.transform, FooterHeight);
            var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            footerLayout.spacing = 8f;
            footerLayout.padding = new RectOffset(4, 4, 4, 4);
            footerLayout.childAlignment = TextAnchor.LowerCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;

            _statusLabel = MenuOverlayUi.CreateText(footer, "Status", string.Empty, 20, TextAnchor.MiddleCenter);
            _statusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statusLabel.verticalOverflow = VerticalWrapMode.Truncate;
            UiLayoutUtility.EnsureLayoutElement(_statusLabel.gameObject, 44f);

            _claimButton = MenuOverlayUi.CreateLayoutButton(
                footer,
                "ClaimButton",
                "오늘 보상 받기",
                64f,
                OnClaimClicked,
                fontSize: 26,
                preferredWidth: UiButtonStyler.OverlayActionWidth);
            _adBonusButton = MenuOverlayUi.CreateLayoutButton(
                footer,
                "AdBonusButton",
                "광고로 추가 보상",
                52f,
                OnAdBonusClicked,
                fontSize: 22,
                preferredWidth: UiButtonStyler.OverlayActionWidth);
            _adBonusLabel = _adBonusButton.GetComponentInChildren<Text>();
            MenuOverlayUi.CreateLayoutButton(
                footer,
                "Close",
                "닫기",
                64f,
                Hide,
                30,
                UiButtonStyler.OverlayActionWidth);

            MenuOverlayUi.OverlayScroll scroll = MenuOverlayUi.CreateOverlayScroll(
                box.transform,
                extraBottom: FooterHeight - MenuOverlayUi.OverlayCloseHeight);
            scroll.Root.name = "Scroll";
            _dayList = scroll.Content;
            VerticalLayoutGroup listLayout = _dayList.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = DayRowSpacing;

            GameFontProvider.ApplyTo(_root);
            RefreshContent();
        }

        public void Hide()
        {
            if (_root == null)
            {
                return;
            }

            GameAudio.PlaySfx(SfxId.UiClose);
            GameObject root = _root;
            _root = null;
            _dayList = null;
            _statusLabel = null;
            _claimButton = null;
            _adBonusButton = null;
            _adBonusLabel = null;
            MenuOverlayUi.DestroyRoot(root);
        }

        private void RefreshContent()
        {
            if (_root == null)
            {
                return;
            }

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            AttendanceService.EnsureDayState(meta);
            RebuildDayList(meta);

            int nextDay = AttendanceService.GetNextRewardDayIndex(meta);
            AttendanceDayReward nextReward = AttendanceRewardTable.GetReward(nextDay);
            bool canClaim = AttendanceService.CanClaimToday(meta);
            bool canAdBonus = AttendanceService.CanClaimAdBonus(meta);

            if (_statusLabel != null)
            {
                if (canClaim)
                {
                    _statusLabel.text =
                        $"오늘 보상 · {AttendanceRewardTable.Describe(nextReward)}";
                }
                else if (canAdBonus)
                {
                    _statusLabel.text = "기본 보상 수령 완료 · 광고로 같은 보상을 한 번 더 받을 수 있습니다.";
                }
                else
                {
                    _statusLabel.text = "오늘 출석 보상을 모두 받았습니다. 내일 다시 만나요!";
                }
            }

            if (_claimButton != null)
            {
                _claimButton.interactable = canClaim;
            }

            if (_adBonusButton != null)
            {
                AdCoordinator ads = AppRoot.Instance?.Ads;
                bool adReady = ads != null
                               && ads.IsReady(RewardedAdPlacement.AttendanceBonus)
                               && canAdBonus;
                _adBonusButton.gameObject.SetActive(adReady);
                _adBonusButton.interactable = adReady;
                if (_adBonusLabel != null)
                {
                    _adBonusLabel.text = adReady
                        ? "광고로 추가 보상"
                        : canAdBonus
                            ? "광고 준비 중"
                            : "광고로 추가 보상";
                }
            }

            RectTransform box = _root.transform.Find("SafeArea/Box") as RectTransform;
            UiLayoutUtility.ForceRebuild(_dayList as RectTransform);
            UiLayoutUtility.ForceRebuild(box);
        }

        private void RebuildDayList(MetaSaveData meta)
        {
            if (_dayList == null)
            {
                return;
            }

            UiLayoutUtility.DestroyChildren(_dayList);

            int completed = AttendanceService.GetCompletedDayCount(meta);
            int nextDay = AttendanceService.GetNextRewardDayIndex(meta);
            bool canClaim = AttendanceService.CanClaimToday(meta);

            for (int day = 0; day < AttendanceRewardTable.CycleLength; day++)
            {
                bool isCompleted = day < completed;
                bool isToday = canClaim && day == nextDay;
                AttendanceDayReward reward = AttendanceRewardTable.GetReward(day);
                AddDayRow(_dayList, day, AttendanceRewardTable.Describe(reward), isCompleted, isToday);
            }
        }

        private static void AddDayRow(Transform parent, int day, string rewardText, bool completed, bool today)
        {
            var row = new GameObject($"Day{day + 1}", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            Image bg = row.GetComponent<Image>();
            bg.color = completed
                ? new Color(0.22f, 0.42f, 0.28f, 0.95f)
                : today
                    ? new Color(0.28f, 0.42f, 0.62f, 0.95f)
                    : new Color(0.14f, 0.16f, 0.2f, 0.95f);

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = DayRowHeight;
            rowLayout.preferredHeight = DayRowHeight;
            rowLayout.flexibleWidth = 1f;
            rowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 10, 10);
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            float innerHeight = DayRowHeight - 20f;
            Text dayLabel = MenuOverlayUi.CreateText(row.transform, "DayLabel", $"{day + 1}일", 22, TextAnchor.MiddleCenter);
            dayLabel.verticalOverflow = VerticalWrapMode.Truncate;
            dayLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            LayoutElement dayElement = UiLayoutUtility.EnsureLayoutElement(dayLabel.gameObject, innerHeight, 0f);
            dayElement.minHeight = innerHeight;
            dayElement.preferredHeight = innerHeight;
            dayElement.minWidth = 72f;
            dayElement.preferredWidth = 72f;
            dayElement.flexibleWidth = 0f;

            Text rewardLabel = MenuOverlayUi.CreateText(row.transform, "Reward", rewardText, 18, TextAnchor.MiddleLeft);
            rewardLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            rewardLabel.verticalOverflow = VerticalWrapMode.Truncate;
            rewardLabel.resizeTextForBestFit = true;
            rewardLabel.resizeTextMinSize = 14;
            rewardLabel.resizeTextMaxSize = 18;
            LayoutElement rewardElement = UiLayoutUtility.EnsureLayoutElement(rewardLabel.gameObject, innerHeight);
            rewardElement.minHeight = innerHeight;
            rewardElement.preferredHeight = innerHeight;
            rewardElement.flexibleWidth = 1f;

            string state = completed ? "완료" : today ? "오늘" : "대기";
            Text stateLabel = MenuOverlayUi.CreateText(row.transform, "State", state, 20, TextAnchor.MiddleCenter);
            stateLabel.verticalOverflow = VerticalWrapMode.Truncate;
            stateLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            LayoutElement stateElement = UiLayoutUtility.EnsureLayoutElement(stateLabel.gameObject, innerHeight, 0f);
            stateElement.minHeight = innerHeight;
            stateElement.preferredHeight = innerHeight;
            stateElement.minWidth = 72f;
            stateElement.preferredWidth = 72f;
            stateElement.flexibleWidth = 0f;
        }

        private void OnClaimClicked()
        {
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (!AttendanceService.TryClaimBase(meta, out AttendanceGrant grant))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                RefreshContent();
                return;
            }

            MetaSaveSystem.Save(meta);
            GameAudio.PlaySfx(SfxId.Reward);
            Debug.Log(
                $"[Attendance] 기본 보상 Day {grant.CycleDayIndex + 1}: "
                + $"tickets+{grant.TicketFragments}, xp+{grant.AccountXp}, freeSummon+{grant.FreeSummonCharges}");
            RefreshContent();
            GetComponent<MainMenuController>()?.RefreshMetaProgress();
            GetComponent<MainMenuController>()?.RefreshAttendanceButton();
        }

        private void OnAdBonusClicked()
        {
            AdCoordinator ads = AppRoot.Instance?.Ads;
            if (ads == null || !ads.IsReady(RewardedAdPlacement.AttendanceBonus))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                RefreshContent();
                return;
            }

            ads.ShowRewarded(
                RewardedAdPlacement.AttendanceBonus,
                () =>
                {
                    MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
                    if (AttendanceService.TryGrantAdBonus(meta, out AttendanceGrant grant))
                    {
                        MetaSaveSystem.Save(meta);
                        Debug.Log(
                            $"[Attendance] 광고 추가 보상 Day {grant.CycleDayIndex + 1}: "
                            + $"tickets+{grant.TicketFragments}, xp+{grant.AccountXp}");
                    }
                },
                _ =>
                {
                    RefreshContent();
                    GetComponent<MainMenuController>()?.RefreshMetaProgress();
                });
        }

        private void OnDestroy()
        {
            Hide();
        }
    }
}
