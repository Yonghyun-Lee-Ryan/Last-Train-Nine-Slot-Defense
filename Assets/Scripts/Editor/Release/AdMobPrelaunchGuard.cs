using System.Text;
using LastTrain.Integrations;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Release AAB / 커밋 전에 Google 테스트 AdMob ID만 쓰는지 검사한다.</summary>
    public static class AdMobPrelaunchGuard
    {
        public const string AdUnitConfigPath = "Assets/Data/Integration/AdUnitConfig.asset";
        public const string GoogleMobileAdsSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

        public static bool TryValidateTrackedAssets(out string error)
        {
            var errors = new StringBuilder();
            AdUnitConfig ads = AssetDatabase.LoadAssetAtPath<AdUnitConfig>(AdUnitConfigPath);
            if (ads == null)
            {
                errors.AppendLine("AdUnitConfig.asset이 없습니다.");
            }
            else
            {
                if (!ads.UseGoogleTestAdUnits)
                {
                    errors.AppendLine("AdUnitConfig.useGoogleTestAdUnits가 꺼져 있습니다. Play 출시 전에는 켜 두세요.");
                }

                string rewarded = ads.GetRewardedUnitId(useTestIds: false);
                string interstitial = ads.GetInterstitialUnitId(useTestIds: false);
                if (!AdMobIdPolicy.IsGoogleSampleAdMobId(rewarded))
                {
                    errors.AppendLine("보상형 광고 단위가 Google 테스트 ID가 아닙니다: " + rewarded);
                }

                if (!AdMobIdPolicy.IsGoogleSampleAdMobId(interstitial))
                {
                    errors.AppendLine("전면 광고 단위가 Google 테스트 ID가 아닙니다: " + interstitial);
                }

                var so = new SerializedObject(ads);
                RejectIfFilled(so, "androidRewardedProductionId", errors);
                RejectIfFilled(so, "androidInterstitialProductionId", errors);
                RejectIfFilled(so, "iosRewardedProductionId", errors);
                RejectIfFilled(so, "iosInterstitialProductionId", errors);
            }

            ScriptableObject gmaSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GoogleMobileAdsSettingsPath);
            if (gmaSettings != null)
            {
                var gmaSo = new SerializedObject(gmaSettings);
                string appId = gmaSo.FindProperty("adMobAndroidAppId")?.stringValue ?? string.Empty;
                string iosAppId = gmaSo.FindProperty("adMobIOSAppId")?.stringValue ?? string.Empty;
                if (!AdMobIdPolicy.IsGoogleSampleAdMobId(appId))
                {
                    errors.AppendLine(
                        "GoogleMobileAds Android App ID가 Google 샘플이 아닙니다. " +
                        AdMobIdPolicy.GoogleSampleAppId + " 를 사용하세요.");
                }

                if (!string.IsNullOrWhiteSpace(iosAppId) && !AdMobIdPolicy.IsGoogleSampleAdMobId(iosAppId))
                {
                    errors.AppendLine("GoogleMobileAds iOS App ID가 Google 샘플이 아닙니다.");
                }
            }

            error = errors.ToString().Trim();
            return string.IsNullOrEmpty(error);
        }

        private static void RejectIfFilled(SerializedObject so, string propertyName, StringBuilder errors)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || string.IsNullOrWhiteSpace(prop.stringValue))
            {
                return;
            }

            errors.AppendLine(
                propertyName + "에 값이 있습니다. GitHub·출시 전 빌드에는 운영 광고 ID를 넣지 마세요.");
        }
    }
}
