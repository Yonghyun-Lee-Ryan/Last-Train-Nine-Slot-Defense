using System;
using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Data;

namespace LastTrain.Passenger
{
    /// <summary>해금된 승객 풀에서 소환 후보를 생성한다.</summary>
    public sealed class PassengerOfferService
    {
        private readonly IReadOnlyList<PassengerData> _unlockedPassengers;
        private readonly RandomService _random;
        private readonly int _offerCount;

        public PassengerOfferService(
            IReadOnlyList<PassengerData> unlockedPassengers,
            RandomService random,
            int offerCount = 3)
        {
            _unlockedPassengers = unlockedPassengers ?? Array.Empty<PassengerData>();
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _offerCount = Math.Max(1, offerCount);
        }

        public IReadOnlyList<PassengerData> UnlockedPassengers => _unlockedPassengers;

        /// <summary>후보 offerCount개를 생성한다. 풀이 부족하면 중복 허용.</summary>
        public List<PassengerData> GenerateOffers()
        {
            var offers = new List<PassengerData>(_offerCount);
            if (_unlockedPassengers.Count == 0)
            {
                return offers;
            }

            if (_unlockedPassengers.Count == 1)
            {
                for (int i = 0; i < _offerCount; i++)
                {
                    offers.Add(_unlockedPassengers[0]);
                }

                return offers;
            }

            // 가능하면 중복 없이 뽑고, 부족하면 중복 허용
            var indices = new List<int>(_unlockedPassengers.Count);
            for (int i = 0; i < _unlockedPassengers.Count; i++)
            {
                if (_unlockedPassengers[i] != null)
                {
                    indices.Add(i);
                }
            }

            if (indices.Count == 0)
            {
                return offers;
            }

            Shuffle(indices);

            for (int i = 0; i < _offerCount; i++)
            {
                if (i < indices.Count)
                {
                    offers.Add(_unlockedPassengers[indices[i]]);
                }
                else
                {
                    int pick = indices[_random.Next(indices.Count)];
                    offers.Add(_unlockedPassengers[pick]);
                }
            }

            return offers;
        }

        private void Shuffle(List<int> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
