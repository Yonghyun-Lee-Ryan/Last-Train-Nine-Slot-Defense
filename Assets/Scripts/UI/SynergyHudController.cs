using System.Text;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>활성 시너지 목록 표시. SynergyProgress.Changed를 구독한다.</summary>
    [DefaultExecutionOrder(70)]
    public sealed class SynergyHudController : MonoBehaviour
    {
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

            if (battleBootstrap == null)
            {
                battleBootstrap = FindAnyObjectByType<GameBattleBootstrap>();
            }

            if (_runState.Synergies != null)
            {
                _runState.Synergies.Changed += Refresh;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_runState?.Synergies != null)
            {
                _runState.Synergies.Changed -= Refresh;
            }
        }

        private void Refresh()
        {
            if (synergyLabel == null || _runState?.Synergies == null)
            {
                return;
            }

            var active = _runState.Synergies.Active;
            if (active.Count == 0)
            {
                synergyLabel.text = "시너지: 없음";
                return;
            }

            var sb = new StringBuilder("시너지: ");
            for (int i = 0; i < active.Count; i++)
            {
                SynergyData data = active[i];
                if (data == null)
                {
                    continue;
                }

                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(data.DisplayName);
            }

            synergyLabel.text = sb.ToString();
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
                return;
            }

            var go = new GameObject("SynergyListLabel", typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(1000f, 40f);

            synergyLabel = go.GetComponent<Text>();
            synergyLabel.fontSize = 22;
            synergyLabel.alignment = TextAnchor.MiddleCenter;
            synergyLabel.color = Color.white;
            synergyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                                ?? Font.CreateDynamicFontFromOSFont("Malgun Gothic", 22);
            synergyLabel.raycastTarget = false;
        }
    }
}
