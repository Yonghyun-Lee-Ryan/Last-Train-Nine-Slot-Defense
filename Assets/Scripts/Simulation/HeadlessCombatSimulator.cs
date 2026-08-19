using System;
using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Mission;
using LastTrain.Passenger;
using LastTrain.Passenger.Skills;
using LastTrain.Relic;
using LastTrain.Run;
using LastTrain.Synergy;
using UnityEngine;

namespace LastTrain.Simulation
{
    /// <summary>
    /// GameObject 없이 StationManager + 전투 서비스를 틱하는 헤드리스 시뮬레이터.
    /// AppRoot/저장/메타 진행을 거치지 않는다.
    /// </summary>
    public sealed class HeadlessCombatSimulator
    {
        private static readonly Vector2 TrainPosition = BattleConstants.TrainTargetAnchoredPosition;
        private static readonly Vector2 SpawnPosition = BattleConstants.SpawnAnchoredPosition;
        private static readonly Vector2[] Waypoints = BattleConstants.EnemyWaypointAnchoredPositions;

        public BattleSimulationAggregate RunBatch(BattleSimulationConfig config, GameDatabase database)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            int iterations = Math.Max(1, config.iterations);
            var aggregate = new BattleSimulationAggregate
            {
                Iterations = iterations,
                MinRemainingHp = float.MaxValue,
                MaxRemainingHp = float.MinValue,
                MinSimulatedSeconds = float.MaxValue,
                MaxSimulatedSeconds = float.MinValue,
            };

            var remainingHpSamples = new List<float>(iterations);
            var timeSamples = new List<float>(iterations);
            var damageTotals = new Dictionary<string, float>(StringComparer.Ordinal);
            var skillTotals = new Dictionary<string, float>(StringComparer.Ordinal);
            var reachTotals = new Dictionary<string, float>(StringComparer.Ordinal);

            for (int i = 0; i < iterations; i++)
            {
                int seed = unchecked(config.baseSeed + i * 997);
                BattleSimulationRunResult run = RunOnce(config, database, seed);
                aggregate.Runs.Add(run);

                if (run.IsVictory)
                {
                    aggregate.Wins++;
                }

                remainingHpSamples.Add(run.RemainingTrainHp);
                timeSamples.Add(run.SimulatedSeconds);
                aggregate.MinRemainingHp = Math.Min(aggregate.MinRemainingHp, run.RemainingTrainHp);
                aggregate.MaxRemainingHp = Math.Max(aggregate.MaxRemainingHp, run.RemainingTrainHp);
                aggregate.MinSimulatedSeconds = Math.Min(aggregate.MinSimulatedSeconds, run.SimulatedSeconds);
                aggregate.MaxSimulatedSeconds = Math.Max(aggregate.MaxSimulatedSeconds, run.SimulatedSeconds);

                Accumulate(damageTotals, run.DamageByPassengerId);
                Accumulate(skillTotals, run.SkillTicksByPassengerId);
                Accumulate(reachTotals, run.TrainReachesByEnemyId);
            }

            aggregate.WinRate = aggregate.Wins / (float)iterations;
            aggregate.AvgRemainingHp = Average(remainingHpSamples);
            aggregate.StdDevRemainingHp = StdDev(remainingHpSamples, aggregate.AvgRemainingHp);
            aggregate.AvgSimulatedSeconds = Average(timeSamples);
            aggregate.AvgDamageByPassengerId = AverageMap(damageTotals, iterations);
            aggregate.AvgSkillTicksByPassengerId = AverageMap(skillTotals, iterations);
            aggregate.AvgTrainReachesByEnemyId = AverageMap(reachTotals, iterations);
            aggregate.DifficultyId = config.difficultyId ?? string.Empty;
            FillAggregateExtras(aggregate, config);

            if (aggregate.MinRemainingHp == float.MaxValue)
            {
                aggregate.MinRemainingHp = 0f;
            }

            if (aggregate.MaxRemainingHp == float.MinValue)
            {
                aggregate.MaxRemainingHp = 0f;
            }

