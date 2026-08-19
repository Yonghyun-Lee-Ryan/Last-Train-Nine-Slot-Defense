using System.Text.RegularExpressions;

namespace LastTrain.Integrations
{
    /// <summary>
    /// Play 스토어 출시 전 AdMob ID 정책.
    /// GitHub·Release AAB에는 Google 공식 샘플(테스트) ID만 허용한다.
    /// </summary>
    public static class AdMobIdPolicy
    {
        public const string GoogleSamplePublisher = "3940256099942544";
        public const string GoogleSampleAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string GoogleSampleRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
        public const string GoogleSampleInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";

        private static readonly Regex AdMobIdRegex = new Regex(
            @"ca-app-pub-(\d+)[~\/][A-Za-z0-9_-]+",
            RegexOptions.CultureInvariant);

        public static bool IsGoogleSampleAdMobId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            Match match = AdMobIdRegex.Match(value.Trim());
            return match.Success
                   && string.Equals(match.Groups[1].Value, GoogleSamplePublisher, System.StringComparison.Ordinal);
        }

        public static bool TryFindNonSampleAdMobId(string text, out string foundId)
        {
            foundId = string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            MatchCollection matches = AdMobIdRegex.Matches(text);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                if (!string.Equals(match.Groups[1].Value, GoogleSamplePublisher, System.StringComparison.Ordinal))
                {
                    foundId = match.Value;
                    return true;
                }
            }

            return false;
        }
    }
}
