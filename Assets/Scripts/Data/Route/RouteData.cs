using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>순서가 있는 역 목록. 노선 구성 변경은 이 에셋만 수정하면 된다.</summary>
    [CreateAssetMenu(fileName = "Route_", menuName = "Last Train/Route Data")]
    public sealed class RouteData : ScriptableObject, IDataWithId
    {
        [SerializeField] private string id = RouteIds.Default;
        [SerializeField] private string displayName = "기본 노선";
        [SerializeField] private StationData[] stationsInOrder = System.Array.Empty<StationData>();
        [SerializeField] private float rewardMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public StationData[] StationsInOrder => stationsInOrder ?? System.Array.Empty<StationData>();
        public int StationCount => StationsInOrder.Length;
        public float RewardMultiplier => rewardMultiplier > 0.01f ? rewardMultiplier : 1f;

        public bool TryGetStationByIndex(int stationIndex, out StationData station)
        {
            station = null;
            if (stationIndex < 1 || stationsInOrder == null)
            {
                return false;
            }

            for (int i = 0; i < stationsInOrder.Length; i++)
            {
                StationData candidate = stationsInOrder[i];
                if (candidate != null && candidate.StationIndex == stationIndex)
                {
                    station = candidate;
                    return true;
                }
            }

            return false;
        }

        public StationData GetFirstStation()
        {
            if (stationsInOrder == null || stationsInOrder.Length == 0)
            {
                return null;
            }

            StationData best = null;
            int bestIndex = int.MaxValue;
            for (int i = 0; i < stationsInOrder.Length; i++)
            {
                StationData station = stationsInOrder[i];
                if (station == null)
                {
                    continue;
                }

                if (station.StationIndex < bestIndex)
                {
                    bestIndex = station.StationIndex;
                    best = station;
                }
            }

            return best;
        }
    }
}
