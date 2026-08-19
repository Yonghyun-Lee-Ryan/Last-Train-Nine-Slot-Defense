using System;
using LastTrain.Ads;
using LastTrain.Battle;
using LastTrain.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
#if LASTTRAIN_ADMOB
using GoogleMobileAds.Api;
#endif

namespace LastTrain.Integrations
{
    /// <summary>
    /// Google AdMob IAdService 어댑터.
    /// LASTTRAIN_ADMOB + Google Mobile Ads SDK(com.google.ads.mobile) 설치 후 활성화한다.
    /// </summary>
    public sealed class AdMobAdService : IAdService
    {
        private readonly AdUnitConfig _config;
        private readonly bool _useTestIds;
        private bool _sdkInitialized;
#if LASTTRAIN_ADMOB
        private bool _rewardedLoading;
        private RewardedAd _rewardedAd;
        private InterstitialAd _interstitialAd;
        private bool _interstitialLoading;
#endif

        private AdMobAdService(AdUnitConfig config, bool useTestIds)
        {
            _config = config;
            _useTestIds = useTestIds;
        }

        public static IAdService TryCreate(AdUnitConfig config, bool useTestIds)
        {
            if (config == null)
            {
                return null;
            }

#if LASTTRAIN_ADMOB
            try
            {
                var service = new AdMobAdService(config, useTestIds);
                service.BeginInitialize();
                return service;
            }
            catch (Exception ex)
            {
                Debug.LogError("[AdMobAdService] TryCreate failed: " + ex.Message);
                return null;
            }
#else
            return null;
#endif
        }

        private void BeginInitialize()
        {
#if LASTTRAIN_ADMOB
            try
            {
                MobileAds.Initialize(status =>
                {
                    if (status == null)
                    {
                        Debug.LogError("[AdMobAdService] MobileAds.Initialize returned null status.");
                        _sdkInitialized = false;
                        return;
                    }

                    _sdkInitialized = true;
                    Debug.Log("[AdMobAdService] SDK initialized. useTestIds=" + _useTestIds);
                    LoadRewarded();
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[AdMobAdService] Initialize exception: " + ex.Message);
                _sdkInitialized = false;
            }
#endif
        }

        public bool IsRewardedReady(RewardedAdPlacement placement)
        {
#if LASTTRAIN_ADMOB
            return _sdkInitialized
                   && _rewardedAd != null
                   && _rewardedAd.CanShowAd();
#else
            return false;
#endif
        }

        public void ShowRewardedAd(LastTrain.Ads.AdRequest request, Action<AdResult> onFinished)
        {
#if LASTTRAIN_ADMOB
            if (!_sdkInitialized || _rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                onFinished?.Invoke(AdResult.NotReady);
                LoadRewarded();
                return;
            }

            RewardedAd ad = _rewardedAd;
            _rewardedAd = null;
            bool rewarded = false;
            bool finished = false;

            void Finish(AdResult result)
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                try
                {
                    ad.Destroy();
                }
                catch (Exception)
                {
                    // ignore destroy errors
                }

                BattleSpeedRuntime.RestoreTimeScaleAfterAd();
                LoadRewarded();
                onFinished?.Invoke(result);
            }

            bool audioPausedBefore = AudioListener.pause;

            try
            {
                ad.OnAdFullScreenContentClosed += () =>
                {
                    BattleSpeedRuntime.RestoreTimeScaleAfterAd();
                    AudioListener.pause = audioPausedBefore;
                    Finish(rewarded ? AdResult.Completed : AdResult.Cancelled);
                };
                ad.OnAdFullScreenContentFailed += _ =>
                {
                    BattleSpeedRuntime.RestoreTimeScaleAfterAd();
                    AudioListener.pause = audioPausedBefore;
                    Finish(AdResult.Failed);
                };

                ad.Show(_ => { rewarded = true; });
            }
            catch (Exception ex)
            {
                Debug.LogError("[AdMobAdService] ShowRewarded exception: " + ex.Message);
                Finish(AdResult.Failed);
            }
#else
            onFinished?.Invoke(AdResult.NotReady);
#endif
        }

        public void ShowInterstitial(Action<AdResult> onFinished)
        {
#if LASTTRAIN_ADMOB
            string activeScene = SceneManager.GetActiveScene().name;
            if (string.Equals(activeScene, SceneNames.Game, StringComparison.Ordinal))
            {
                Debug.LogWarning("[AdMobAdService] Game 씬에서는 전면 광고를 차단합니다.");
                onFinished?.Invoke(AdResult.NotReady);
                return;
            }

            if (AppRoot.Instance?.GameSession?.HasActiveRun == true)
            {
                Debug.LogWarning("[AdMobAdService] 활성 run 중에는 전면 광고를 차단합니다.");
                onFinished?.Invoke(AdResult.NotReady);
                return;
            }

            if (!_sdkInitialized || _interstitialAd == null || !_interstitialAd.CanShowAd())
            {
                onFinished?.Invoke(AdResult.NotReady);
                LoadInterstitial();
                return;
            }

            bool audioPausedBefore = AudioListener.pause;

            InterstitialAd ad = _interstitialAd;
            _interstitialAd = null;
            bool finished = false;

            void Finish(AdResult result)
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                try
                {
                    ad.Destroy();
                }
                catch (Exception)
                {
                    // ignore
                }

                BattleSpeedRuntime.RestoreTimeScaleAfterAd();
                AudioListener.pause = audioPausedBefore;
                LoadInterstitial();
                onFinished?.Invoke(result);
            }

            try
            {
                ad.OnAdFullScreenContentClosed += () => Finish(AdResult.Completed);
                ad.OnAdFullScreenContentFailed += _ => Finish(AdResult.Failed);
                ad.Show();
            }
            catch (Exception ex)
            {
                Debug.LogError("[AdMobAdService] ShowInterstitial exception: " + ex.Message);
                Finish(AdResult.Failed);
            }
#else
            onFinished?.Invoke(AdResult.NotReady);
#endif
        }

#if LASTTRAIN_ADMOB
        private void LoadRewarded()
        {
            if (!_sdkInitialized || _rewardedLoading)
            {
                return;
            }

            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                return;
            }

