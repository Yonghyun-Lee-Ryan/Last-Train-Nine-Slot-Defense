using System;
using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Passenger;
using LastTrain.Passenger.Skills;
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

            var runState = new RunState();
            runState.Initialize(startConfig);
            runState.Battle.StartRun();

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
                if (!database.TryGetStationByIndex(startConfig.InitialStationIndex, out StationData startStation)
                    || startStation == null)
                {
                    result.RemainingTrainHp = runState.Train.CurrentHp;
                    result.TrainMaxHp = runState.Train.MaxHp;
                    return result;
                }

                var stationManager = new StationManager(index =>
                {
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
                stationManager.TryStartNextWave();

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
                        && runState.Battle.IsRunActive)
                    {
                        stationManager.TryStartNextWave();
                    }

                    elapsed += dt;
                }

                result.IsVictory = victory && !defeat;
                result.SimulatedSeconds = elapsed;
                result.RemainingTrainHp = runState.Train.CurrentHp;
                result.TrainMaxHp = runState.Train.MaxHp;
                result.EnemiesKilled = runState.History.EnemiesKilled;
                result.BossesKilled = runState.History.BossesKilled;
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
                    synergyModifiers);

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
                if (slot == null || string.IsNullOrWhiteSpace(slot.passengerId))
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
                EnemyRuntime runtime = EnemyFactory.CreateRuntime(enemyData, spawn, _difficulty);
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
