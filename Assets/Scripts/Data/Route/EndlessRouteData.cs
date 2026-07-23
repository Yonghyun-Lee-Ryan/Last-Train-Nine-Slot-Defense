using System.Collections.Generic;
using LastTrain.Difficulty;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>
    /// 무한 모드 노선. 지정 Station 패턴을 반복하고 역 번호에 따라 난이도를 올린다.
    /// </summary>
    [CreateAssetMenu(fileName = "EndlessRoute_", menuName = "Last Train/Endless Route Data")]
    public sealed class EndlessRouteData : ScriptableObject, IDataWithId
    {
        [Header("Identity")]
        [SerializeField] private string id = RouteIds.Endless;
        [SerializeField] private string displayName = "무한 노선";

        [Header("Pattern")]
        [Tooltip("반복할 전투 역 템플릿 (상점/이벤트 제외 가능)")]
        [SerializeField] private StationData[] patternStations = System.Array.Empty<StationData>();
        [Tooltip("보스 역 템플릿. 비어 있으면 패턴 내 Boss 타입을 사용")]
        [SerializeField] private StationData bossStationTemplate;

        [Header("Scaling")]
        [SerializeField] private int bossInterval = 5;
        [SerializeField] private float difficultyGrowthPerStation = 0.08f;
        [SerializeField] private float bossDifficultyBonus = 0.35f;
        [SerializeField] private int maxPassengerStarLevel = 3;

        [Header("Depth Modifiers")]
        [Tooltip("StationIndexMin 기준으로 일정 역마다 활성화")]
        [SerializeField] private DifficultyModifierData[] depthModifiers = System.Array.Empty<DifficultyModifierData>();

        private readonly Dictionary<int, StationData> _runtimeCache = new();
        private StationData[] _combatPatternCache;

        public string Id => string.IsNullOrWhiteSpace(id) ? RouteIds.Endless : id;
        public string DisplayName => displayName;
        public int BossInterval => Mathf.Max(1, bossInterval);
        public float DifficultyGrowthPerStation => Mathf.Max(0f, difficultyGrowthPerStation);
        public float BossDifficultyBonus => Mathf.Max(0f, bossDifficultyBonus);
        public int MaxPassengerStarLevel => Mathf.Max(1, maxPassengerStarLevel);
        public IReadOnlyList<StationData> PatternStations => patternStations ?? System.Array.Empty<StationData>();
        public StationData BossStationTemplate => bossStationTemplate;
        public IReadOnlyList<DifficultyModifierData> DepthModifiers =>
            depthModifiers ?? System.Array.Empty<DifficultyModifierData>();

        public bool IsBossStation(int stationIndex)
        {
            return stationIndex > 0 && stationIndex % BossInterval == 0;
        }

        public float ComputeDifficultyMultiplier(int stationIndex, bool isBoss)
        {
            float growth = 1f + (Mathf.Max(1, stationIndex) - 1) * DifficultyGrowthPerStation;
            if (isBoss)
            {
                growth += BossDifficultyBonus;
            }

            return Mathf.Max(0.1f, growth);
        }

        public bool TryGetStationByIndex(int stationIndex, out StationData station)
        {
            station = null;
            if (stationIndex < 1)
            {
                return false;
            }

            if (_runtimeCache.TryGetValue(stationIndex, out StationData cached) && cached != null)
            {
                station = cached;
                return true;
            }

            StationData template = ResolveTemplate(stationIndex, out bool isBoss, out StationType type);
            if (template == null)
            {
                return false;
            }

            float difficulty = ComputeDifficultyMultiplier(stationIndex, isBoss);
            string name = isBoss
                ? $"{stationIndex}번째 역 (보스)"
                : $"{stationIndex}번째 역";

            station = StationData.CreateRuntimeClone(template, stationIndex, difficulty, type, name);
            _runtimeCache[stationIndex] = station;
            return station != null;
        }

        public StationData GetFirstStation()
        {
            return TryGetStationByIndex(1, out StationData station) ? station : null;
        }

        public void ClearRuntimeCache()
        {
            _runtimeCache.Clear();
            _combatPatternCache = null;
        }

        /// <summary>오래된 런타임 역 클론을 제거해 장시간 무한 모드 메모리 증가를 막는다.</summary>
        public void PruneRuntimeCache(int keepFromStationIndex, int keepWindow = 8)
        {
            if (_runtimeCache.Count == 0)
            {
                return;
            }

            int minKeep = Mathf.Max(1, keepFromStationIndex - Mathf.Max(1, keepWindow));
            var removeKeys = new List<int>();
            foreach (KeyValuePair<int, StationData> pair in _runtimeCache)
            {
                if (pair.Key < minKeep)
                {
                    removeKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeKeys.Count; i++)
            {
                _runtimeCache.Remove(removeKeys[i]);
            }
        }

        public int RuntimeCacheCount => _runtimeCache.Count;

#if UNITY_EDITOR
        public void EditorSet(
            string newId,
            string newDisplayName,
            StationData[] pattern,
            StationData bossTemplate,
            int interval,
            float growth,
            float bossBonus,
            DifficultyModifierData[] modifiers)
        {
            id = newId;
            displayName = newDisplayName;
            patternStations = pattern ?? System.Array.Empty<StationData>();
            bossStationTemplate = bossTemplate;
            bossInterval = Mathf.Max(1, interval);
            difficultyGrowthPerStation = Mathf.Max(0f, growth);
            bossDifficultyBonus = Mathf.Max(0f, bossBonus);
            depthModifiers = modifiers ?? System.Array.Empty<DifficultyModifierData>();
            maxPassengerStarLevel = 3;
            ClearRuntimeCache();
        }
#endif

        private StationData ResolveTemplate(int stationIndex, out bool isBoss, out StationType type)
        {
            isBoss = IsBossStation(stationIndex);
            if (isBoss)
            {
                type = StationType.Boss;
                if (bossStationTemplate != null)
                {
                    return bossStationTemplate;
                }

                StationData bossInPattern = FindFirstOfType(StationType.Boss);
                if (bossInPattern != null)
                {
                    return bossInPattern;
                }
            }

            StationData[] pattern = patternStations;
            if (pattern == null || pattern.Length == 0)
            {
                type = StationType.Normal;
                return null;
            }

            // 보스 슬롯이 아닌 패턴만 순환 (보스 템플릿이 패턴에 섞여 있어도 전투 역 유지)
            StationData[] combat = GetOrBuildCombatPattern();
            if (combat.Length == 0)
            {
                type = StationType.Normal;
                return null;
            }

            int cycleIndex = (stationIndex - 1) % combat.Length;
            if (isBoss)
            {
                cycleIndex = (stationIndex / BossInterval - 1) % combat.Length;
            }

            StationData picked = combat[Mathf.Abs(cycleIndex) % combat.Length];
            type = isBoss ? StationType.Boss : (picked != null ? picked.StationType : StationType.Normal);
            if (type == StationType.Boss && !isBoss)
            {
                type = StationType.Normal;
            }

            return picked;
        }

        private StationData[] GetOrBuildCombatPattern()
        {
            if (_combatPatternCache != null)
            {
                return _combatPatternCache;
            }

            StationData[] pattern = patternStations;
            if (pattern == null || pattern.Length == 0)
            {
                _combatPatternCache = System.Array.Empty<StationData>();
                return _combatPatternCache;
            }

            var combat = new List<StationData>(pattern.Length);
            for (int i = 0; i < pattern.Length; i++)
            {
                StationData s = pattern[i];
                if (s == null)
                {
                    continue;
                }

                if (s.StationType == StationType.Boss && bossStationTemplate != null)
                {
                    continue;
                }

                if (s.StationType == StationType.Shop || s.StationType == StationType.Rest
                    || s.StationType == StationType.Event)
                {
                    continue;
                }

                combat.Add(s);
            }

            if (combat.Count == 0)
            {
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (pattern[i] != null)
                    {
                        combat.Add(pattern[i]);
                    }
                }
            }

            _combatPatternCache = combat.Count > 0
                ? combat.ToArray()
                : System.Array.Empty<StationData>();
            return _combatPatternCache;
        }

        private StationData FindFirstOfType(StationType target)
        {
            if (patternStations == null)
            {
                return null;
            }

            for (int i = 0; i < patternStations.Length; i++)
            {
                StationData s = patternStations[i];
                if (s != null && s.StationType == target)
                {
                    return s;
                }
            }

            return null;
        }

        private void OnDisable()
        {
            ClearRuntimeCache();
        }
    }
}
