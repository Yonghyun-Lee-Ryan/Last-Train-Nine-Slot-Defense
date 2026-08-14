using System;
using LastTrain.Audio;
using LastTrain.Passenger;
using LastTrain.Passenger.Skills;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>승객 상세·판매 팝업. RunState는 판매 서비스를 통해서만 변경한다.</summary>
    public sealed class PassengerDetailPopup : MonoBehaviour
    {
        private const float LineStep = 44f;

        [SerializeField] private GameObject root;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text starLabel;
        [SerializeField] private Text attackLabel;
        [SerializeField] private Text intervalLabel;
        [SerializeField] private Text rangeLabel;
        [SerializeField] private Text sellPriceLabel;
        [SerializeField] private Text skillLabel;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button closeButton;

        private RunState _runState;
        private int _slotIndex = -1;
        private Action<int> _onSold;
        private Action _onClosed;
        private readonly UiInputGuard _inputGuard = new();
        private GameObject _sellConfirm;

        public bool IsOpen => root != null && root.activeSelf;

        public void Initialize(RunState runState, Action<int> onSold, Action onClosed = null)
        {
            _runState = runState;
            _onSold = onSold;
            _onClosed = onClosed;

            if (sellButton != null)
            {
                sellButton.onClick.RemoveListener(OnSellClicked);
                sellButton.onClick.AddListener(OnSellClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            EnsureSkillLabel();
            Close(playSfx: false);
        }

        public void Show(int slotIndex)
        {
            if (_runState == null || slotIndex < 0 || slotIndex >= RunState.GridSlotCount)
            {
                return;
            }

            PassengerRuntime passenger = _runState.GetPassengerAtSlot(slotIndex);
            if (passenger == null)
            {
                return;
            }

            EnsureSkillLabel();
            _slotIndex = slotIndex;

            string baseName = passenger.Data != null ? passenger.Data.DisplayName : "승객";
            string grade = passenger.Data != null ? passenger.Data.GetStarTitle(passenger.StarLevel) : string.Empty;

            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrWhiteSpace(grade) || string.Equals(grade, baseName, StringComparison.Ordinal)
                    ? baseName
                    : $"{baseName} · {grade}";
            }

            if (starLabel != null)
            {
                starLabel.text = $"{passenger.StarLevel}★";
            }

            if (attackLabel != null)
            {
                attackLabel.text = $"공격 {passenger.GetEffectiveAttack():0.#}";
            }

            if (intervalLabel != null)
            {
                intervalLabel.text = $"주기 {passenger.GetEffectiveAttackInterval():0.##}s";
            }

            if (skillLabel != null)
            {
                string skill = ResolveSkillName(passenger.Data != null ? passenger.Data.SkillId : null);
                skillLabel.text = string.IsNullOrEmpty(skill) ? "고유 스킬: 없음" : $"고유 스킬: {skill}";
                skillLabel.gameObject.SetActive(true);
            }

            if (rangeLabel != null)
            {
                rangeLabel.text = $"사거리 {passenger.GetEffectiveRange():0.#}";
            }

            if (sellPriceLabel != null)
            {
                sellPriceLabel.text = $"판매가 {PassengerSellService.GetSellPrice(passenger, _runState)}";
            }

            ApplyEvenTextSpacing();

            if (root != null)
            {
                root.SetActive(true);
            }

            GameAudio.PlaySfx(SfxId.UiOpen);
        }

        public void Close()
        {
            Close(playSfx: true);
        }

        public void Close(bool playSfx)
        {
            bool wasOpen = IsOpen;
            _slotIndex = -1;
            if (root != null)
            {
                root.SetActive(false);
            }

            if (playSfx && wasOpen)
            {
                GameAudio.PlaySfx(SfxId.UiClose);
            }

            if (wasOpen)
            {
                _onClosed?.Invoke();
            }

            DestroySellConfirm();
        }

        private void DestroySellConfirm()
        {
            if (_sellConfirm != null)
            {
                Destroy(_sellConfirm);
                _sellConfirm = null;
            }
        }

        private void ApplyEvenTextSpacing()
        {
            Text[] lines =
            {
                nameLabel,
                starLabel,
                attackLabel,
                intervalLabel,
                skillLabel,
                rangeLabel,
                sellPriceLabel,
            };

            float y = 190f;
            for (int i = 0; i < lines.Length; i++)
            {
                Text label = lines[i];
                if (label == null || !label.gameObject.activeSelf)
                {
                    continue;
                }

                RectTransform rect = label.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(520f, 40f);
                rect.anchoredPosition = new Vector2(0f, y);
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.fontSize = i == 0 ? 34 : 26;
                y -= LineStep;
            }
        }

        private void EnsureSkillLabel()
        {
            if (skillLabel != null || root == null)
            {
                return;
            }

            Transform existing = root.transform.Find("SkillLabel");
            if (existing != null)
            {
                skillLabel = existing.GetComponent<Text>();
                return;
            }

            var go = new GameObject("SkillLabel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(root.transform, false);
            skillLabel = go.GetComponent<Text>();
            skillLabel.font = GameFontProvider.Get();
            skillLabel.color = Color.white;
            skillLabel.raycastTarget = false;
        }

        private static string ResolveSkillName(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return string.Empty;
            }

            return skillId switch
            {
                PassengerSkillIds.Knockback => "넉백",
                PassengerSkillIds.TrainHeal => "객차 회복",
                PassengerSkillIds.TemporaryTurret => "임시 터렛",
                PassengerSkillIds.CriticalAreaDamage => "범위 치명타",
                PassengerSkillIds.PaperThrow => "빠른 손놀림",
                PassengerSkillIds.LowHpBonus => "저체력 보정",
                PassengerSkillIds.BossInterrupt => "보스 방해",
                PassengerSkillIds.LuckyCrit => "행운 치명타",
                PassengerSkillIds.ChainZap => "환승 연쇄",
                PassengerSkillIds.ScaldSplash => "핫 스플래시",
                PassengerSkillIds.PerimeterPulse => "경계 펄스",
                PassengerSkillIds.FocusShot => "집중 사격",
                _ => skillId,
            };
        }

        private void OnSellClicked()
        {
            if (!_inputGuard.TryAcquire() || _runState == null || _slotIndex < 0)
            {
                return;
            }

            PassengerRuntime passenger = _runState.GetPassengerAtSlot(_slotIndex);
            if (passenger == null)
            {
                return;
            }

            int price = PassengerSellService.GetSellPrice(passenger, _runState);
            string name = passenger.Data != null ? passenger.Data.DisplayName : "승객";
            ShowSellConfirm(name, price);
        }

        private void ShowSellConfirm(string passengerName, int coins)
        {
            DestroySellConfirm();

            GameObject overlay = MenuOverlayUi.CreateRoot("SellConfirm", sortingOrder: 4300);
            _sellConfirm = overlay;
            MenuOverlayUi.CreateFullScreenDim(overlay.transform, new Color(0f, 0f, 0f, 0.7f), DestroySellConfirm);
            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(overlay.transform);
            GameObject box = MenuOverlayUi.CreateCenteredPanel(
                host,
                "Box",
                new Vector2(680f, 360f),
                MenuOverlayUi.OverlayFill);
            MenuOverlayUi.EnableClipping(box);

            Text title = MenuOverlayUi.CreateText(box.transform, "Title", "판매 확인", 32, TextAnchor.MiddleCenter);
            title.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-80f, 48f);
            titleRect.anchoredPosition = new Vector2(0f, -36f);

            Text body = MenuOverlayUi.CreateText(
                box.transform,
                "Body",
                $"{passengerName}을(를) {coins} 코인에 판매할까요?",
                24,
                TextAnchor.MiddleCenter);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(40f, 100f);
            bodyRect.offsetMax = new Vector2(-40f, -92f);

            MenuOverlayUi.CreateButton(
                box.transform,
                "Cancel",
                "취소",
                new Vector2(-140f, -118f),
                new Vector2(220f, 56f),
                () => DestroySellConfirm());
            MenuOverlayUi.CreateButton(
                box.transform,
                "Confirm",
                "판매",
                new Vector2(140f, -118f),
                new Vector2(220f, 56f),
                () =>
                {
                    int slot = _slotIndex;
                    DestroySellConfirm();
                    ConfirmSell(slot);
                });
        }

        private void ConfirmSell(int slot)
        {
            if (_runState == null || slot < 0)
            {
                return;
            }

            if (!PassengerSellService.TrySell(_runState, slot, out int coins))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            Close(playSfx: false);
            _onSold?.Invoke(coins);
        }

        private void OnDestroy()
        {
            DestroySellConfirm();
            if (sellButton != null)
            {
                sellButton.onClick.RemoveListener(OnSellClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }
    }
}
