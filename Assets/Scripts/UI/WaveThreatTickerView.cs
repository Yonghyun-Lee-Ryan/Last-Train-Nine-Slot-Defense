using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>다음 웨이브 적을 유형 이름·대수로 보여 주는 HUD 티커.</summary>
    public sealed class WaveThreatTickerView : MonoBehaviour
    {
        public const string RootName = "WaveThreatTicker";
        public const string DefaultCaption = "이번 웨이브";

        private RectTransform _root;
        private Text _caption;
        private RectTransform _row;
        private readonly List<GameObject> _slots = new();

        public int IconCount => _slots.Count;
        public bool IsShowing => isActiveAndEnabled && gameObject.activeSelf && IconCount > 0;
        public string CaptionText => _caption != null ? _caption.text : string.Empty;

        public static WaveThreatTickerView Ensure(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(RootName);
            WaveThreatTickerView view = existing != null
                ? existing.GetComponent<WaveThreatTickerView>()
                : null;
            if (view == null)
            {
                var go = new GameObject(RootName, typeof(RectTransform), typeof(WaveThreatTickerView), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, CombatTopHudLayout.ThreatTickerY);
                rect.sizeDelta = new Vector2(CombatTopHudLayout.ThreatTickerWidth, CombatTopHudLayout.ThreatTickerHeight);
                Transform pause = parent.Find("PauseOverlay");
                if (pause != null)
                {
                    rect.SetSiblingIndex(pause.GetSiblingIndex());
                }
                var bg = go.GetComponent<Image>();
                bg.color = new Color(0.06f, 0.08f, 0.12f, 0.72f);
                bg.raycastTarget = false;
                view = go.GetComponent<WaveThreatTickerView>();
            }

            view.EnsureLayout();
            return view;
        }

        private void Awake()
        {
            EnsureLayout();
        }

        public void Bind(IReadOnlyList<ThreatPreviewEntry> entries, VisualDatabase visuals = null, string caption = null)
        {
            EnsureLayout();
            ClearSlots();
            if (entries == null || entries.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (_caption != null)
            {
                _caption.text = string.IsNullOrWhiteSpace(caption) ? DefaultCaption : caption;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                _slots.Add(CreateSlot(entries[i], visuals));
            }
        }

        public void Hide()
        {
            ClearSlots();
            gameObject.SetActive(false);
        }

        private void EnsureLayout()
        {
            _root ??= transform as RectTransform;
            if (_root != null)
            {
                _root.anchorMin = new Vector2(0.5f, 1f);
                _root.anchorMax = new Vector2(0.5f, 1f);
                _root.pivot = new Vector2(0.5f, 1f);
                _root.anchoredPosition = new Vector2(0f, CombatTopHudLayout.ThreatTickerY);
                _root.sizeDelta = new Vector2(CombatTopHudLayout.ThreatTickerWidth, CombatTopHudLayout.ThreatTickerHeight);
            }

            EnsureCaption();
            EnsureRow();
        }

        private void EnsureCaption()
        {
            if (_caption != null)
            {
                return;
            }

            Transform existing = transform.Find("Caption");
            if (existing != null)
            {
                _caption = existing.GetComponent<Text>();
            }

            if (_caption == null)
            {
                var go = new GameObject("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(transform, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 32f);
                _caption = go.GetComponent<Text>();
            }

            _caption.font = GameFontProvider.Get();
            _caption.fontSize = 22;
            _caption.alignment = TextAnchor.MiddleCenter;
            _caption.color = Color.white;
            _caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            _caption.verticalOverflow = VerticalWrapMode.Truncate;
            _caption.raycastTarget = false;
            _caption.text = DefaultCaption;
        }

        private void EnsureRow()
        {
            if (_row != null)
            {
                return;
            }

            Transform existing = transform.Find("Row");
            if (existing is RectTransform existingRow)
            {
                _row = existingRow;
                ApplyRowLayout(_row);
                return;
            }

            var rowGo = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            _row = rowGo.GetComponent<RectTransform>();
            _row.SetParent(_root, false);
            ApplyRowLayout(_row);
        }

        private static void ApplyRowLayout(RectTransform row)
        {
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.offsetMin = new Vector2(8f, 8f);
            row.offsetMax = new Vector2(-8f, -36f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private GameObject CreateSlot(ThreatPreviewEntry entry, VisualDatabase visuals)
        {
            var slot = new GameObject($"Threat_{entry.EnemyId}", typeof(RectTransform));
            var slotRect = slot.GetComponent<RectTransform>();
            slotRect.SetParent(_row, false);
            slotRect.sizeDelta = new Vector2(108f, 100f);

            Sprite sprite = ResolveSprite(entry, visuals);
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.SetParent(slotRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -2f);
            iconRect.sizeDelta = new Vector2(48f, 48f);

            var image = iconGo.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = EnemyTypeIconPalette.ColorFor(entry.EnemyType);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(slotRect, false);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(0f, 48f);

            var label = labelGo.GetComponent<Text>();
            label.font = GameFontProvider.Get();
            label.fontSize = 18;
            label.alignment = TextAnchor.UpperCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            string typeName = EnemyTypeIconPalette.DisplayNameFor(entry.EnemyType);
            label.text = $"{typeName}\n×{Mathf.Max(1, entry.Count)}";

            return slot;
        }

        private static Sprite ResolveSprite(ThreatPreviewEntry entry, VisualDatabase visuals)
        {
            if (visuals != null
                && visuals.TryGetEnemyVisual(entry.EnemyId, out EnemyVisualSet set)
                && set != null)
            {
                Sprite portrait = set.GetMoveOrFallback();
                if (portrait != null)
                {
                    return portrait;
                }
            }

            return EnemyTypeIconPalette.SpriteFor(entry.EnemyType);
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                DestroyChild(_slots[i]);
            }

            _slots.Clear();
            if (_row == null)
            {
                return;
            }

            for (int i = _row.childCount - 1; i >= 0; i--)
            {
                DestroyChild(_row.GetChild(i).gameObject);
            }
        }

        private static void DestroyChild(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            go.transform.SetParent(null, false);
            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
