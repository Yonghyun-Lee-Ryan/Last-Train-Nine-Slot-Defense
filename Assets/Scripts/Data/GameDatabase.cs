using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 게임 전체 정적 데이터베이스.
    /// AppRoot 또는 Game Scene에서 참조하며 ID 기반 조회를 제공한다.
    /// OnValidate에서 카테고리별 중복 ID와 누락 참조를 검증한다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Last Train/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        [Header("Passengers")]
        [SerializeField] private PassengerData[] passengers;

        [Header("Enemies")]
        [SerializeField] private EnemyData[] enemies;

        [Header("Waves")]
        [SerializeField] private WaveData[] waves;

        [Header("Stations")]
        [SerializeField] private StationData[] stations;

        [Header("Abilities")]
        [SerializeField] private AbilityData[] abilities;

        [Header("Synergies")]
        [SerializeField] private SynergyData[] synergies;

        [Header("Relics")]
        [SerializeField] private RelicData[] relics;

        [Header("Difficulties")]
        [SerializeField] private Difficulty.DifficultyData[] difficulties;

        [Header("Routes")]
        [SerializeField] private RouteData[] routes;

        [Header("Events")]
        [SerializeField] private EventData[] events;

        [Header("Missions")]
        [SerializeField] private Mission.MissionData[] missions;

        [Header("Endless")]
        [SerializeField] private EndlessRouteData endlessRoute;

        [Header("Tutorial")]
        [SerializeField] private Tutorial.TutorialStepData[] tutorialSteps;

        public IReadOnlyList<PassengerData> Passengers => passengers;
        public IReadOnlyList<EnemyData> Enemies => enemies;
        public IReadOnlyList<WaveData> Waves => waves;
        public IReadOnlyList<StationData> Stations => stations;
        public IReadOnlyList<AbilityData> Abilities => abilities;
        public IReadOnlyList<SynergyData> Synergies => synergies;
        public IReadOnlyList<RelicData> Relics => relics;
        public IReadOnlyList<Difficulty.DifficultyData> Difficulties => difficulties;
        public IReadOnlyList<RouteData> Routes => routes;
        public IReadOnlyList<EventData> Events => events;
        public IReadOnlyList<Mission.MissionData> Missions => missions;
        public EndlessRouteData EndlessRoute => endlessRoute;
        public IReadOnlyList<Tutorial.TutorialStepData> TutorialSteps => tutorialSteps;

        public bool TryGetEvent(string id, out EventData data) =>
            TryFindById(events, id, out data);

        public bool TryGetPassenger(string id, out PassengerData data) =>
            TryFindById(passengers, id, out data);

        public bool TryGetEnemy(string id, out EnemyData data) =>
            TryFindById(enemies, id, out data);

        public bool TryGetWave(string id, out WaveData data) =>
            TryFindById(waves, id, out data);

        public bool TryGetStation(string id, out StationData data) =>
            TryFindById(stations, id, out data);

        public bool TryGetStationByIndex(int stationIndex, out StationData data)
        {
            data = null;
            if (stations == null)
            {
                return false;
            }

            for (int i = 0; i < stations.Length; i++)
            {
                StationData station = stations[i];
                if (station != null && station.StationIndex == stationIndex)
                {
                    data = station;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetAbility(string id, out AbilityData data) =>
            TryFindById(abilities, id, out data);

        public bool TryGetSynergy(string id, out SynergyData data) =>
            TryFindById(synergies, id, out data);

        public bool TryGetRelic(string id, out RelicData data) =>
            TryFindById(relics, id, out data);

        public bool TryGetDifficulty(string id, out Difficulty.DifficultyData data) =>
            TryFindById(difficulties, id, out data);

        public bool TryGetRoute(string routeId, out RouteData route)
        {
            route = null;
            if (!DataValidationUtility.IsValidId(routeId))
            {
                return false;
            }

            return TryFindById(routes, routeId, out route);
        }

        public bool TryGetStationByRouteIndex(string routeId, int stationIndex, out StationData data)
        {
            data = null;
            if (string.Equals(routeId, RouteIds.Endless, System.StringComparison.Ordinal)
                && endlessRoute != null)
            {
                return endlessRoute.TryGetStationByIndex(stationIndex, out data);
            }

            if (!TryGetRoute(routeId, out RouteData route) || route == null)
            {
                return false;
            }

            return route.TryGetStationByIndex(stationIndex, out data);
        }

        public int GetRouteStationCount(string routeId)
        {
            if (string.Equals(routeId, RouteIds.Endless, System.StringComparison.Ordinal)
                && endlessRoute != null)
            {
                return int.MaxValue / 4;
            }

            if (!TryGetRoute(routeId, out RouteData route) || route == null)
            {
                return stations != null ? stations.Length : 0;
            }

            return route.StationCount;
        }

        private static bool TryFindById<T>(T[] items, string id, out T data) where T : ScriptableObject, IDataWithId
        {
            data = null;
            if (items == null || !DataValidationUtility.IsValidId(id))
            {
                return false;
            }

            for (int i = 0; i < items.Length; i++)
            {
                T item = items[i];
                if (item != null && item.Id == id)
                {
                    data = item;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            ValidateCategory("Passenger", passengers);
            ValidateCategory("Enemy", enemies);
            ValidateCategory("Wave", waves);
            ValidateCategory("Station", stations);
            ValidateCategory("Ability", abilities);
            ValidateCategory("Synergy", synergies);
            ValidateCategory("Relic", relics);
            ValidateCategory("Difficulty", difficulties);
            ValidateCategory("Route", routes);
            ValidateCategory("Event", events);
            ValidateCategory("Mission", missions);

            ValidateStationIndices();
            ValidateAlphaContentCounts();
        }

        private void ValidateAlphaContentCounts()
        {
            // EditMode 테스트의 CreateInstance 인스턴스에서는 콘텐츠 수량 경고를 내지 않는다.
            if (!DataValidationUtility.IsPersistedProjectAsset(this))
            {
                return;
            }

            int passengerCount = CountNonNull(passengers);
            if (passengerCount < 8)
            {
                Debug.LogWarning(
                    $"[GameDatabase] 알파 콘텐츠 승객이 {passengerCount}/8 입니다. Unit 26 빌더를 실행하세요.",
                    this);
            }

            int enemyCount = CountNonNull(enemies);
            if (enemyCount < 6)
            {
                Debug.LogWarning(
                    $"[GameDatabase] 알파 콘텐츠 적이 {enemyCount}/6 입니다. Unit 26 빌더를 실행하세요.",
                    this);
            }
        }

        private static int CountNonNull<T>(T[] items) where T : class
        {
            if (items == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void ValidateCategory<T>(string categoryName, T[] items) where T : ScriptableObject, IDataWithId
        {
            if (items == null)
            {
                return;
            }

            var ids = new List<string>(items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                T item = items[i];
                if (item == null)
                {
                    Debug.LogWarning($"[GameDatabase] {categoryName}[{i}] 참조가 비어 있습니다.", this);
                    continue;
                }

                ids.Add(item.Id);
            }

            List<string> duplicates = DataValidationUtility.FindDuplicateIds(ids);
            for (int i = 0; i < duplicates.Count; i++)
            {
                Debug.LogWarning($"[GameDatabase] {categoryName} 중복 ID: '{duplicates[i]}'", this);
            }
        }

        private void ValidateStationIndices()
        {
            if (stations == null)
            {
                return;
            }

            var indices = new List<int>();
            for (int i = 0; i < stations.Length; i++)
            {
                StationData station = stations[i];
                if (station == null)
                {
                    continue;
                }

                if (indices.Contains(station.StationIndex))
                {
                    Debug.LogWarning(
                        $"[GameDatabase] Station stationIndex 중복: {station.StationIndex} ('{station.Id}')",
                        this);
                }
                else
                {
                    indices.Add(station.StationIndex);
                }
            }
        }
    }
}
