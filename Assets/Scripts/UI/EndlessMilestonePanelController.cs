using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Endless;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>무한 모드 마일스톤 수령. 로컬 최고와 수령 상태를 분리 표시한다.</summary>
    public sealed class EndlessMilestonePanelController : MonoBehaviour
    {
        private GameObject _root;

        public bool IsOpen => _root != null;

        public void Show()
        {
            if (_root != null)
            {
                return;
            }

            EndlessMilestoneTrack track = EndlessMilestoneCatalog.Load();
            if (track == null || track.Steps.Length == 0)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            GameAudio.PlaySfx(SfxId.UiOpen);
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            _root = MenuOverlayUi.CreateRoot("EndlessMilestonePanel", sortingOrder: 4120);
            MenuOverlayUi.CreateFullScreenDim(_root.transform, new Color(0f, 0f, 0f, 0.72f), Hide);
            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_root.transform);

            GameObject box = MenuOverlayUi.CreateOverlayBox(host, MenuOverlayUi.OverlaySizeStandard);
            MenuOverlayUi.CreateOverlayTitle(box.transform, "무한 마일스톤");
            MenuOverlayUi.OverlayScroll scroll = MenuOverlayUi.CreateOverlayScroll(box.transform);
            Transform content = scroll.Content;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;

            Text best = MenuOverlayUi.CreateText(
                content,
                "Best",
                $"로컬 최고 점수 {meta.endlessBestScore}  /  최고 역 {meta.endlessBestStationReached}",
                24,
                TextAnchor.MiddleCenter);
            UiLayoutUtility.EnsureLayoutElement(best.gameObject, 48f);
            UiLayoutUtility.ResetForVerticalLayout(best.rectTransform, 48f);

            EndlessMilestoneStep[] steps = track.Steps;
            for (int i = 0; i < steps.Length; i++)
            {
                EndlessMilestoneStep step = steps[i];
                if (step == null)
                {
                    continue;
                }

                bool reached = EndlessProgressService.IsMilestoneReached(meta, step);
                bool claimed = EndlessProgressService.HasClaimedMilestone(meta, step.id);
                string req = step.requiredStation > 0
                    ? $"역 {step.requiredStation}"
                    : $"점수 {step.requiredScore}";
                string label = claimed
                    ? $"수령 완료 · {req}"
                    : reached
                        ? $"수령 · {req} (+{step.ticketFragments})"
                        : $"미달성 · {req}";
                string milestoneId = step.id;
                Button button = MenuOverlayUi.CreateLayoutButton(
                    content,
                    $"Ms_{i}",
                    label,
                    56f,
                    () => OnClaim(milestoneId),
                    30,
                    UiButtonStyler.OverlayActionWidth);
                button.interactable = reached && !claimed;
            }

            MenuOverlayUi.CreateOverlayClose(box.transform, Hide);
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

        private void OnClaim(string milestoneId)
        {
            EndlessMilestoneTrack track = EndlessMilestoneCatalog.Load();
            if (track == null)
            {
                return;
            }

            EndlessMilestoneStep step = null;
            EndlessMilestoneStep[] steps = track.Steps;
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null && steps[i].id == milestoneId)
                {
                    step = steps[i];
                    break;
                }
            }

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (!EndlessProgressService.TryClaimMilestone(meta, step))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            MetaSaveSystem.Save(meta);
            GameAudio.PlaySfx(SfxId.UiConfirm);
            Hide();
            Show();
        }
    }
}
