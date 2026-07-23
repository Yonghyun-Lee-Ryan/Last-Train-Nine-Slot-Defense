using System;

namespace LastTrain.Mission
{
    /// <summary>미션 진행 저장 항목. 조건 정의(MissionCondition)와 분리된다.</summary>
    [Serializable]
    public sealed class MissionProgressSave
    {
        public string missionId = string.Empty;
        public string periodKey = string.Empty;
        public int progress;
        public bool claimed;
        public bool completed;
    }

    /// <summary>런타임 조회용 진행 스냅샷.</summary>
    public sealed class MissionProgressView
    {
        public MissionProgressView(
            MissionData data,
            int progress,
            int target,
            bool completed,
            bool claimed,
            string periodKey)
        {
            Data = data;
            Progress = Math.Max(0, progress);
            Target = Math.Max(1, target);
            Completed = completed;
            Claimed = claimed;
            PeriodKey = periodKey ?? string.Empty;
        }

        public MissionData Data { get; }
        public int Progress { get; }
        public int Target { get; }
        public bool Completed { get; }
        public bool Claimed { get; }
        public string PeriodKey { get; }
        public bool CanClaim => Completed && !Claimed;
    }
}
