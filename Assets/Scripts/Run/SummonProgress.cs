using System;
using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Run
{
    /// <summary>회차 내 소환·리롤 진행 상태.</summary>
    public sealed class SummonProgress
    {
        public event Action OffersChanged;
        public event Action CostsChanged;

        private readonly List<PassengerData> _currentOffers = new();

        public int PaidSummonCount { get; private set; }
        public int FreeRerollsUsed { get; private set; }
        public int AdRerollsUsed { get; private set; }
        public bool HasActiveOffers => _currentOffers.Count > 0;
        public IReadOnlyList<PassengerData> CurrentOffers => _currentOffers;

        public void Reset()
        {
            PaidSummonCount = 0;
            FreeRerollsUsed = 0;
            AdRerollsUsed = 0;
            ClearOffers();
        }

        public void RecordPaidSummon()
        {
            PaidSummonCount++;
            CostsChanged?.Invoke();
        }

        public void RecordFreeReroll()
        {
            FreeRerollsUsed++;
            CostsChanged?.Invoke();
        }

        public void RecordAdReroll()
        {
            AdRerollsUsed++;
            CostsChanged?.Invoke();
        }

        public void SetOffers(IReadOnlyList<PassengerData> offers)
        {
            _currentOffers.Clear();
            if (offers != null)
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i] != null)
                    {
                        _currentOffers.Add(offers[i]);
                    }
                }
            }

            OffersChanged?.Invoke();
        }

        public void ClearOffers()
        {
            if (_currentOffers.Count == 0)
            {
                return;
            }

            _currentOffers.Clear();
            OffersChanged?.Invoke();
        }

        public PassengerData GetOffer(int index)
        {
            if (index < 0 || index >= _currentOffers.Count)
            {
                return null;
            }

            return _currentOffers[index];
        }
    }
}
