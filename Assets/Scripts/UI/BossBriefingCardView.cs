using LastTrain.Battle;
using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// 보스 역 페이즈·위협 아이콘 카드. 전투 HUD에는 올리지 않는다
    /// (중앙의 색 도형만 떠서 경로 화살표·그리드를 가리기 때문).
    /// </summary>
    public sealed class BossBriefingCardView : MonoBehaviour
    {
        public const string RootName = "BossBriefingCard";

        private GameObject _card;
        private GameObject _dim;
        private RectTransform _phaseRow;
        private RectTransform _threatRow;
        private int _phaseSegmentCount;
        private int _threatIconCount;

        public bool IsShowing => _card != null && _card.activeSelf && gameObject.activeSelf;
        public bool IsDimBlocking =>
            gameObject.activeSelf
            && _dim != null
            && _dim.activeSelf
            && (_card == null || !_card.activeSelf);
        public int PhaseSegmentCount => _phaseSegmentCount;
        public int ThreatIconCount => _threatIconCount;

        public static BossBriefingCardView Ensure(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(RootName);
            BossBriefingCardView view = existing != null
                ? existing.GetComponent<BossBriefingCardView>()
                : null;
            if (view == null)
            {
                var go = new GameObject(RootName, typeof(RectTransform), typeof(BossBriefingCardView));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.SetAsLastSibling();
                view = go.GetComponent<BossBriefingCardView>();
            }

            view.EnsureCard();
            view.Hide();
            return view;
        }

        public void Show(BossPhaseBriefing briefing)
        {
            EnsureCard();
            ClearRows();
            if (briefing == null || !briefing.ShouldShow)
            {
                Hide();
                return;
            }

            _card.SetActive(true);
            if (_dim != null)
            {
                _dim.SetActive(true);
            }

            gameObject.SetActive(true);
            BindPhases(briefing);
            BindThreats(briefing);
            if (Application.isPlaying)
            {
                CancelInvoke(nameof(Hide));
                Invoke(nameof(Hide), 3.6f);
            }
        }

        public void Hide()
        {
            CancelInvoke(nameof(Hide));
            if (_card != null)
            {
                _card.SetActive(false);
            }

            if (_dim != null)
            {
                _dim.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        private void EnsureCard()
        {
            if (_card != null)
            {
                if (_dim == null)
                {
                    Transform existingDim = transform.Find("Dim");
                    if (existingDim != null)
                    {
                        _dim = existingDim.gameObject;
                    }
                }

                return;
            }

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform dimRect = dim.GetComponent<RectTransform>();
            dimRect.SetParent(transform, false);
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.45f);
            dim.GetComponent<Button>().transition = Selectable.Transition.None;
            dim.GetComponent<Button>().onClick.AddListener(Hide);
            _dim = dim;

            _card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform cardRect = _card.GetComponent<RectTransform>();
            cardRect.SetParent(transform, false);
            cardRect.anchorMin = new Vector2(0.12f, 0.38f);
            cardRect.anchorMax = new Vector2(0.88f, 0.68f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            _card.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

            Button cardButton = _card.AddComponent<Button>();
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.AddListener(Hide);

            _phaseRow = CreateRow(_card.transform, "PhaseRow", new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.82f));
            _threatRow = CreateRow(_card.transform, "ThreatRow", new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.46f));
        }

        private static RectTransform CreateRow(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 16f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 8, 8);
            return rect;
        }

        private void BindPhases(BossPhaseBriefing briefing)
        {
            if (briefing.Segments == null)
            {
                return;
            }

            for (int i = 0; i < briefing.Segments.Count; i++)
            {
                BossPhaseSegment segment = briefing.Segments[i];
                var go = new GameObject($"Phase_{segment.Phase}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(_phaseRow, false);
                var image = go.GetComponent<Image>();
                image.sprite = UiProceduralSprites.RoundedSquare();
                image.color = EnemyTypeIconPalette.PhaseColor(segment.Phase);
                image.preserveAspect = true;
                image.raycastTarget = false;
                var layout = go.GetComponent<LayoutElement>();
                layout.preferredWidth = 48f;
                layout.preferredHeight = 36f;
                layout.flexibleWidth = Mathf.Max(0.08f, segment.Span);
                layout.minHeight = 28f;
                _phaseSegmentCount++;
            }
        }

        private void BindThreats(BossPhaseBriefing briefing)
        {
            if (briefing.ThreatTypes == null || briefing.ThreatTypes.Count == 0)
            {
                var bossOnly = new GameObject("Threat_Boss", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                bossOnly.transform.SetParent(_threatRow, false);
                var image = bossOnly.GetComponent<Image>();
                image.sprite = EnemyTypeIconPalette.SpriteFor(EnemyType.Boss);
                image.color = EnemyTypeIconPalette.ColorFor(EnemyType.Boss);
                image.preserveAspect = true;
                image.raycastTarget = false;
                var iconLayout = bossOnly.GetComponent<LayoutElement>();
                iconLayout.preferredWidth = 48f;
                iconLayout.preferredHeight = 48f;
                iconLayout.minWidth = 48f;
                iconLayout.minHeight = 48f;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;
                _threatIconCount = 1;
                return;
            }

            for (int i = 0; i < briefing.ThreatTypes.Count; i++)
            {
                EnemyType type = briefing.ThreatTypes[i];
                var go = new GameObject($"Threat_{type}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(_threatRow, false);
                var image = go.GetComponent<Image>();
                image.sprite = EnemyTypeIconPalette.SpriteFor(type);
                image.color = EnemyTypeIconPalette.ColorFor(type);
                image.preserveAspect = true;
                image.raycastTarget = false;
                var iconLayout = go.GetComponent<LayoutElement>();
                iconLayout.preferredWidth = 48f;
                iconLayout.preferredHeight = 48f;
                iconLayout.minWidth = 48f;
                iconLayout.minHeight = 48f;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;
                _threatIconCount++;
            }
        }

        private void ClearRows()
        {
            _phaseSegmentCount = 0;
            _threatIconCount = 0;
            ClearChildren(_phaseRow);
            ClearChildren(_threatRow);
        }

        private static void ClearChildren(RectTransform row)
        {
            if (row == null)
            {
                return;
            }

            for (int i = row.childCount - 1; i >= 0; i--)
            {
                GameObject child = row.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

        private void OnDestroy()
        {
            CancelInvoke();
        }
    }
}
