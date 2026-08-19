using System.Collections.Generic;
using System.Text;
using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Synergy;

namespace LastTrain.UI
{
    /// <summary>시너지 HUD 문구. 활성(금색) / 비활성(슬레이트) + 진행도.</summary>
    public static class SynergyHudFormatter
    {
        public const string ActiveColorHex = "FBBF24";
        public const string InactiveColorHex = "94A3B8";

        public static string Format(IReadOnlyList<SynergyData> catalog, RunState runState)
        {
            if (catalog == null || catalog.Count == 0)
            {
                return "시너지: 없음";
            }

            var sb = new StringBuilder(128);
            sb.Append("시너지");
            int lines = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                SynergyData data = catalog[i];
                if (data == null || string.IsNullOrWhiteSpace(data.DisplayName))
                {
                    continue;
                }

                bool active = SynergyEffectCalculator.IsActive(data, runState);
                GetProgress(data, runState, out int current, out int required);
                string marker = active ? "●" : "○";
                string progress = !active && required > 0 ? $" {current}/{required}" : string.Empty;
                string color = active ? ActiveColorHex : InactiveColorHex;
                sb.Append('\n');
                sb.Append("<color=#").Append(color).Append('>');
                sb.Append(marker).Append(' ').Append(data.DisplayName).Append(progress);
                sb.Append("</color>");
                lines++;
            }

            return lines == 0 ? "시너지: 없음" : sb.ToString();
        }

        public static void GetProgress(SynergyData data, RunState runState, out int current, out int required)
        {
            current = 0;
            required = 0;
            if (data == null || runState == null)
            {
                return;
            }

            bool hasTags = data.RequiredTags != PassengerTag.None;
            bool hasUnique = data.RequiredUniquePassengerCount > 0;

            if (hasTags)
            {
                current = SynergyEffectCalculator.CountMatchingPassengers(runState, data.RequiredTags);
                required = data.RequiredCount > 0 ? data.RequiredCount : 1;
                return;
            }

            if (hasUnique)
            {
                current = SynergyEffectCalculator.CountUniquePassengerTypes(runState);
                required = data.RequiredUniquePassengerCount;
            }
        }
    }
}
