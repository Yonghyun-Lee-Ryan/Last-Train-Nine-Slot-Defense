using System;
using LastTrain.Ads;
using LastTrain.Core;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>
    /// 전면 광고 노출 정책.
    /// 전투 중·보상형 직후·초기 N회차에는 노출하지 않는다.
    /// </summary>
    public sealed class InterstitialAdCoordinator
    {
        private readonly AdCoordinator _ads;
        private readonly PrivacyConsentService _consent;
        private readonly SceneLoader _sceneLoader;
        private readonly Func<GameSession> _sessionProvider;

        private float _lastRewardedUtc;
        private float _lastInterstitialUtc;
        private int _completedRuns;

        public InterstitialAdCoordinator(
            AdCoordinator ads,
            PrivacyConsentService consent,
            SceneLoader sceneLoader,
            Func<GameSession> sessionProvider)
        {
            _ads = ads;
            _consent = consent;
            _sceneLoader = sceneLoader;
            _sessionProvider = sessionProvider;
        }

        public void Subscribe()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.SceneLoadCompleted += HandleSceneLoaded;
            }
        }

        public void Unsubscribe()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.SceneLoadCompleted -= HandleSceneLoaded;
            }
        }

        public void NotifyRewardedCompleted()
        {
            _lastRewardedUtc = Time.unscaledTime;
        }

        public void NotifyRunCompleted()
        {
            _completedRuns++;
        }

        private void HandleSceneLoaded(string sceneName)
        {
            if (!_consent.CanRequestAds || _ads == null)
            {
                return;
            }

            if (!string.Equals(sceneName, SceneNames.MainMenu, StringComparison.Ordinal))
            {
                return;
            }

            if (IsBattleActive())
            {
                return;
            }

            RemoteConfigSnapshot rc = RemoteConfigRuntime.Current;
            if (_completedRuns < rc.RunsBeforeInterstitial)
            {
                return;
            }

            if (Time.unscaledTime - _lastRewardedUtc < 5f)
            {
                return;
            }

            if (Time.unscaledTime - _lastInterstitialUtc < rc.InterstitialIntervalSeconds)
            {
                return;
            }

            var request = new AdRequest(RewardedAdPlacement.ShopRefresh);
            _ads.AdService.ShowInterstitial(request, result =>
            {
                if (result == AdResult.Completed)
                {
                    _lastInterstitialUtc = Time.unscaledTime;
                }
            });
        }

        private bool IsBattleActive()
        {
            GameSession session = _sessionProvider?.Invoke();
            if (session == null || !session.HasActiveRun)
            {
                return false;
            }

            RunPhase phase = session.RunState.Battle.CurrentPhase;
            return phase == RunPhase.Fighting
                   || phase == RunPhase.WaveStarting
                   || phase == RunPhase.WaveCompleted;
        }
    }
}
