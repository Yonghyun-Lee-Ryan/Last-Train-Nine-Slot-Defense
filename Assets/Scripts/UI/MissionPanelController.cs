using System.Collections.Generic;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Mission;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>일일/주간 미션 목록·보상 수령 패널.</summary>
    public sealed class MissionPanelController : MonoBehaviour
    {
        private GameObject _root;
        private GameDatabase _database;

        public bool IsOpen => _root != null;

        public void Show(GameDatabase database)
        {
            if (_root != null)
            {
                return;
            }

            _database = database ?? GameDatabaseLocator.Load();
            GameAudio.PlaySfx(SfxId.UiOpen);
            _root = MenuOverlayUi.CreateRoot("MissionPanel", sortingOrder: 4100);
            GameObject dim = MenuOverlayUi.CreateFullScreenDim(
                _root.transform,
                new Color(0f, 0f, 0f, 0.72f),
                Hide);
            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_root.transform);

            GameObject box = MenuOverlayUi.CreateOverlayBox(host, MenuOverlayUi.OverlaySizeTall);
            MenuOverlayUi.CreateOverlayTitle(box.transform, "일일 · 주간 미션");
            MenuOverlayUi.OverlayScroll scroll = MenuOverlayUi.CreateOverlayScroll(box.transform);
            Transform content = scroll.Content;

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            IReadOnlyList<MissionData> missions = _database?.Missions;
            MissionProgressService.EnsurePeriods(meta, missions);
            MetaSaveSystem.Save(meta);

            AddSection(content, "일일 미션", MissionPeriod.Daily, meta, missions);
            AddSection(content, "주간 미션", MissionPeriod.Weekly, meta, missions);
            MenuOverlayUi.CreateOverlayClose(box.transform, Hide);
        }

        public void Hide()
        {
            if (_root == null)
            {
                return;
            }

            GameAudio.PlaySfx(SfxId.UiClose);
            Destroy(_root);
            _root = null;
        }

        private void AddSection(
            Transform parent,
            string heading,
            MissionPeriod period,
            MetaSaveData meta,
            IReadOnlyList<MissionData> missions)
        {
            Text header = MenuOverlayUi.CreateText(parent, heading, heading, 28, TextAnchor.MiddleLeft);
            UiLayoutUtility.EnsureLayoutElement(header.gameObject, 40f);

            List<MissionProgressView> views = MissionProgressService.BuildViews(meta, missions, period);
            if (views.Count == 0)
            {
                Text empty = MenuOverlayUi.CreateText(parent, heading + "Empty", "등록된 미션이 없습니다.", 22, TextAnchor.MiddleLeft);
                UiLayoutUtility.EnsureLayoutElement(empty.gameObject, 36f);
                return;
            }

            for (int i = 0; i < views.Count; i++)
            {
                MissionProgressView view = views[i];
                MissionData data = view.Data;
                string label =
                    $"{data.DisplayName}  ({view.Progress}/{view.Target})"
                    + (view.Claimed ? "  [수령완료]" : view.CanClaim ? "  [수령 가능]" : string.Empty);

                MissionData captured = data;
                Button button = MenuOverlayUi.CreateLayoutButton(
                    parent,
                    data.Id,
                    label,
                    70f,
                    () => OnClaimClicked(captured),
                    fontSize: 22);
                button.interactable = view.CanClaim;
                Text detail = MenuOverlayUi.CreateText(
                    parent,
                    data.Id + "Desc",
                    $"{data.Description}  보상: 조각 {data.RewardTicketFragments} / XP {data.RewardAccountXp}",
                    18,
                    TextAnchor.MiddleLeft);
                UiLayoutUtility.EnsureLayoutElement(detail.gameObject, 28f);
            }
        }

        private void OnClaimClicked(MissionData mission)
        {
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (!MissionProgressService.TryClaimReward(meta, mission, out int tickets, out int xp))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            MetaSaveSystem.Save(meta);
            GameAudio.PlaySfx(SfxId.Reward);
            Hide();
            Show(_database);
            Debug.Log($"[Mission] 보상 수령: {mission.Id}, tickets+{tickets}, xp+{xp}");
        }

        private void OnDestroy()
        {
            Hide();
        }
    }
}
