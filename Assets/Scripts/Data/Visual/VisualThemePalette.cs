using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>플랫 벡터 심야 지하철 테마 공용 팔레트.</summary>
    public static class VisualThemePalette
    {
        public static readonly Color CarNavy = Hex("#1A2744");
        public static readonly Color CarNavyLight = Hex("#243656");
        public static readonly Color FluorescentTeal = Hex("#2DD4BF");
        public static readonly Color FluorescentTealDim = Hex("#14B8A6");
        public static readonly Color WarningOrange = Hex("#F97316");
        public static readonly Color AlertRed = Hex("#EF4444");
        public static readonly Color CoinGold = Hex("#FBBF24");
        public static readonly Color PanelDark = Hex("#0F172A");
        public static readonly Color PanelMid = Hex("#334155");
        public static readonly Color TextLight = Hex("#F8FAFC");
        public static readonly Color Outline = Hex("#0B1220");
        public static readonly Color SeatFrame = Hex("#475569");
        public static readonly Color WindowGlow = Hex("#38BDF8");
        public static readonly Color VictoryGreen = Hex("#22C55E");
        public static readonly Color DefeatRed = Hex("#DC2626");

        public static readonly Color[] PassengerAccent =
        {
            Hex("#64748B"), // office worker
            Hex("#F97316"), // delivery
            Hex("#EF4444"), // trainer
            Hex("#EC4899"), // nurse
            Hex("#6366F1"), // developer
            Hex("#8B5CF6"), // graduate
            Hex("#3B82F6"), // police
            Hex("#F59E0B"), // cat
            Hex("#14B8A6"), // conductor
            Hex("#D97706"), // barista
            Hex("#475569"), // security
            Hex("#0EA5E9")  // student
        };

        public static readonly Color[] EnemyAccent =
        {
            Hex("#84CC16"), // normal
            Hex("#06B6D4"), // fast
            Hex("#78716C"), // tank
            Hex("#B91C1C"), // boss drunk manager
            Hex("#A855F7"), // split
            Hex("#C084FC"), // split minion
            Hex("#14B8A6"), // aura watcher
            Hex("#F59E0B"), // seat blocker
            Hex("#7F1D1D")  // final conductor
        };

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }

            return Color.magenta;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color StarTint(int starLevel)
        {
            return starLevel switch
            {
                3 => Hex("#FDE047"),
                2 => Hex("#CBD5E1"),
                _ => Color.white
            };
        }
    }
}