            return aggregate;
        }

        public BattleSimulationRunResult RunOnce(
            BattleSimulationConfig config,
            GameDatabase database,
            int seed)
        {
            var result = new BattleSimulationRunResult { Seed = seed };
            var random = new RandomService(seed);

            var startConfig = RunStartConfig.CreateDefault();
            startConfig.InitialCoins = Math.Max(0, config.initialCoins);
            startConfig.InitialTrainMaxHp = Math.Max(1, config.initialTrainHp);
            startConfig.InitialTrainCurrentHp = Math.Max(1, config.initialTrainHp);
            startConfig.InitialStationIndex = Math.Max(1, config.startingStationIndex);
            if (!string.IsNullOrWhiteSpace(config.lineId))
            {
                startConfig.LineId = config.lineId;
            }

            if (!string.IsNullOrWhiteSpace(config.difficultyId))
            {
                startConfig.DifficultyId = config.difficultyId;
            }

            if (config.isEndlessRun)
            {
                startConfig.IsEndlessRun = true;
                if (string.IsNullOrWhiteSpace(config.lineId))
                {
                    startConfig.LineId = RouteIds.Endless;
                }
            }

            if (config.isDailyRun)
            {
                startConfig.IsDailyRun = true;
                startConfig.RandomSeed = seed == 0 ? 1 : seed;
                DailyRuleData rule = null;
                if (!string.IsNullOrWhiteSpace(config.dailyRuleId))
                {
                    database.TryGetDailyRule(config.dailyRuleId, out rule);
                }

                if (rule == null)
                {
                    rule = DailyRunService.ResolveRule(database.DailyRules, startConfig.RandomSeed);
                }

                DailyRunService.BindRule(
                    startConfig,
                    rule,
                    startConfig.RandomSeed,
                    database.DailyRules != null ? database.DailyRules.Count : 0);
            }

            var runState = new RunState();
            runState.Initialize(startConfig);
            runState.Battle.StartRun();

            if (runState.IsDailyRun && !string.IsNullOrWhiteSpace(runState.DailyStartingRelicId))
            {
                var relicManager = new RelicManager(runState, database);
                relicManager.TryAcquire(runState.DailyStartingRelicId);
            }

            PlacePassengers(runState, database, config);
            ApplyAbilities(runState, database, config);
            SynergyEffectApplier.Refresh(runState);
            AbilityEffectApplier.Refresh(runState, config.initialTrainHp);

            var registry = new EnemyRegistry();
            var launcher = new InstantHitProjectileLauncher();
            var passengerControllers = BuildPassengerControllers(runState);
            var context = new HeadlessBattleContext(
                runState,
                registry,
                config.difficultyMultiplier,
                random,
                OnEnemyReachedTrain);

            bool victory = false;
            bool defeat = false;
            runState.Train.Destroyed += () => defeat = true;

            void OnEnemyReachedTrain(EnemyRuntime enemy)
            {
                if (enemy?.Data == null)
                {
                    return;
                }

                string id = enemy.Data.Id;
                if (!result.TrainReachesByEnemyId.TryGetValue(id, out int count))
                {
                    count = 0;
                }

                result.TrainReachesByEnemyId[id] = count + 1;
            }

            CombatVisualEvents.EnemyDamaged += HandleDamaged;

            void HandleDamaged(EnemyRuntime enemy, float damage, bool isCrit)
            {
                string passengerId = launcher.LastPassengerId;
                if (string.IsNullOrWhiteSpace(passengerId))
                {
                    return;
                }

                if (!result.DamageByPassengerId.TryGetValue(passengerId, out float total))
                {
                    total = 0f;
                }

                result.DamageByPassengerId[passengerId] = total + damage;
            }

            try
            {
                if ((!database.TryGetStationByRouteIndex(
                        runState.LineId,
                        startConfig.InitialStationIndex,
                        out StationData startStation)
                    && !database.TryGetStationByIndex(startConfig.InitialStationIndex, out startStation))
                    || startStation == null)
                {
                    result.RemainingTrainHp = runState.Train.CurrentHp;
                    result.TrainMaxHp = runState.Train.MaxHp;
                    return result;
                }

                var stationManager = new StationManager(index =>
                {
                    if (database.TryGetStationByRouteIndex(runState.LineId, index, out StationData routeStation))
                    {
                        return routeStation;
                    }

                    database.TryGetStationByIndex(index, out StationData station);
                    return station;
                });

                stationManager.RunVictoryRequested += () => victory = true;
                stationManager.AbilityRewardRequested += _ =>
                {
                    if (config.autoContinueAbilityRewards)
                    {
                        stationManager.ContinueAfterAbilityReward();
                    }
                };
                stationManager.Initialize(runState, startStation);
                stationManager.TryActivateStation();

                float elapsed = 0f;
                float dt = Mathf.Max(0.01f, config.deltaTime);
                float maxTime = Mathf.Max(dt, config.maxSimulatedSeconds);
                int maxStation = Math.Max(startConfig.InitialStationIndex, config.maxStationIndex);

                while (!victory && !defeat && elapsed < maxTime && runState.Battle.IsRunActive)
                {
                    if (runState.Station.CurrentStationIndex > maxStation && !victory)
                    {
                        // 지정 역까지만 시뮬할 때, 그 이상 진행되면 생존 승리로 간주
                        victory = true;
                        break;
                    }

                    stationManager.Tick(dt, context);
                    TickCombat(dt, runState, registry, passengerControllers, launcher, result);

                    if (stationManager.CurrentPhase == RunPhase.Preparing
                        && !stationManager.IsWaitingForAbilityReward
                        && !victory
                        && runState.Battle.IsRunActive
                        && runState.Station.CurrentStationIndex <= maxStation)
                    {
                        stationManager.TryActivateStation();
                    }

                    if (runState.Station.CurrentStationIndex > maxStation && !victory)
                    {
                        victory = true;
                        break;
                    }

                    elapsed += dt;
                }

                result.IsVictory = victory && !defeat;
                result.SimulatedSeconds = elapsed;
                result.RemainingTrainHp = runState.Train.CurrentHp;
                result.TrainMaxHp = runState.Train.MaxHp;
                result.RemainingCoins = runState.Currency.CurrentCoins;
                int reached = runState.Station.CurrentStationIndex;
                result.ReachedStationIndex = victory && reached > maxStation ? maxStation : reached;
                result.DifficultyId = config.difficultyId ?? string.Empty;
                result.EnemiesKilled = runState.History.EnemiesKilled;
                result.BossesKilled = runState.History.BossesKilled;

                if (runState.Synergies?.Active != null)
                {
                    for (int s = 0; s < runState.Synergies.Active.Count; s++)
                    {
                        var synergy = runState.Synergies.Active[s];
                        if (synergy == null || string.IsNullOrWhiteSpace(synergy.Id))
                        {
                            continue;
                        }

                        result.SynergyActivations[synergy.Id] = 1;
                    }
                }

                return result;
            }
            finally
            {
                CombatVisualEvents.EnemyDamaged -= HandleDamaged;
                runState.Dispose();
            }
        }

