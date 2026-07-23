using System;
using UnityEngine;

namespace LastTrain.LiveOps
{
    /// <summary>시즌 메타 정보. 여러 LiveEvent를 묶을 수 있다.</summary>
    [CreateAssetMenu(fileName = "Season_", menuName = "LastTrain/LiveOps/Season Data")]
    public sealed class SeasonData : ScriptableObject
    {
        [SerializeField] private string id = "season_01";
        [SerializeField] private string displayName = "시즌 1";
        [SerializeField] private string themeId = "default";
        [SerializeField] private LiveEventData[] events = Array.Empty<LiveEventData>();

        public string Id => id;
        public string DisplayName => displayName;
        public string ThemeId => themeId;
        public LiveEventData[] Events => events ?? Array.Empty<LiveEventData>();
    }
}
