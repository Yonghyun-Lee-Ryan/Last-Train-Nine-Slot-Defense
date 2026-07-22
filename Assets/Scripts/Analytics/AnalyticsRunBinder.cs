using System;
using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Synergy;

namespace LastTrain.Analytics
{
    /// <summary>전투 씬의 매니저 이벤트를 분석으로 연결한다.</summary>
    public sealed class AnalyticsRunBinder : IDisposable
    {
        private readonly AnalyticsCoordinator _analytics;
        private StationManager _stationManager;
        private BattleManager _battleManager;
        private SynergyManager _synergyManager;
        private GridManager _gridManager;
        private SummonManager _summonManager;
        private AbilityManager _abilityManager;
        private bool _disposed;

        public AnalyticsRunBinder(AnalyticsCoordinator analytics)
        {
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public void BindBattle(
            StationManager stationManager,
            BattleManager battleManager,
            SynergyManager synergyManager,
            GridManager gridManager)
        {
            UnbindBattle();
            _stationManager = stationManager;
            _battleManager = battleManager;
            _synergyManager = synergyManager;
            _gridManager = gridManager;

            if (_stationManager != null)
            {
                _stationManager.StationStarted += HandleStationStarted;
                _stationManager.StationCompleted += HandleStationCompleted;
                _stationManager.WaveManager.WaveStarted += HandleWaveStarted;
                _stationManager.WaveManager.WaveCompleted += HandleWaveCompleted;
            }

            if (_battleManager != null)
            {
                _battleManager.BossSpawned += HandleBossSpawned;
                _battleManager.BossDespawned += HandleBossDespawned;
                _battleManager.BossPhaseChanged += HandleBossPhaseChanged;
            }

            if (_synergyManager != null)
            {
                _synergyManager.SynergyActivated += HandleSynergyActivated;
            }

            if (_gridManager != null)
            {
                _gridManager.PassengerDropped += HandlePassengerDropped;
                _gridManager.MergeCompleted += HandleMergeCompleted;
            }

            PassengerSellService.Sold += HandlePassengerSold;
        }

        public void BindSummon(SummonManager summonManager)
        {
            if (_summonManager != null)
            {
                _summonManager.SummonRequested -= HandleSummonRequested;
                _summonManager.OfferSelected -= HandleOfferSelected;
            }

            _summonManager = summonManager;
            if (_summonManager == null)
            {
                return;
            }

            _summonManager.SummonRequested += HandleSummonRequested;
            _summonManager.OfferSelected += HandleOfferSelected;
        }

        public void BindAbility(AbilityManager abilityManager)
        {
            if (_abilityManager != null)
            {
                _abilityManager.OffersGenerated -= HandleAbilityOffersGenerated;
                _abilityManager.AbilitySelected -= HandleAbilitySelected;
            }

            _abilityManager = abilityManager;
            if (_abilityManager == null)
            {
                return;
            }

            _abilityManager.OffersGenerated += HandleAbilityOffersGenerated;
            _abilityManager.AbilitySelected += HandleAbilitySelected;
        }

        public void UnbindBattle()
        {
            if (_stationManager != null)
            {
                _stationManager.StationStarted -= HandleStationStarted;
                _stationManager.StationCompleted -= HandleStationCompleted;
                _stationManager.WaveManager.WaveStarted -= HandleWaveStarted;
                _stationManager.WaveManager.WaveCompleted -= HandleWaveCompleted;
                _stationManager = null;
            }

            if (_battleManager != null)
            {
                _battleManager.BossSpawned -= HandleBossSpawned;
                _battleManager.BossDespawned -= HandleBossDespawned;
                _battleManager.BossPhaseChanged -= HandleBossPhaseChanged;
                _battleManager = null;
            }

            if (_synergyManager != null)
            {
                _synergyManager.SynergyActivated -= HandleSynergyActivated;
                _synergyManager = null;
            }

            if (_gridManager != null)
            {
                _gridManager.PassengerDropped -= HandlePassengerDropped;
                _gridManager.MergeCompleted -= HandleMergeCompleted;
                _gridManager = null;
            }

            PassengerSellService.Sold -= HandlePassengerSold;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnbindBattle();
            BindSummon(null);
            BindAbility(null);
        }

        private void HandlePassengerSold(int slotIndex, string passengerId, int coins, int starLevel)
        {
            _analytics.Track(AnalyticsEventNames.PassengerSold, new Dictionary<string, object>
            {
                ["slot_index"] = slotIndex,
                ["passenger_id"] = passengerId ?? string.Empty,
                ["coins"] = coins,
                ["star"] = starLevel,
            });
        }

        private void HandleStationStarted(StationData station)
        {
            if (station != null)
            {
                _analytics.Context.StationIndex = station.StationIndex;
                _analytics.Context.WaveIndex = 0;
                _analytics.Context.DifficultyId = BuildDifficultyId(station);
            }

            _analytics.Track(AnalyticsEventNames.StationStarted, new Dictionary<string, object>
            {
                ["station_id"] = station?.Id ?? string.Empty,
                ["difficulty_multiplier"] = station != null ? station.DifficultyMultiplier : 1f,
            });
        }

        private static string BuildDifficultyId(StationData station)
        {
            if (station == null)
            {
                return AnalyticsContext.DefaultDifficultyId;
            }

            return $"station_{station.StationIndex}_x{station.DifficultyMultiplier:0.##}";
        }

        private void HandleStationCompleted(StationData station)
        {
            _analytics.Track(AnalyticsEventNames.StationCompleted, new Dictionary<string, object>
            {
                ["station_id"] = station?.Id ?? string.Empty,
            });
        }

        private void HandleWaveStarted(int waveIndex)
        {
            _analytics.Context.WaveIndex = waveIndex;
            _analytics.Track(AnalyticsEventNames.WaveStarted, new Dictionary<string, object>
            {
                ["wave_index"] = waveIndex,
            });
        }

        private void HandleWaveCompleted(int waveIndex)
        {
            _analytics.Track(AnalyticsEventNames.WaveCompleted, new Dictionary<string, object>
            {
                ["wave_index"] = waveIndex,
            });
        }

        private void HandleBossSpawned(EnemyRuntime enemy)
        {
            _analytics.Track(AnalyticsEventNames.BossStarted, new Dictionary<string, object>
            {
                ["enemy_id"] = enemy?.Data?.Id ?? string.Empty,
            });
        }

        private void HandleBossDespawned(EnemyRuntime enemy)
        {
            if (enemy == null || enemy.IsAlive)
            {
                return;
            }

            _analytics.Track(AnalyticsEventNames.BossDefeated, new Dictionary<string, object>
            {
                ["enemy_id"] = enemy.Data?.Id ?? string.Empty,
            });
        }

        private void HandleBossPhaseChanged(BossPhase previous, BossPhase next)
        {
            _analytics.Track(AnalyticsEventNames.BossPhaseChanged, new Dictionary<string, object>
            {
                ["previous_phase"] = previous.ToString(),
                ["next_phase"] = next.ToString(),
            });
        }

        private void HandleSynergyActivated(SynergyData synergy)
        {
            _analytics.Track(AnalyticsEventNames.SynergyActivated, new Dictionary<string, object>
            {
                ["synergy_id"] = synergy?.Id ?? string.Empty,
            });
        }

        private void HandlePassengerDropped(int fromSlot, int toSlot, GridDropResult result)
        {
            if (result == GridDropResult.Moved || result == GridDropResult.Swapped)
            {
                _analytics.Track(AnalyticsEventNames.PassengerMoved, new Dictionary<string, object>
                {
                    ["from_slot"] = fromSlot,
                    ["to_slot"] = toSlot,
                    ["drop_result"] = result.ToString(),
                });
            }
        }

        private void HandleMergeCompleted(MergeResult merge)
        {
            _analytics.Track(AnalyticsEventNames.PassengerMerged, new Dictionary<string, object>
            {
                ["from_slot"] = merge.SourceSlot,
                ["to_slot"] = merge.TargetSlot,
                ["result_star"] = merge.ResultingStarLevel,
                ["passenger_id"] = merge.PassengerId ?? string.Empty,
            });
        }

        private void HandleSummonRequested(SummonRequestResult result)
        {
            if (result != SummonRequestResult.Success || _summonManager == null)
            {
                return;
            }

            var ids = new List<object>();
            IReadOnlyList<PassengerData> offers = _summonManager.CurrentOffers;
            if (offers != null)
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i] != null)
                    {
                        ids.Add(offers[i].Id);
                    }
                }
            }

            _analytics.Track(AnalyticsEventNames.PassengerOfferShown, new Dictionary<string, object>
            {
                ["offer_ids"] = ids,
            });
        }

        private void HandleOfferSelected(SelectOfferResult result, PassengerRuntime placed)
        {
            if (result != SelectOfferResult.Success || placed?.Data == null)
            {
                return;
            }

            _analytics.Track(AnalyticsEventNames.PassengerSelected, new Dictionary<string, object>
            {
                ["passenger_id"] = placed.Data.Id,
                ["star"] = placed.StarLevel,
                ["slot_index"] = placed.GridSlotIndex,
            });
            _analytics.Track(AnalyticsEventNames.PassengerPlaced, new Dictionary<string, object>
            {
                ["passenger_id"] = placed.Data.Id,
                ["slot_index"] = placed.GridSlotIndex,
            });
        }

        private void HandleAbilityOffersGenerated(AbilityOfferResult result)
        {
            if (result != AbilityOfferResult.Success || _abilityManager == null)
            {
                return;
            }

            var ids = new List<object>();
            IReadOnlyList<AbilityData> offers = _abilityManager.CurrentOffers;
            if (offers != null)
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i] != null)
                    {
                        ids.Add(offers[i].Id);
                    }
                }
            }

            _analytics.Track(AnalyticsEventNames.AbilityOfferShown, new Dictionary<string, object>
            {
                ["offer_ids"] = ids,
            });
        }

        private void HandleAbilitySelected(AbilitySelectResult result, AbilityData ability)
        {
            if (result != AbilitySelectResult.Success || ability == null)
            {
                return;
            }

            _analytics.Track(AnalyticsEventNames.AbilitySelected, new Dictionary<string, object>
            {
                ["ability_id"] = ability.Id,
            });
        }
    }
}