            _rewardedLoading = true;
            string unitId = _config.GetRewardedUnitId(_useTestIds);
            try
            {
                var gmaRequest = new GoogleMobileAds.Api.AdRequest();
                RewardedAd.Load(unitId, gmaRequest, (RewardedAd ad, LoadAdError error) =>
                {
                    _rewardedLoading = false;
                    if (error != null || ad == null)
                    {
                        Debug.LogWarning("[AdMobAdService] Rewarded load failed: " + error);
                        return;
                    }

                    _rewardedAd = ad;
                });
            }
            catch (Exception ex)
            {
                _rewardedLoading = false;
                Debug.LogError("[AdMobAdService] Rewarded load exception: " + ex.Message);
            }
        }

        private void LoadInterstitial()
        {
            if (!_sdkInitialized || _interstitialLoading)
            {
                return;
            }

            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                return;
            }

            _interstitialLoading = true;
            string unitId = _config.GetInterstitialUnitId(_useTestIds);
            try
            {
                var gmaRequest = new GoogleMobileAds.Api.AdRequest();
                InterstitialAd.Load(unitId, gmaRequest, (InterstitialAd ad, LoadAdError error) =>
                {
                    _interstitialLoading = false;
                    if (error != null || ad == null)
                    {
                        Debug.LogWarning("[AdMobAdService] Interstitial load failed: " + error);
                        return;
                    }

                    _interstitialAd = ad;
                });
            }
            catch (Exception ex)
            {
                _interstitialLoading = false;
                Debug.LogError("[AdMobAdService] Interstitial load exception: " + ex.Message);
            }
        }
#endif
    }
}
