using LastTrain.Core;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Ux
{
    /// <summary>코인/슬롯/등급 등 플레이어 안내를 한곳에서 표시.</summary>
    public static class UxGuidanceService
    {
        public static void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Debug.Log($"[UX] {message}");
            BattleHudController hud = Object.FindAnyObjectByType<BattleHudController>();
            hud?.ShowStatusMessage(message);
        }

        public static void ShowSummonResult(SummonRequestResult result)
        {
            switch (result)
            {
                case SummonRequestResult.NotEnoughCoins:
                    Show("코인이 부족합니다.");
                    break;
                case SummonRequestResult.NoEmptySlot:
                    Show("빈 슬롯이 없습니다.");
                    break;
                case SummonRequestResult.OfferAlreadyOpen:
                    Show("이미 소환 후보가 열려 있습니다.");
                    break;
            }
        }

        public static void ShowMaxStar()
        {
            Show("이미 최대 등급입니다.");
        }
    }

    /// <summary>합성 가능한 동일 승객 슬롯을 강조한다.</summary>
    public static class MergeHighlightService
    {
        public static void Refresh(GridManager grid, RunState runState)
        {
            Clear(grid);
            if (grid == null || runState == null)
            {
                return;
            }

            for (int a = 0; a < RunState.GridSlotCount; a++)
            {
                PassengerRuntime pa = runState.GetPassengerAtSlot(a);
                if (pa == null)
                {
                    continue;
                }

                for (int b = a + 1; b < RunState.GridSlotCount; b++)
                {
                    PassengerRuntime pb = runState.GetPassengerAtSlot(b);
                    if (pb == null || !MergeService.CanMerge(pa, pb))
                    {
                        continue;
                    }

                    HighlightSlot(grid, a, true);
                    HighlightSlot(grid, b, true);
                }
            }
        }

        public static void Clear(GridManager grid)
        {
            if (grid?.Slots == null)
            {
                return;
            }

            for (int i = 0; i < grid.Slots.Count; i++)
            {
                HighlightSlot(grid, i, false);
            }
        }

        private static void HighlightSlot(GridManager grid, int index, bool on)
        {
            GridSlot slot = grid.GetSlot(index);
            if (slot == null)
            {
                return;
            }

            Image image = slot.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.color = on
                ? new Color(0.45f, 0.95f, 0.55f, 0.85f)
                : Color.white;
        }
    }
}
