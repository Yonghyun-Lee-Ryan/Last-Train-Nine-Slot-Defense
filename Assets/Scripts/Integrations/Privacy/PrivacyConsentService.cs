using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>광고·분석 개인정보 동의 상태. 동의 전에는 SDK 요청을 하지 않는다.</summary>
    public sealed class PrivacyConsentService
    {
        private const string AdsKey = "lasttrain.consent.ads";
        private const string AnalyticsKey = "lasttrain.consent.analytics";
        private const string PromptKey = "lasttrain.consent.prompted";

        public bool HasAdsConsent { get; private set; }
        public bool HasAnalyticsConsent { get; private set; }
        public bool HasCompletedConsentPrompt { get; private set; }

        public bool CanRequestAds => HasAdsConsent;
        public bool CanCollectAnalytics => HasAnalyticsConsent;

        public void Initialize(bool autoGrantInEditor)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (autoGrantInEditor)
            {
                HasAdsConsent = true;
                HasAnalyticsConsent = true;
                return;
            }
#endif
            HasAdsConsent = PlayerPrefs.GetInt(AdsKey, 0) == 1;
            HasAnalyticsConsent = PlayerPrefs.GetInt(AnalyticsKey, 0) == 1;
            HasCompletedConsentPrompt = PlayerPrefs.GetInt(PromptKey, 0) == 1;
        }

        public bool NeedsConsentPrompt()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return false;
#else
            return !HasCompletedConsentPrompt;
#endif
        }

        public void MarkConsentPromptCompleted()
        {
            HasCompletedConsentPrompt = true;
            PlayerPrefs.SetInt(PromptKey, 1);
            PlayerPrefs.Save();
        }

        public void SetAdsConsent(bool granted)
        {
            HasAdsConsent = granted;
            PlayerPrefs.SetInt(AdsKey, granted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetAnalyticsConsent(bool granted)
        {
            HasAnalyticsConsent = granted;
            PlayerPrefs.SetInt(AnalyticsKey, granted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void GrantAllForTesting()
        {
            SetAdsConsent(true);
            SetAnalyticsConsent(true);
            MarkConsentPromptCompleted();
        }

        public void RevokeAll()
        {
            HasAdsConsent = false;
            HasAnalyticsConsent = false;
            HasCompletedConsentPrompt = false;
            PlayerPrefs.DeleteKey(AdsKey);
            PlayerPrefs.DeleteKey(AnalyticsKey);
            PlayerPrefs.DeleteKey(PromptKey);
            PlayerPrefs.Save();
        }
    }
}
