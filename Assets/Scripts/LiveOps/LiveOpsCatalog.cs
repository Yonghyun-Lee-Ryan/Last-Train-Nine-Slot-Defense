using UnityEngine;

namespace LastTrain.LiveOps
{
    /// <summary>Resources에서 로드하는 시즌/이벤트 카탈로그.</summary>
    [CreateAssetMenu(fileName = "LiveOpsCatalog", menuName = "LastTrain/LiveOps/Catalog")]
    public sealed class LiveOpsCatalog : ScriptableObject
    {
        [SerializeField] private SeasonData[] seasons = System.Array.Empty<SeasonData>();
        [SerializeField] private LiveEventData[] events = System.Array.Empty<LiveEventData>();

        public SeasonData[] Seasons => seasons ?? System.Array.Empty<SeasonData>();
        public LiveEventData[] Events => events ?? System.Array.Empty<LiveEventData>();
    }
}