        private static void TickCombat(
            float deltaTime,
            RunState runState,
            EnemyRegistry registry,
            List<PassengerController> passengers,
            InstantHitProjectileLauncher launcher,
            BattleSimulationRunResult result)
        {
            if (!BattlePhaseUtility.IsCombatActive(runState.Battle.CurrentPhase))
            {
                return;
            }

            IReadOnlyList<EnemyRuntime> enemies = registry.Enemies;
            var reached = new List<EnemyRuntime>();

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntime enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                bool toWaypoint = TryGetWaypoint(enemy.RouteWaypointIndex, out Vector2 target);
                if (!toWaypoint)
                {
                    target = TrainPosition;
                }

                enemy.SetRouteSegment(GetSegmentStart(enemy.RouteWaypointIndex, enemy.SpawnPosition), target);
                bool reachedTarget = EnemyMovementService.TickMove(
                    enemy,
                    target,
                    deltaTime,
                    BattleConstants.MoveSpeedToWorldScale,
                    toWaypoint ? 8f : 32f);

                if (!enemy.IsTargetable
                    && Vector2.Distance(enemy.SpawnPosition, enemy.Position)
                    >= BattleConstants.SpawnTargetProtectionDistance)
                {
                    enemy.SetTargetable(true);
                }

                if (reachedTarget && toWaypoint)
                {
                    enemy.AdvanceRouteWaypoint();
                }
                else if (reachedTarget)
                {
                    reached.Add(enemy);
                }
            }

