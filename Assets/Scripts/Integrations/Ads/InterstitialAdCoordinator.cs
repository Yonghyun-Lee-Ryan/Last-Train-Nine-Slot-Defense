using System;
using LastTrain.Ads;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastTrain.Integrations
{
    /// <summary>
    /// 전면 광고 노출 정책.
    /// 한 run(승/패)이 완전히 끝난 뒤 Result 화면에서만 1회 검토한다.
    /// </summary>
    public sealed class InterstitialAdCoordinator
    {
        private readonly AdCoordinator _ads;
        private readonly PrivacyConsentService _consent;
        private readonly Func<GameSession> _sessionProvider;

        private float _lastRewardedUtc;
        private float _lastInterstitialUtc;
        private int _completedRuns;
        private int _lastInterstitialCompletedRunCount;
        private bool _pendingAfterRunEnd;
        private RunResult _pendingRunResult;

        public InterstitialAdCoordinator(
            AdCoordinator ads,
            PrivacyConsentService consent,
            SceneLoader sceneLoader,
            Func<GameSession> sessionProvider)
        {
            _ads = ads;
            _consent = consent;
            _ = sceneLoader;
            _sessionProvider = sessionProvider;
        }

        public void Subscribe()
        {
        }

        public void Unsubscribe()
        {
        }

        public void NotifyRewardedCompleted()
        {
            _lastRewardedUtc = Time.unscaledTime;
        }

        public void NotifyRunCompleted(RunResult result)
        {
            _completedRuns++;
            _pendingRunResult = result;
            _pendingAfterRunEnd = true;
        }

        /// <summary>Result 화면 진입 시 1회 호출.</summary>
        public void TryShowAfterRunEnded()
        {
            if (!_pendingAfterRunEnd)
            {
                return;
            }

            if (!IsResultSceneActive())
            {
                return;
            }

            if (HasActiveBattleSession())
            {
                return;
            }

            RunResult result = _pendingRunResult;
            _pendingAfterRunEnd = false;
            _pendingRunResult = null;

            if (!_consent.CanRequestAds || _ads == null)
            {
                return;
            }

            if (!ShouldOfferInterstitialForRun(result))
            {
                return;
            }

            if (!PassesInterstitialPolicy())
            {
                return;
            }

            int runIndex = _completedRuns;
            _ads.ShowInterstitial(adResult =>
            {
                if (adResult == AdResult.Completed)
                {
                    _lastInterstitialUtc = Time.unscaledTime;
                    _lastInterstitialCompletedRunCount = runIndex;
                }
            });
        }

        private bool ShouldOfferInterstitialForRun(RunResult result)
        {
            if (result == null)
            {
                return false;
            }

            if (!result.IsEndlessRun)
            {
                return true;
            }

            int threshold = ResolveStandardRunStationCount();
            return result.CompletedStationCount >= threshold;
        }

        private static int ResolveStandardRunStationCount()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            if (database == null)
            {
                return 10;
            }

            return Mathf.Max(1, database.GetRouteStationCount(RouteIds.Default));
        }

        private bool PassesInterstitialPolicy()
        {
            RemoteConfigSnapshot remoteConfig = RemoteConfigRuntime.Current;

            if (_completedRuns < remoteConfig.RunsBeforeInterstitial)
            {
                return false;
            }

            if (_completedRuns <= _lastInterstitialCompletedRunCount)
            {
                return false;
            }

            if (Time.unscaledTime - _lastRewardedUtc < 5f)
            {
                return false;
            }

            return Time.unscaledTime - _lastInterstitialUtc >= remoteConfig.InterstitialIntervalSeconds;
        }

        private static bool IsResultSceneActive()
        {
            return string.Equals(
                SceneManager.GetActiveScene().name,
                SceneNames.Result,
                StringComparison.Ordinal);
        }

        private bool HasActiveBattleSession()
        {
            GameSession session = _sessionProvider?.Invoke();
            return session != null && session.HasActiveRun;
        }
    }
}
