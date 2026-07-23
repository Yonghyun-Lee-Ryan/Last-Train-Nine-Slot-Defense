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

            GameObject dim = MenuOverlayUi.CreatePanel(_root.transform, "Dim", new Color(0f, 0f, 0f, 0.72f));
            MenuOverlayUi.Stretch(dim.GetComponent<RectTransform>());
            Button dimButton = dim.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Hide);

            GameObject box = MenuOverlayUi.CreatePanel(_root.transform, "Box", new Color(0.12f, 0.16f, 0.22f, 0.98f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.08f, 0.08f);
            boxRect.anchorMax = new Vector2(0.92f, 0.92f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;

            Text title = MenuOverlayUi.CreateText(box.transform, "Title", "일일 · 주간 미션", 36, TextAnchor.MiddleCenter);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-56f, 56f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);

            GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(box.transform, false);
            RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(24f, 100f);
            scrollRect.offsetMax = new Vector2(-24f, -84f);
            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.15f);
            scrollBg.raycastTarget = true;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            MenuOverlayUi.Stretch(viewport.GetComponent<RectTransform>());

            var contentGo = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            IReadOnlyList<MissionData> missions = _database?.Missions;
            MissionProgressService.EnsurePeriods(meta, missions);
            MetaSaveSystem.Save(meta);

            AddSection(content, "일일 미션", MissionPeriod.Daily, meta, missions);
            AddSection(content, "주간 미션", MissionPeriod.Weekly, meta, missions);

            MenuOverlayUi.CreateButton(
                box.transform,
                "Close",
                "닫기",
                new Vector2(0f, 36f),
                new Vector2(280f, 72f),
                Hide);
            Button close = box.transform.Find("Close")?.GetComponent<Button>();
            if (close != null)
            {
                RectTransform closeRect = close.GetComponent<RectTransform>();
                closeRect.anchorMin = new Vector2(0.5f, 0f);
                closeRect.anchorMax = new Vector2(0.5f, 0f);
                closeRect.pivot = new Vector2(0.5f, 0f);
                closeRect.anchoredPosition = new Vector2(0f, 28f);
            }
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