            for (int i = 0; i < reached.Count; i++)
            {
                TrainDamageService.TryApplyTrainDamage(runState, reached[i]);
            }

            float fastBonus = runState.Synergies?.Modifiers?.FastEnemyDamagePercent ?? 0f;
            float bossBonus = runState.Abilities?.Modifiers?.PoliceBossDamagePercent ?? 0f;
            AbilityModifiers modifiers = runState.Abilities?.Modifiers ?? AbilityModifiers.Empty;
            SynergyModifiers synergyModifiers = runState.Synergies?.Modifiers ?? SynergyModifiers.Empty;
            Relic.RelicModifiers relicModifiers = runState.Relics?.Modifiers ?? Relic.RelicModifiers.Empty;
            var skillRandom = new RandomService(result.Seed ^ 0x5f3759df);

            for (int i = 0; i < passengers.Count; i++)
            {
                PassengerController controller = passengers[i];
                PassengerRuntime runtime = controller.Runtime;
                if (runtime == null || runtime.GridSlotIndex < 0)
                {
                    continue;
                }

                Vector2 worldPos = GetSlotWorldPosition(runtime.GridSlotIndex);
                float range = BattleConstants.ToWorldRange(runtime.GetEffectiveRange());
                var skillContext = new PassengerSkillContext(
                    runtime,
                    worldPos,
                    range,
                    enemies,
                    runState.Train,
                    modifiers,
                    SpawnPosition,
                    TrainPosition,
                    null,
                    skillRandom,
                    synergyModifiers,
                    relicModifiers.CritChancePercent,
                    relicModifiers.DeveloperTurretDurationPercent);

                bool attacked = controller.Tick(
                    deltaTime,
                    worldPos,
                    range,
                    enemies,
                    launcher,
                    skillContext,
                    fastBonus,
                    bossBonus);

                if (attacked)
                {
                    string id = runtime.Data.Id;
                    if (!result.SkillTicksByPassengerId.TryGetValue(id, out int count))
                    {
                        count = 0;
                    }

                    result.SkillTicksByPassengerId[id] = count + 1;
                }
            }
        }

        private static void PlacePassengers(
            RunState runState,
            GameDatabase database,
            BattleSimulationConfig config)
        {
            if (config.slots == null)
            {
                return;
            }

            int count = Math.Min(config.slots.Length, RunState.GridSlotCount);
            for (int i = 0; i < count; i++)
            {
                BattleSimulationSlotConfig slot = config.slots[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.passengerId) || runState.IsSlotLocked(i))
                {
                    continue;
                }

                if (!database.TryGetPassenger(slot.passengerId, out PassengerData data) || data == null)
                {
                    continue;
                }

                PassengerRuntime passenger = PassengerRuntime.Create(data, Math.Max(1, slot.starLevel));
                runState.TryPlacePassengerFromSave(i, passenger);
            }
        }

        private static void ApplyAbilities(
            RunState runState,
            GameDatabase database,
            BattleSimulationConfig config)
        {
            if (config.abilityIds == null)
            {
                return;
            }

            for (int i = 0; i < config.abilityIds.Length; i++)
            {
                string id = config.abilityIds[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!database.TryGetAbility(id, out AbilityData ability) || ability == null)
                {
                    continue;
                }

                runState.Abilities.AddSelected(ability);
            }
        }

        private static List<PassengerController> BuildPassengerControllers(RunState runState)
        {
            var list = new List<PassengerController>();
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(i);
                if (passenger == null)
                {
                    continue;
                }

                list.Add(PassengerFactory.CreateController(passenger));
            }

            return list;
        }

        private static Vector2 GetSlotWorldPosition(int slotIndex)
        {
            int row = slotIndex / 3;
            int col = slotIndex % 3;
            Vector2 gridOrigin = BattleConstants.GridAnchoredPosition;
            Vector2 cell = BattleConstants.GridCellSize;
            Vector2 spacing = BattleConstants.GridSpacing;
            float x = gridOrigin.x + (col - 1) * (cell.x + spacing.x);
            float y = gridOrigin.y + (1 - row) * (cell.y + spacing.y);
            return new Vector2(x, y);
        }

        private static bool TryGetWaypoint(int waypointIndex, out Vector2 position)
        {
            if (waypointIndex >= 0 && waypointIndex < Waypoints.Length)
            {
                position = Waypoints[waypointIndex];
                return true;
            }

            position = default;
            return false;
        }

        private static Vector2 GetSegmentStart(int waypointIndex, Vector2 spawn)
        {
            if (waypointIndex <= 0)
            {
                return spawn;
            }

            if (waypointIndex - 1 < Waypoints.Length)
            {
                return Waypoints[waypointIndex - 1];
            }

            return spawn;
        }

        private static void Accumulate(Dictionary<string, float> totals, Dictionary<string, float> values)
        {
            foreach (KeyValuePair<string, float> pair in values)
            {
                if (!totals.TryGetValue(pair.Key, out float current))
                {
                    current = 0f;
                }

                totals[pair.Key] = current + pair.Value;
            }
        }

        private static void Accumulate(Dictionary<string, float> totals, Dictionary<string, int> values)
        {
            foreach (KeyValuePair<string, int> pair in values)
            {
                if (!totals.TryGetValue(pair.Key, out float current))
                {
                    current = 0f;
                }

                totals[pair.Key] = current + pair.Value;
            }
        }

        private static void FillAggregateExtras(BattleSimulationAggregate aggregate, BattleSimulationConfig config)
        {
            if (aggregate?.Runs == null || aggregate.Runs.Count == 0)
            {
                return;
            }

            int n = aggregate.Runs.Count;
            float coins = 0f;
            int reach5 = 0;
            var failCounts = new Dictionary<int, int>();
            var reachCounts = new Dictionary<int, int>();
            var passengerPicks = new Dictionary<string, int>(StringComparer.Ordinal);
            var abilityPicks = new Dictionary<string, int>(StringComparer.Ordinal);
            var synergyCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            if (config?.slots != null)
            {
                for (int i = 0; i < config.slots.Length; i++)
                {
                    string id = config.slots[i]?.passengerId;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    passengerPicks[id] = passengerPicks.TryGetValue(id, out int c) ? c + 1 : 1;
                }
            }

            if (config?.abilityIds != null)
            {
                for (int i = 0; i < config.abilityIds.Length; i++)
                {
                    string id = config.abilityIds[i];
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    abilityPicks[id] = abilityPicks.TryGetValue(id, out int c) ? c + 1 : 1;
                }
            }

            int maxStation = Math.Max(1, config != null ? config.maxStationIndex : 1);
            for (int i = 0; i < n; i++)
            {
                BattleSimulationRunResult run = aggregate.Runs[i];
                coins += run.RemainingCoins;
                int reached = Math.Max(1, run.ReachedStationIndex);
                if (reached >= 5 || run.IsVictory)
                {
                    reach5++;
                }

                if (!run.IsVictory)
                {
                    failCounts[reached] = failCounts.TryGetValue(reached, out int fc) ? fc + 1 : 1;
                }

                for (int s = 1; s <= maxStation; s++)
                {
                    if (reached >= s || run.IsVictory)
                    {
                        reachCounts[s] = reachCounts.TryGetValue(s, out int rc) ? rc + 1 : 1;
                    }
                }

                foreach (KeyValuePair<string, int> pair in run.SynergyActivations)
                {
                    synergyCounts[pair.Key] = synergyCounts.TryGetValue(pair.Key, out int sc)
                        ? sc + 1
                        : 1;
                }
            }

            aggregate.AvgRemainingCoins = coins / n;
            aggregate.ReachStation5Rate = reach5 / (float)n;

            foreach (KeyValuePair<int, int> pair in failCounts)
            {
                aggregate.FailRateByStationIndex[pair.Key] = pair.Value / (float)n;
            }

            for (int s = 1; s <= maxStation; s++)
            {
                float rate = reachCounts.TryGetValue(s, out int count) ? count / (float)n : 0f;
                aggregate.SurvivalCurveByStation[s] = rate;
            }

            foreach (KeyValuePair<string, int> pair in passengerPicks)
            {
                aggregate.PassengerPickRate[pair.Key] = 1f; // 고정 슬롯 구성이면 픽률 100%
            }

            foreach (KeyValuePair<string, int> pair in abilityPicks)
            {
                aggregate.AbilityPickRate[pair.Key] = 1f;
            }

            foreach (KeyValuePair<string, int> pair in synergyCounts)
            {
                aggregate.SynergyActivationRate[pair.Key] = pair.Value / (float)n;
            }
        }

        private static float Average(List<float> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0f;
            }

            double sum = 0d;
            for (int i = 0; i < samples.Count; i++)
            {
                sum += samples[i];
            }

            return (float)(sum / samples.Count);
        }

        private static float StdDev(List<float> samples, float mean)
        {
            if (samples == null || samples.Count <= 1)
            {
                return 0f;
            }

            double sumSq = 0d;
            for (int i = 0; i < samples.Count; i++)
            {
                double d = samples[i] - mean;
                sumSq += d * d;
            }

            return (float)Math.Sqrt(sumSq / samples.Count);
        }

        private static Dictionary<string, float> AverageMap(Dictionary<string, float> totals, int iterations)
        {
            var result = new Dictionary<string, float>(StringComparer.Ordinal);
            float denom = Math.Max(1, iterations);
            foreach (KeyValuePair<string, float> pair in totals)
            {
                result[pair.Key] = pair.Value / denom;
            }

            return result;
        }

        private sealed class HeadlessBattleContext : IBattleFlowContext
        {
            private readonly RunState _runState;
            private readonly EnemyRegistry _registry;
            private readonly float _difficulty;
            private readonly RandomService _random;
            private readonly Action<EnemyRuntime> _onReached;

            public HeadlessBattleContext(
                RunState runState,
                EnemyRegistry registry,
                float difficulty,
                RandomService random,
                Action<EnemyRuntime> onReached)
            {
                _runState = runState;
                _registry = registry;
                _difficulty = Mathf.Max(0.01f, difficulty);
                _random = random;
                _onReached = onReached;
            }

            public bool TrySpawnEnemy(EnemyData enemyData)
            {
                if (enemyData == null || _runState == null)
                {
                    return false;
                }

                // 시드 기반 미세 스폰 오프셋으로 재현 가능한 분산
                float ox = (_random.NextFloat() - 0.5f) * 20f;
                Vector2 spawn = SpawnPosition + new Vector2(ox, 0f);
                EnemyRuntime runtime = EnemyFactory.CreateRuntime(
                    enemyData,
                    spawn,
                    _difficulty,
                    _runState?.Difficulty);
                runtime.SetRouteWaypointIndex(0);
                runtime.SetTargetable(false);
                runtime.Died += HandleKilled;
                runtime.ReachedTrain += HandleReached;
                _registry.Register(runtime);
                _runState.RecordEnemyEncounter(runtime);
                return true;
            }

            public int GetAliveEnemyCount()
            {
                return _registry.Enemies.Count;
            }

            private void HandleKilled(EnemyRuntime enemy)
            {
                EnemyRewardService.TryGrantKillReward(_runState, enemy);
            }

            private void HandleReached(EnemyRuntime enemy)
            {
                _onReached?.Invoke(enemy);
            }
        }
    }
}
