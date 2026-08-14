using System.Collections.Generic;
using LastTrain.Audio;
using LastTrain.Data;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>승객·적·보스·유물 도감. Meta 발견/숙련과 연동한다.</summary>
    public sealed class CodexPanelController : MonoBehaviour
    {
        private GameObject _root;
        private GameDatabase _database;
        private VisualDatabase _visuals;
        private MetaSaveData _meta;
        private Transform _content;
        private CodexCategory _activeCategory = CodexCategory.Passenger;
        private readonly Dictionary<CodexCategory, Button> _tabButtons = new Dictionary<CodexCategory, Button>();

        public bool IsOpen => _root != null;

        public void Show(GameDatabase database)
        {
            if (_root != null)
            {
                return;
            }

            _database = database ?? GameDatabaseLocator.Load();
            _visuals = VisualDatabaseLocator.Load();
            _meta = MetaSaveSystem.LoadOrCreate();
            _meta.pendingNewDiscoveryIds = System.Array.Empty<string>();
            MetaSaveSystem.Save(_meta);

            GameAudio.PlaySfx(SfxId.UiOpen);
            _root = MenuOverlayUi.CreateRoot("CodexPanel", sortingOrder: 4100);
            GameObject dim = MenuOverlayUi.CreateFullScreenDim(
                _root.transform,
                new Color(0f, 0f, 0f, 0.72f),
                Hide);
            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(_root.transform);

            GameObject box = MenuOverlayUi.CreateOverlayBox(host, MenuOverlayUi.OverlaySizeTall);
            MenuOverlayUi.CreateOverlayTitle(box.transform, "도감");
            BuildTabRow(box.transform);

            MenuOverlayUi.OverlayScroll scroll = MenuOverlayUi.CreateOverlayScroll(box.transform, extraTop: 60f);
            _content = scroll.Content;
            VerticalLayoutGroup layout = _content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 8);

            RebuildList();
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
            _content = null;
            _tabButtons.Clear();
        }

        private void BuildTabRow(Transform parent)
        {
            var row = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(-(MenuOverlayUi.OverlayPad * 2f), 52f);
            rowRect.anchoredPosition = new Vector2(
                0f,
                -(MenuOverlayUi.OverlayPad + MenuOverlayUi.OverlayTitleHeight + 4f));

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            AddTab(row.transform, CodexCategory.Passenger, "승객");
            AddTab(row.transform, CodexCategory.Enemy, "적");
            AddTab(row.transform, CodexCategory.Boss, "보스");
            AddTab(row.transform, CodexCategory.Relic, "유물");
            RefreshTabHighlight();
        }

        private void AddTab(Transform parent, CodexCategory category, string label)
        {
            CodexCategory captured = category;
            Button button = MenuOverlayUi.CreateLayoutButton(
                parent,
                category.ToString(),
                label,
                48f,
                () => SelectTab(captured),
                fontSize: 22);
            _tabButtons[category] = button;
        }

        private void SelectTab(CodexCategory category)
        {
            if (_activeCategory == category)
            {
                return;
            }

            _activeCategory = category;
            RefreshTabHighlight();
            RebuildList();
            GameAudio.PlaySfx(SfxId.UiClick);
        }

        private void RefreshTabHighlight()
        {
            foreach (KeyValuePair<CodexCategory, Button> pair in _tabButtons)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                Image image = pair.Value.GetComponent<Image>();
                if (image != null)
                {
                    image.color = pair.Key == _activeCategory
                        ? new Color(0.28f, 0.42f, 0.62f, 1f)
                        : new Color(0.18f, 0.22f, 0.28f, 1f);
                }
            }
        }

        private void RebuildList()
        {
            if (_content == null)
            {
                return;
            }

            UiLayoutUtility.DestroyChildren(_content);

            List<CodexEntryView> entries = BuildActiveEntries();
            if (entries.Count == 0)
            {
                Text empty = MenuOverlayUi.CreateText(
                    _content,
                    "Empty",
                    "등록된 항목이 없습니다.",
                    22,
                    TextAnchor.MiddleLeft);
                UiLayoutUtility.EnsureLayoutElement(empty.gameObject, 36f);
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AddEntryRow(_content, entries[i]);
            }
        }

        private List<CodexEntryView> BuildActiveEntries()
        {
            var entries = new List<CodexEntryView>();
            if (_database == null)
            {
                return entries;
            }

            switch (_activeCategory)
            {
                case CodexCategory.Passenger:
                    AppendPassengers(entries);
                    break;
                case CodexCategory.Enemy:
                    AppendEnemies(entries, EnemyType.Normal);
                    break;
                case CodexCategory.Boss:
                    AppendEnemies(entries, EnemyType.Boss);
                    break;
                case CodexCategory.Relic:
                    AppendRelics(entries);
                    break;
            }

            return entries;
        }

        private void AppendPassengers(List<CodexEntryView> entries)
        {
            IReadOnlyList<PassengerData> passengers = _database.Passengers;
            if (passengers == null)
            {
                return;
            }

            for (int i = 0; i < passengers.Count; i++)
            {
                PassengerData data = passengers[i];
                if (data == null)
                {
                    continue;
                }

                entries.Add(CodexVisibilityResolver.BuildPassengerEntry(_meta, data, _visuals));
            }
        }

        private void AppendEnemies(List<CodexEntryView> entries, EnemyType typeFilter)
        {
            IReadOnlyList<EnemyData> enemies = _database.Enemies;
            if (enemies == null)
            {
                return;
            }

            CodexCategory category = typeFilter == EnemyType.Boss ? CodexCategory.Boss : CodexCategory.Enemy;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData data = enemies[i];
                if (data == null || data.EnemyType != typeFilter)
                {
                    continue;
                }

                entries.Add(CodexVisibilityResolver.BuildEnemyEntry(_meta, data, _visuals, category));
            }
        }

        private void AppendRelics(List<CodexEntryView> entries)
        {
            IReadOnlyList<RelicData> relics = _database.Relics;
            if (relics == null)
            {
                return;
            }

            for (int i = 0; i < relics.Count; i++)
            {
                RelicData data = relics[i];
                if (data == null)
                {
                    continue;
                }

                entries.Add(CodexVisibilityResolver.BuildRelicEntry(_meta, data));
            }
        }

        private static void AddEntryRow(Transform parent, CodexEntryView entry)
        {
            var row = new GameObject(
                entry.Id + "Row",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(Image));
            row.transform.SetParent(parent, false);
            Image rowBg = row.GetComponent<Image>();
            rowBg.color = entry.IsDiscovered
                ? new Color(0.16f, 0.2f, 0.28f, 0.95f)
                : new Color(0.08f, 0.08f, 0.1f, 0.95f);

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.padding = new RectOffset(12, 12, 10, 10);
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            UiLayoutUtility.EnsureLayoutElement(row, 112f);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(row.transform, false);
            Image icon = iconGo.GetComponent<Image>();
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(72f, 72f);
            LayoutElement iconLayout = iconGo.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 72f;
            iconLayout.preferredHeight = 72f;

            if (entry.IsDiscovered && entry.Portrait != null)
            {
                icon.sprite = entry.Portrait;
                icon.color = Color.white;
                icon.preserveAspect = true;
            }
            else
            {
                icon.color = entry.IsDiscovered
                    ? new Color(0.35f, 0.38f, 0.42f, 1f)
                    : new Color(0.12f, 0.12f, 0.14f, 1f);
            }

            var textCol = new GameObject(
                "TextCol",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            textCol.transform.SetParent(row.transform, false);
            LayoutElement textColLayout = textCol.GetComponent<LayoutElement>();
            textColLayout.flexibleWidth = 1f;
            textColLayout.preferredHeight = 88f;
            textColLayout.minHeight = 88f;

            VerticalLayoutGroup vlg = textCol.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;

            Text title = MenuOverlayUi.CreateText(textCol.transform, "Title", entry.Title, 24, TextAnchor.MiddleLeft);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            UiLayoutUtility.EnsureLayoutElement(title.gameObject, 28f);
            UiLayoutUtility.ResetForVerticalLayout(title.rectTransform, 28f);
            Text detail = MenuOverlayUi.CreateText(textCol.transform, "Detail", entry.Detail, 18, TextAnchor.UpperLeft);
            detail.color = new Color(0.82f, 0.86f, 0.92f, 1f);
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            detail.verticalOverflow = VerticalWrapMode.Truncate;
            UiLayoutUtility.EnsureLayoutElement(detail.gameObject, 48f);
            UiLayoutUtility.ResetForVerticalLayout(detail.rectTransform, 48f);
        }

        private void OnDestroy()
        {
            Hide();
        }
    }
}
