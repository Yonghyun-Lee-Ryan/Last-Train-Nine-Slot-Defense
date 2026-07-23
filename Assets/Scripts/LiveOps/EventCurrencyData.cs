using UnityEngine;

namespace LastTrain.LiveOps
{
    [CreateAssetMenu(fileName = "EventCurrency_", menuName = "LastTrain/LiveOps/Event Currency Data")]
    public sealed class EventCurrencyData : ScriptableObject
    {
        [SerializeField] private string id = "event_token";
        [SerializeField] private string displayName = "이벤트 토큰";
        [SerializeField] private int maxBalance = 99999;

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxBalance => Mathf.Max(0, maxBalance);
    }
}
