using System;
using LastTrain.Passenger;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>승객 상세·판매 팝업. RunState는 판매 서비스를 통해서만 변경한다.</summary>
    public sealed class PassengerDetailPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text starLabel;
        [SerializeField] private Text attackLabel;
        [SerializeField] private Text intervalLabel;
        [SerializeField] private Text rangeLabel;
        [SerializeField] private Text sellPriceLabel;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button closeButton;

        private RunState _runState;
        private int _slotIndex = -1;
        private Action<int> _onSold;
        private readonly UiInputGuard _inputGuard = new();

        public bool IsOpen => root != null && root.activeSelf;

        public void Initialize(RunState runState, Action<int> onSold)
        {
            _runState = runState;
            _onSold = onSold;

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

            Close();
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

            _slotIndex = slotIndex;
            if (nameLabel != null)
            {
                nameLabel.text = passenger.Data.GetDisplayNameAtStar(passenger.StarLevel);
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

            if (rangeLabel != null)
            {
                rangeLabel.text = $"사거리 {passenger.GetEffectiveRange():0.#}";
            }

            if (sellPriceLabel != null)
            {
                sellPriceLabel.text = $"판매가 {PassengerSellService.GetSellPrice(passenger)}";
            }

            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Close()
        {
            _slotIndex = -1;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void OnSellClicked()
        {
            if (!_inputGuard.TryAcquire() || _runState == null || _slotIndex < 0)
            {
                return;
            }

            int slot = _slotIndex;
            if (!PassengerSellService.TrySell(_runState, slot, out int coins))
            {
                return;
            }

            Close();
            _onSold?.Invoke(coins);
        }

        private void OnDestroy()
        {
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
