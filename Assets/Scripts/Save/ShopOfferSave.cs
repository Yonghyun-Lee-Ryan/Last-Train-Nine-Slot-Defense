using System;

namespace LastTrain.Save
{
    [Serializable]
    public struct ShopOfferSave
    {
        public string offerId;
        public int itemType;
        public int price;
        public string payloadId;
        public int payloadValue;
        public bool purchased;
    }
}
