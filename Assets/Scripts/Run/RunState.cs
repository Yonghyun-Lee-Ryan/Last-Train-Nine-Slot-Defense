using System;
using System.Collections.Generic;

namespace LastTrain.Run
{
    /// <summary>
    /// 한 회차의 모든 변경 가능 상태를 보유한다.
    /// ScriptableObject 원본 데이터는 참조만 하며 수정하지 않는다.
    /// </summary>
    public sealed class RunState
    {
        public const int GridSlotCount = 9;

        public event Action TrainDestroyed;

        public string RunId { get; private set; } = string.Empty;
        public string LineId { get; private set; } = string.Empty;
        public TrainState Train { get; private set; }
        public CurrencyState Currency { get; private set; }
        public BattleState Battle { get; private set; }
        public StationProgress Station { get; private set; }
        public RunHistory History { get; private set; }
        public SummonProgress Summon { get; private set; }
        public AbilityProgress Abilities { get; private set; }
        public SynergyProgress Synergies { get; private set; }
        public int BaseTrainMaxHp { get; private set; }

        private readonly PassengerRuntime[] _gridSlots = new PassengerRuntime[GridSlotCount];
        private readonly List<PassengerRuntime> _allPassengers = new();

        public IReadOnlyList<PassengerRuntime> AllPassengers => _allPassengers;

        public void Initialize(RunStartConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RunId = Guid.NewGuid().ToString("N");
            LineId = config.LineId ?? "line1";

            ClearGridInternal();

            BaseTrainMaxHp = config.InitialTrainMaxHp;
            Train = new TrainState(config.InitialTrainMaxHp, config.InitialTrainCurrentHp);
            Currency = new CurrencyState(config.InitialCoins);
            Battle = new BattleState();
            Station = new StationProgress();
            History = new RunHistory();
            Summon = new SummonProgress();
            Summon.Reset();
            Abilities = new AbilityProgress();
            Abilities.Reset();
            Synergies = new SynergyProgress();
            Synergies.Reset();

            Station.Initialize(config.InitialStationIndex);

            Train.Destroyed += HandleTrainDestroyed;
        }

        public PassengerRuntime GetPassengerAtSlot(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return _gridSlots[slotIndex];
        }

        public bool IsSlotEmpty(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return _gridSlots[slotIndex] == null;
        }

        public int FindFirstEmptySlot()
        {
            for (int i = 0; i < GridSlotCount; i++)
            {
                if (_gridSlots[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool HasEmptySlot()
        {
            return FindFirstEmptySlot() >= 0;
        }

        /// <summary>빈 슬롯에 승객을 배치한다.</summary>
        public bool TryPlacePassenger(int slotIndex, PassengerRuntime passenger)
        {
            ValidateSlotIndex(slotIndex);
            if (passenger == null)
            {
                throw new ArgumentNullException(nameof(passenger));
            }

            if (_gridSlots[slotIndex] != null)
            {
                return false;
            }

            if (passenger.GridSlotIndex >= 0 && _gridSlots[passenger.GridSlotIndex] == passenger)
            {
                _gridSlots[passenger.GridSlotIndex] = null;
            }

            _gridSlots[slotIndex] = passenger;
            passenger.GridSlotIndex = slotIndex;

            if (!_allPassengers.Contains(passenger))
            {
                _allPassengers.Add(passenger);
                History.RecordSummon(passenger.StarLevel);
            }

            return true;
        }

        /// <summary>슬롯에서 승객을 제거한다. AllPassengers 목록은 유지한다.</summary>
        public bool TryRemovePassenger(int slotIndex, out PassengerRuntime removed)
        {
            ValidateSlotIndex(slotIndex);
            removed = _gridSlots[slotIndex];
            if (removed == null)
            {
                return false;
            }

            _gridSlots[slotIndex] = null;
            removed.GridSlotIndex = -1;
            return true;
        }

        /// <summary>
        /// 합성·판매 등으로 회차에서 승객을 완전히 제거한다.
        /// Grid와 AllPassengers에서 모두 제거한다.
        /// </summary>
        public bool TryConsumePassenger(int slotIndex, out PassengerRuntime removed)
        {
            if (!TryRemovePassenger(slotIndex, out removed))
            {
                return false;
            }

            _allPassengers.Remove(removed);
            return true;
        }

        /// <summary>두 슬롯의 승객 위치를 교환한다. 한쪽만 비어 있으면 이동한다.</summary>
        public bool TrySwapSlots(int slotA, int slotB)
        {
            ValidateSlotIndex(slotA);
            ValidateSlotIndex(slotB);

            if (slotA == slotB)
            {
                return true;
            }

            PassengerRuntime passengerA = _gridSlots[slotA];
            PassengerRuntime passengerB = _gridSlots[slotB];

            _gridSlots[slotA] = passengerB;
            _gridSlots[slotB] = passengerA;

            if (passengerA != null)
            {
                passengerA.GridSlotIndex = slotB;
            }

            if (passengerB != null)
            {
                passengerB.GridSlotIndex = slotA;
            }

            return true;
        }

        public void RecordEnemyKill(int coinReward)
        {
            History.RecordEnemyKill();
            Currency.AddCoins(coinReward);
        }

        public void RecordMerge(int resultingStarLevel)
        {
            History.RecordMerge(resultingStarLevel);
        }

        public void RecordPassengerSold()
        {
            History.RecordSell();
        }

        public RunResult BuildResult(RunEndReason endReason, bool isVictory)
        {
            if (Train == null || Currency == null || Station == null || History == null)
            {
                throw new InvalidOperationException("RunState가 초기화되지 않았습니다.");
            }

            return new RunResult(
                RunId,
                LineId,
                isVictory,
                endReason,
                Station.CurrentStationIndex,
                Station.CompletedStationCount,
                History.EnemiesKilled,
                History.MergeCount,
                History.HighestPassengerStar,
                Train.CurrentHp,
                Train.MaxHp,
                Currency.CurrentCoins,
                Currency.TotalEarned,
                Currency.TotalSpent,
                History.PassengersSummoned,
                History.PassengersSold,
                History.AbilityCardsSelected);
        }

        public void Dispose()
        {
            if (Train != null)
            {
                Train.Destroyed -= HandleTrainDestroyed;
            }
        }

        private void HandleTrainDestroyed()
        {
            TrainDestroyed?.Invoke();
        }

        private void ClearGridInternal()
        {
            for (int i = 0; i < GridSlotCount; i++)
            {
                _gridSlots[i] = null;
            }

            _allPassengers.Clear();
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= GridSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
        }
    }
}
