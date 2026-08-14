using LastTrain.Audio;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>업적 목록 패널. Meta unlockedAchievementIds와 연동한다.</summary>
    public sealed class AchievementPanelController : MonoBehaviour
    {
        private GameObject _root;

        public bool IsOpen => _root != null;

        public void Show()
        {
            if (_root != null)
            {
                return;
            }

            GameAudio.PlaySfx(SfxId.UiOpen);
            _root = MenuOverlayUi.CreateRoot("AchievementPanel", sortingOrder: 4100);
            MenuOverlayUi.CreateFullScreenDim(_root.transform, new Color(0f, 0f, 0f, 0.72f), Hide);
            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_root.transform);

            GameObject box = MenuOverlayUi.CreateOverlayBox(host, MenuOverlayUi.OverlaySizeTall);
            Text title = MenuOverlayUi.CreateOverlayTitle(box.transform, "업적");
            MenuOverlayUi.OverlayScroll scroll = MenuOverlayUi.CreateOverlayScroll(box.transform);
            Transform content = scroll.Content;

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            int unlocked = 0;
            for (int i = 0; i < AchievementCatalog.AllIds.Length; i++)
            {
                AchievementEntryView view = AchievementVisibilityResolver.BuildEntry(meta, AchievementCatalog.AllIds[i]);
                if (view.IsUnlocked)
                {
                    unlocked++;
                }

                AddEntryRow(content, view);
            }

            title.text = $"업적  {unlocked}/{AchievementCatalog.AllIds.Length}";
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

        private static void AddEntryRow(Transform parent, AchievementEntryView entry)
        {
            var row = new GameObject(
                entry.Id + "Row",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup));
            row.transform.SetParent(parent, false);
            Image rowBg = row.GetComponent<Image>();
            rowBg.color = entry.IsUnlocked
                ? new Color(0.16f, 0.24f, 0.18f, 0.95f)
                : new Color(0.08f, 0.08f, 0.1f, 0.95f);

            VerticalLayoutGroup vlg = row.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            UiLayoutUtility.EnsureLayoutElement(row, 84f);

            Text title = MenuOverlayUi.CreateText(row.transform, "Title", entry.Title, 24, TextAnchor.MiddleLeft);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            UiLayoutUtility.EnsureLayoutElement(title.gameObject, 30f);
            UiLayoutUtility.ResetForVerticalLayout(title.rectTransform, 30f);
            Text detail = MenuOverlayUi.CreateText(row.transform, "Detail", entry.Detail, 18, TextAnchor.UpperLeft);
            detail.color = new Color(0.82f, 0.86f, 0.92f, 1f);
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            detail.verticalOverflow = VerticalWrapMode.Truncate;
            UiLayoutUtility.EnsureLayoutElement(detail.gameObject, 36f);
            UiLayoutUtility.ResetForVerticalLayout(detail.rectTransform, 36f);
        }

        private void OnDestroy()
        {
            Hide();
        }
    }
}
