using System.Collections.Generic;
using System.Text;
using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Save;

namespace LastTrain.UI
{
    /// <summary>Result Scene에 표시할 원인·보상·통계 텍스트를 만든다.</summary>
    public static class RunResultFormatter
    {
        public static string GetTitle(RunResult result)
        {
            if (result == null)
            {
                return "결과";
            }

            return result.IsVictory ? "도착 성공!" : "게임 오버";
        }

        public static string GetOverlayMessage(RunResult result)
        {
            return GetCauseLine(result);
        }

        /// <summary>승/패 원인을 한 줄로 설명한다.</summary>
        public static string GetCauseLine(RunResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            if (result.IsVictory || result.EndReason == RunEndReason.Victory)
            {
                return result.IsEndlessRun
                    ? "무한 노선을 더 깊이 돌파했습니다."
                    : "최종 역에 도착했습니다.";
            }

            if (result.EndReason == RunEndReason.Abandoned)
            {
                return "회차를 중단했습니다.";
            }

            if (result.ReachedStationType == StationType.Boss)
            {
                return "보스전에서 객차가 파괴되었습니다.";
            }

            if (result.RemainingTrainHp <= 0)
            {
                return "객차 내구도가 0이 되었습니다.";
            }

            return "객차가 파괴되었습니다.";
        }

        public static string BuildStatsText(RunResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            return
                $"도달 역: {result.ReachedStationIndex}\n" +
                $"완료 역: {result.CompletedStationCount}\n" +
                $"처치 수: {result.EnemiesKilled}\n" +
                $"보스 처치: {result.BossesKilled}\n" +
                $"합성 수: {result.MergeCount}\n" +
                $"최고 승객 등급: {result.HighestPassengerStar}★\n" +
                $"남은 내구도: {result.RemainingTrainHp}/{result.TrainMaxHp}\n" +
                $"획득 코인: {result.TotalCoinsEarned}\n" +
                $"보유 코인: {result.FinalCoins}";
        }

        public static string BuildMetaRewardText(MetaApplyResult applyResult, GameDatabase database = null)
        {
            string summary = BuildRewardSummary(applyResult, database);
            return string.IsNullOrEmpty(summary) ? string.Empty : "\n\n" + summary;
        }

        /// <summary>조각·신규 해금이 한눈에 읽히도록 요약한다.</summary>
        public static string BuildRewardSummary(MetaApplyResult applyResult, GameDatabase database = null)
        {
            if (applyResult == null)
            {
                return string.Empty;
            }

            if (applyResult.WasDuplicate)
            {
                return "[메타] 이미 보상을 받은 회차입니다.";
            }

            if (!applyResult.Applied || applyResult.Breakdown == null)
            {
                return string.Empty;
            }

            MetaRewardBreakdown b = applyResult.Breakdown;
            var sb = new StringBuilder();
            sb.Append("[획득]\n");
            sb.Append("승차권 조각 +").Append(b.TotalTickets);
            sb.Append(" (보유 ").Append(applyResult.TicketFragmentsAfter).Append(')');
            sb.Append("\n조각 내역: 역 +").Append(b.StationTickets)
                .Append(" · 처치 +").Append(b.KillTickets)
                .Append(" · 보스 +").Append(b.BossTickets);
            sb.Append("\n계정 Lv.").Append(applyResult.AccountLevelAfter)
                .Append(" (XP ").Append(applyResult.AccountXpAfter).Append(')');

            AppendNamedList(sb, "신규 발견", CollectDiscoveryNames(b, database));
            AppendNamedList(sb, "신규 해금", ResolveNames(b.NewlyUnlockedPassengers, database, isPassenger: true));
            AppendNamedList(sb, "업적 해금", ResolveAchievementNames(b.NewlyUnlockedAchievements));
            return sb.ToString();
        }

        public static IReadOnlyList<string> CollectRevealLines(MetaApplyResult applyResult, GameDatabase database = null)
        {
            var lines = new List<string>();
            if (applyResult == null)
            {
                return lines;
            }

            if (applyResult.WasDuplicate)
            {
                lines.Add("이미 보상을 받은 회차입니다.");
                return lines;
            }

            if (!applyResult.Applied || applyResult.Breakdown == null)
            {
                return lines;
            }

            MetaRewardBreakdown b = applyResult.Breakdown;
            if (b.TotalTickets > 0)
            {
                lines.Add($"승차권 조각 +{b.TotalTickets}");
            }

            IReadOnlyList<string> discoveries = CollectDiscoveryNames(b, database);
            for (int i = 0; i < discoveries.Count; i++)
            {
                lines.Add($"신규 발견: {discoveries[i]}");
            }

            IReadOnlyList<string> unlocks = ResolveNames(b.NewlyUnlockedPassengers, database, isPassenger: true);
            for (int i = 0; i < unlocks.Count; i++)
            {
                lines.Add($"신규 해금: {unlocks[i]}");
            }

            IReadOnlyList<string> achievements = ResolveAchievementNames(b.NewlyUnlockedAchievements);
            for (int i = 0; i < achievements.Count; i++)
            {
                lines.Add($"업적 해금: {achievements[i]}");
            }

            return lines;
        }

        private static IReadOnlyList<string> CollectDiscoveryNames(MetaRewardBreakdown breakdown, GameDatabase database)
        {
            var names = new List<string>();
            names.AddRange(ResolveNames(breakdown.NewPassengerDiscoveries, database, isPassenger: true));
            names.AddRange(ResolveNames(breakdown.NewEnemyDiscoveries, database, isPassenger: false));
            names.AddRange(ResolveNames(breakdown.NewBossDiscoveries, database, isPassenger: false));
            return DistinctPreserveOrder(names);
        }

        private static IReadOnlyList<string> ResolveAchievementNames(List<string> ids)
        {
            var names = new List<string>();
            if (ids == null)
            {
                return names;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(ids[i]))
                {
                    names.Add(AchievementCatalog.GetDisplayNameOrId(ids[i]));
                }
            }

            return DistinctPreserveOrder(names);
        }

        private static IReadOnlyList<string> ResolveNames(List<string> ids, GameDatabase database, bool isPassenger)
        {
            var names = new List<string>();
            if (ids == null)
            {
                return names;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                names.Add(ResolveDisplayName(id, database, isPassenger));
            }

            return DistinctPreserveOrder(names);
        }

        private static string ResolveDisplayName(string id, GameDatabase database, bool isPassenger)
        {
            if (database == null)
            {
                return id;
            }

            if (isPassenger && database.TryGetPassenger(id, out PassengerData passenger) && passenger != null)
            {
                return string.IsNullOrWhiteSpace(passenger.DisplayName) ? id : passenger.DisplayName;
            }

            if (!isPassenger && database.TryGetEnemy(id, out EnemyData enemy) && enemy != null)
            {
                return string.IsNullOrWhiteSpace(enemy.DisplayName) ? id : enemy.DisplayName;
            }

            return id;
        }

        private static List<string> DistinctPreserveOrder(List<string> names)
        {
            if (names == null || names.Count <= 1)
            {
                return names ?? new List<string>();
            }

            var seen = new HashSet<string>();
            var unique = new List<string>(names.Count);
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                {
                    continue;
                }

                unique.Add(name);
            }

            return unique;
        }

        private static void AppendNamedList(StringBuilder sb, string label, IReadOnlyList<string> names)
        {
            if (sb == null || names == null || names.Count == 0)
            {
                return;
            }

            sb.Append('\n').Append(label).Append(": ");
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(names[i]);
            }
        }
    }
}
