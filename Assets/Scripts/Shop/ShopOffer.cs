using System;

namespace LastTrain.Shop
{
    [Serializable]
    public sealed class ShopOffer
    {
        public string offerId = string.Empty;
        public ShopItemType itemType;
        public int price;
        public string payloadId = string.Empty;
        public int payloadValue;
        public bool purchased;
    }
}
