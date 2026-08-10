using System;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.LiveOps;
using LastTrain.Run;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>활성 라이브 이벤트 배너·보상 수령·이벤트 런 시작.</summary>
    public sealed class LiveEventPanelController : MonoBehaviour
    {
        private GameObject _root;

        public bool IsOpen => _root != null;

        public void Show()
        {
            if (_root != null)
            {
                return;
            }

            AppRoot appRoot = AppRoot.Instance;
            appRoot?.RefreshLiveOpsOnMenu();
            LiveEventService live = appRoot?.LiveEvents;
            if (live == null || !live.HasActiveEvent)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            GameAudio.PlaySfx(SfxId.UiOpen);
            _root = MenuOverlayUi.CreateRoot("LiveEventPanel", sortingOrder: 4150);
            MenuOverlayUi.CreateFullScreenDim(_root.transform, new Color(0f, 0f, 0f, 0.72f), Hide);
            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_root.transform);

            GameObject box = MenuOverlayUi.CreatePanel(host, "Box", new Color(0.14f, 0.18f, 0.26f, 0.98f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.08f, 0.16f);
            boxRect.anchorMax = new Vector2(0.92f, 0.84f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.SetParent(box.transform, false);
            MenuOverlayUi.Stretch(content);
            content.offsetMin = new Vector2(28f, 28f);
            content.offsetMax = new Vector2(-28f, -28f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LiveEventData evt = live.ActiveEvent;
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            LiveEventProgress progress = live.GetOrCreateProgress(meta, evt);

            Text title = MenuOverlayUi.CreateText(content, "Title", evt.DisplayName, 36, TextAnchor.MiddleCenter);
            UiLayoutUtility.EnsureLayoutElement(title.gameObject, 52f);
            UiLayoutUtility.ResetForVerticalLayout(title.rectTransform, 52f);

            string remaining = FormatRemaining(evt);
            string currencyName = evt.EventCurrency != null ? evt.EventCurrency.DisplayName : "이벤트 재화";
            Text status = MenuOverlayUi.CreateText(
                content,
                "Status",
                $"{remaining}\n{currencyName}: {progress.currencyBalance}",
                26,
                TextAnchor.MiddleCenter);
            UiLayoutUtility.EnsureLayoutElement(status.gameObject, 72f);
            UiLayoutUtility.ResetForVerticalLayout(status.rectTransform, 72f);

            EventRewardTrack track = evt.RewardTrack;
            if (track != null)
            {
                EventRewardStep[] steps = track.Steps;
                for (int i = 0; i < steps.Length && i < 4; i++)
                {
                    EventRewardStep step = steps[i];
                    if (step == null)
                    {
                        continue;
                    }

                    string label = progress.HasClaimed(step.rewardId)
                        ? $"수령 완료 · {step.rewardId}"
                        : $"보상 수령 ({step.requiredCurrency})";
                    string rewardId = step.rewardId;
                    Button claim = MenuOverlayUi.CreateLayoutButton(
                        content,
                        $"Claim_{i}",
                        label,
                        64f,
                        () => OnClaim(rewardId));
                    claim.interactable = !progress.HasClaimed(step.rewardId)
                                        && progress.currencyBalance >= step.requiredCurrency;
                }
            }

            MenuOverlayUi.CreateLayoutButton(content, "StartEventRun", "이벤트 플레이", 72f, OnStartEventRun);
            MenuOverlayUi.CreateLayoutButton(content, "Close", "닫기", 64f, Hide);
        }

        public void Hide()
        {
            if (_root == null)
            {
                return;
            }

            GameAudio.PlaySfx(SfxId.UiCancel);
            Destroy(_root);
            _root = null;
        }

        private void OnClaim(string rewardId)
        {
            AppRoot appRoot = AppRoot.Instance;
            LiveEventService live = appRoot?.LiveEvents;
            LiveEventData evt = live?.ActiveEvent;
            if (live == null || evt == null)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (!live.TryClaimReward(meta, evt, rewardId))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            MetaSaveSystem.Save(meta);
            GameAudio.PlaySfx(SfxId.Reward);
            Hide();
            Show();
        }

        private void OnStartEventRun()
        {
            AppRoot appRoot = AppRoot.Instance;
            LiveEventData evt = appRoot?.LiveEvents?.ActiveEvent;
            if (appRoot == null || evt == null)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            GameAudio.PlaySfx(SfxId.UiConfirm);
            RunSaveSystem.DeleteRunSave();
            Difficulty.DifficultySelectionState.UnlockSelection();

            RunStartConfig config = RunStartConfig.CreateLiveEventRun(evt);
            if (string.IsNullOrWhiteSpace(config.DifficultyId)
                || string.Equals(config.DifficultyId, Difficulty.DifficultyIds.Normal, StringComparison.Ordinal)
                   && evt.EventDifficulty == null)
            {
                config.DifficultyId = Difficulty.DifficultySelectionState.SelectedDifficultyId;
            }

            appRoot.GameSession.StartNewRun(config);
            Hide();
            SceneFlow.Load(SceneNames.Game);
        }

        private static string FormatRemaining(LiveEventData evt)
        {
            if (evt == null || !evt.TryGetSchedule(out _, out DateTime end))
            {
                return "기간 정보 없음";
            }

            TimeSpan left = end.ToUniversalTime() - DateTime.UtcNow;
            if (left.TotalSeconds <= 0)
            {
                return "종료됨";
            }

            if (left.TotalDays >= 1)
            {
                return $"남은 기간: {Math.Ceiling(left.TotalDays)}일";
            }

            return $"남은 시간: {Math.Ceiling(left.TotalHours)}시간";
        }
    }
}
