using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Enemy;

namespace LastTrain.Battle
{
    public readonly struct BossPhaseSegment
    {
        public BossPhaseSegment(BossPhase phase, float healthRatioFrom, float healthRatioTo)
        {
            Phase = phase;
            HealthRatioFrom = healthRatioFrom;
            HealthRatioTo = healthRatioTo;
        }

        public BossPhase Phase { get; }
        public float HealthRatioFrom { get; }
        public float HealthRatioTo { get; }
        public float Span => UnityEngine.Mathf.Max(0f, HealthRatioFrom - HealthRatioTo);
    }

    /// <summary>보스 역 진입 시 페이즈 막대·위협 아이콘 카드 데이터. StationBriefing을 재사용한다.</summary>
    public sealed class BossPhaseBriefing
    {
        public bool ShouldShow { get; set; }
        public int PhaseCount => Segments != null ? Segments.Count : 0;
        public IReadOnlyList<BossPhaseSegment> Segments { get; set; } = System.Array.Empty<BossPhaseSegment>();
        public IReadOnlyList<EnemyType> ThreatTypes { get; set; } = System.Array.Empty<EnemyType>();
        public string PatternHint { get; set; } = string.Empty;
        public bool HasDoorOpen { get; set; }
        public float DoorOpenHealthRatio { get; set; }
        public float EnrageHealthRatio { get; set; }
    }

    public static class BossPhaseBriefingResolver
    {
        public static BossPhaseBriefing Build(StationData station, StationBriefing briefing)
        {
            var result = new BossPhaseBriefing();
            if (station == null || station.StationType != StationType.Boss)
            {
                return result;
            }

            result.ShouldShow = true;
            briefing ??= new StationBriefing();
            result.PatternHint = !string.IsNullOrWhiteSpace(briefing.BossPatternHint)
                ? briefing.BossPatternHint
                : station.BossPatternHint;
            result.ThreatTypes = briefing.CollectThreatTypes();

            BossPhaseThresholds thresholds = ResolveThresholds(FindBossEnemy(station));
            float enrage = thresholds.EnrageHealthRatio > 0f
                ? thresholds.EnrageHealthRatio
                : BossPhaseController.LegacyEnrageHealthRatio;
            float door = thresholds.DoorOpenHealthRatio;
            result.EnrageHealthRatio = enrage;
            result.DoorOpenHealthRatio = door;
            result.HasDoorOpen = door > 0f && door > enrage;
            result.Segments = BuildSegments(door, enrage, result.HasDoorOpen);
            return result;
        }

        private static IReadOnlyList<BossPhaseSegment> BuildSegments(float door, float enrage, bool hasDoorOpen)
        {
            var segments = new List<BossPhaseSegment>(3);
            if (hasDoorOpen)
            {
                segments.Add(new BossPhaseSegment(BossPhase.Normal, 1f, door));
                segments.Add(new BossPhaseSegment(BossPhase.DoorOpen, door, enrage));
            }
            else
            {
                segments.Add(new BossPhaseSegment(BossPhase.Normal, 1f, enrage));
            }

            segments.Add(new BossPhaseSegment(BossPhase.Enraged, enrage, 0f));
            return segments;
        }

        private static BossPhaseThresholds ResolveThresholds(EnemyData boss)
        {
            if (boss == null)
            {
                return BossPhaseThresholds.DefaultMidBoss;
            }

            BossPhaseThresholds raw = boss.BossPhaseThresholds;
            if (raw.EnrageHealthRatio <= 0f && raw.DoorOpenHealthRatio <= 0f)
            {
                return BossPhaseThresholds.DefaultMidBoss;
            }

            return raw;
        }

        private static EnemyData FindBossEnemy(StationData station)
        {
            IReadOnlyList<WaveData> waves = station?.Waves;
            if (waves == null)
            {
                return null;
            }

            for (int i = 0; i < waves.Count; i++)
            {
                WaveData wave = waves[i];
                if (wave?.Spawns == null)
                {
                    continue;
                }

                for (int j = 0; j < wave.Spawns.Count; j++)
                {
                    EnemyData enemy = wave.Spawns[j].enemy;
                    if (enemy != null && enemy.EnemyType == EnemyType.Boss)
                    {
                        return enemy;
                    }
                }
            }

            return null;
        }
    }
}
