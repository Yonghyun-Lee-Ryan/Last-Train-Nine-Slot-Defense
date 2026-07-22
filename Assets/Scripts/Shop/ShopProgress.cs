using System;
using System.Collections.Generic;

namespace LastTrain.Shop
{
    public sealed class ShopProgress
    {
        public event Action Changed;

        private readonly List<ShopOffer> _offers = new();

        public bool IsActive { get; private set; }
        public bool IsResolved { get; private set; }
        public string StationId { get; private set; } = string.Empty;
        public int StationIndex { get; private set; }
        public IReadOnlyList<ShopOffer> Offers => _offers;

        public void Reset()
        {
            IsActive = false;
            IsResolved = false;
            StationId = string.Empty;
            StationIndex = 0;
            _offers.Clear();
            Changed?.Invoke();
        }

        public void Begin(string stationId, int stationIndex, IReadOnlyList<ShopOffer> offers)
        {
            IsActive = true;
            IsResolved = false;
            StationId = stationId ?? string.Empty;
            StationIndex = stationIndex;
            _offers.Clear();
            if (offers != null)
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i] != null)
                    {
                        _offers.Add(CloneOffer(offers[i]));
                    }
                }
            }

            Changed?.Invoke();
        }

        public void MarkPurchased(int offerIndex)
        {
            if (offerIndex < 0 || offerIndex >= _offers.Count)
            {
                return;
            }

            _offers[offerIndex].purchased = true;
            Changed?.Invoke();
        }

        public void Resolve()
        {
            IsActive = false;
            IsResolved = true;
            Changed?.Invoke();
        }

        public void Restore(string stationId, int stationIndex, bool isActive, bool isResolved, ShopOffer[] offers)
        {
            StationId = stationId ?? string.Empty;
            StationIndex = stationIndex;
            IsActive = isActive;
            IsResolved = isResolved;
            _offers.Clear();
            if (offers != null)
            {
                for (int i = 0; i < offers.Length; i++)
                {
                    if (offers[i] != null)
                    {
                        _offers.Add(CloneOffer(offers[i]));
                    }
                }
            }

            Changed?.Invoke();
        }

        private static ShopOffer CloneOffer(ShopOffer source)
        {
            return new ShopOffer
            {
                offerId = source.offerId,
                itemType = source.itemType,
                price = source.price,
                payloadId = source.payloadId,
                payloadValue = source.payloadValue,
                purchased = source.purchased,
            };
        }
    }
}
