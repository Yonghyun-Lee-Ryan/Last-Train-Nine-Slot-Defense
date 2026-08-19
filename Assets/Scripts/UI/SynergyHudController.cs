using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>시너지 카탈로그 전체 표시(활성/비활성 대비). SynergyProgress.Changed 구독.</summary>
    [DefaultExecutionOrder(70)]
    public sealed class SynergyHudController : MonoBehaviour
    {
        public const float LabelMinHeight = 200f;
        public const int LabelFontSize = 16;

        [SerializeField] private Text synergyLabel;
        [SerializeField] private GameBattleBootstrap battleBootstrap;

        private RunState _runState;

        private void Start()
        {
            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null || !appRoot.GameSession.HasActiveRun)
            {
                return;
            }

            _runState = appRoot.GameSession.RunState;
            if (synergyLabel == null)
            {
                EnsureLabel();
            }

            ApplyLabelStyle(synergyLabel);

            if (battleBootstrap == null)
            {
                battleBootstrap = FindAnyObjectByType<GameBattleBootstrap>();
            }

            if (_runState.Synergies != null)
            {
                _runState.Synergies.Changed += Refresh;
            }

            if (_runState.Battle != null)
            {
                _runState.Battle.PhaseChanged += HandlePhaseChanged;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_runState?.Synergies != null)
            {
                _runState.Synergies.Changed -= Refresh;
            }

            if (_runState?.Battle != null)
            {
                _runState.Battle.PhaseChanged -= HandlePhaseChanged;
            }
        }

        public void RefreshLayout()
        {
            ApplyLayout();
        }

        private void HandlePhaseChanged(RunPhase _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (synergyLabel == null || _runState?.Synergies == null)
            {
                return;
            }

            RunPhase phase = _runState.Battle != null ? _runState.Battle.CurrentPhase : RunPhase.None;
            bool show = CombatTopHudLayout.ShouldShowSideChrome(phase);
            synergyLabel.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            synergyLabel.supportRichText = true;
            synergyLabel.text = SynergyHudFormatter.Format(_runState.Synergies.Catalog, _runState);
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (synergyLabel == null)
            {
                return;
            }

            WaveThreatTickerView ticker = FindAnyObjectByType<WaveThreatTickerView>();
            bool threatVisible = ticker != null && ticker.IsShowing;
            float top = CombatTopHudLayout.GetSynergyTop(threatVisible);

            RectTransform rect = synergyLabel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(CombatTopHudLayout.SynergyLeftX, top);
            rect.sizeDelta = new Vector2(CombatTopHudLayout.SynergyWidth, CombatTopHudLayout.SynergyMaxHeight);
        }

        private void EnsureLabel()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform parent = canvas.transform.Find("SafeArea") ?? canvas.transform;
            Transform existing = parent.Find("SynergyListLabel");
            if (existing != null)
            {
                synergyLabel = existing.GetComponent<Text>();
                ApplyLabelStyle(synergyLabel);
                return;
            }

            var go = new GameObject("SynergyListLabel", typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            synergyLabel = go.GetComponent<Text>();
            ApplyLabelStyle(synergyLabel);
            ApplyLayout();
        }

        private static void ApplyLabelStyle(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.fontSize = LabelFontSize;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.font = GameFontProvider.Get();
            label.raycastTarget = false;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.lineSpacing = 1f;
        }
    }
}
