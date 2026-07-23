using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Endless;
using LastTrain.Leaderboard;
using LastTrain.Run;
using LastTrain.Save;
using LastTrain.Score;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class EndlessModeTests
    {
        [Test]
        public void ScoreCalculator_IsDeterministic_ForSameInput()
        {
            var input = new ScoreInput(
                reachedStationIndex: 42,
                completedStationCount: 41,
                enemiesKilled: 120,
                bossesKilled: 8,
                remainingTrainHp: 55,
                difficultyId: DifficultyIds.Express,
                adsUsed: false);

            int a = ScoreCalculator.Calculate(input);
            int b = ScoreCalculator.Calculate(input);
            Assert.AreEqual(a, b);

            ScoreBreakdown breakdown = ScoreCalculator.CalculateBreakdown(input);
            Assert.AreEqual(a, breakdown.Total);
            Assert.Greater(breakdown.NoAdsBonus, 0);
            Assert.Greater(breakdown.DifficultyBonus, 0);
        }

        [Test]
        public void EndlessRoute_ResolvesAtLeast50Stations_BossEvery5_DifficultyGrows()
        {
            EndlessRouteData route = CreateEndlessRoute();
            float previousBase = 0f;
            for (int i = 1; i <= 55; i++)
            {
                Assert.IsTrue(route.TryGetStationByIndex(i, out StationData station), $"station {i}");
                Assert.IsNotNull(station);
                Assert.AreEqual(i, station.StationIndex);

                bool isBoss = i % 5 == 0;
                if (isBoss)
                {
                    Assert.AreEqual(StationType.Boss, station.StationType, $"boss at {i}");
                }

                // 기본 성장은 역 번호에 따라 단조 증가. 보스는 그 위에 보너스가 붙는다.
                float baseDifficulty = route.ComputeDifficultyMultiplier(i, isBoss: false);
                Assert.GreaterOrEqual(baseDifficulty, previousBase - 0.0001f, $"base growth at {i}");
                previousBase = baseDifficulty;

                if (isBoss)
                {
                    Assert.Greater(
                        station.DifficultyMultiplier,
                        baseDifficulty,
                        $"boss bonus at {i}");
                }
                else
                {
                    Assert.AreEqual(baseDifficulty, station.DifficultyMultiplier, 0.0001f);
                }
            }

            Assert.Greater(
                route.ComputeDifficultyMultiplier(55, isBoss: false),
                route.ComputeDifficultyMultiplier(1, isBoss: false));
        }

        [Test]
        public void Leaderboard_RejectsDuplicateRunId_AndUpdatesLocalBest()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            var mock = new MockLeaderboardService();

            RunResult result = CreateEndlessResult("run-endless-1", reached: 12, kills: 30, bosses: 2, hp: 40);
            LeaderboardSubmitResult first = EndlessProgressService.TrySubmitRun(
                meta,
                result,
                runState: null,
                mock,
                out int score,
                out bool bestUpdated);

            Assert.AreEqual(LeaderboardSubmitResult.Success, first);
            Assert.IsTrue(bestUpdated);
            Assert.AreEqual(score, meta.endlessBestScore);
            Assert.AreEqual(1, mock.SubmitCount);

            LeaderboardSubmitResult dup = EndlessProgressService.TrySubmitRun(
                meta,
                result,
                runState: null,
                mock,
                out _,
                out _);
            Assert.AreEqual(LeaderboardSubmitResult.DuplicateRunId, dup);
            Assert.AreEqual(1, mock.SubmitCount);
        }

        [Test]
        public void EndlessUnlock_RequiresRouteClear()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            Assert.IsFalse(EndlessProgressService.IsUnlocked(meta));

            meta.difficultyRecords = new[]
            {
                new MetaDifficultyRecord
                {
                    difficultyId = DifficultyIds.Normal,
                    clearCount = 1,
                    highestStationReached = 10,
                },
            };
            Assert.IsTrue(EndlessProgressService.IsUnlocked(meta));
        }

        [Test]
        public void CreateEndlessRun_SetsLineAndFlag()
        {
            RunStartConfig config = RunStartConfig.CreateEndlessRun(DifficultyIds.Express);
            Assert.IsTrue(config.IsEndlessRun);
            Assert.AreEqual(RouteIds.Endless, config.LineId);
            Assert.AreEqual(DifficultyIds.Express, config.DifficultyId);
        }

        [Test]
        public void DepthModifiers_ActivateByStationIndex()
        {
            DifficultyModifierData early = ScriptableObject.CreateInstance<DifficultyModifierData>();
            var so = new UnityEditor.SerializedObject(early);
            so.FindProperty("id").stringValue = "e10";
            so.FindProperty("modifierKind").enumValueIndex = (int)DifficultyModifierKind.EscalatingEnemies;
            so.FindProperty("magnitude").floatValue = 1.15f;
            so.FindProperty("stationIndexMin").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();

            var runtime = DifficultyRuntime.Identity.WithAdditionalModifiers(new[] { early });
            List<IDifficultyModifier> at5 = DifficultyModifierFactory.CreateActiveModifiers(runtime, 5);
            List<IDifficultyModifier> at10 = DifficultyModifierFactory.CreateActiveModifiers(runtime, 10);
            Assert.AreEqual(0, at5.Count);
            Assert.AreEqual(1, at10.Count);
        }

        private static EndlessRouteData CreateEndlessRoute()
        {
            StationData normal = ScriptableObject.CreateInstance<StationData>();
            var nSo = new UnityEditor.SerializedObject(normal);
            nSo.FindProperty("id").stringValue = "pat_normal";
            nSo.FindProperty("displayName").stringValue = "N";
            nSo.FindProperty("stationType").enumValueIndex = (int)StationType.Normal;
            nSo.FindProperty("stationIndex").intValue = 1;
            nSo.FindProperty("difficultyMultiplier").floatValue = 1f;
            nSo.ApplyModifiedPropertiesWithoutUndo();

            StationData boss = ScriptableObject.CreateInstance<StationData>();
            var bSo = new UnityEditor.SerializedObject(boss);
            bSo.FindProperty("id").stringValue = "pat_boss";
            bSo.FindProperty("displayName").stringValue = "B";
            bSo.FindProperty("stationType").enumValueIndex = (int)StationType.Boss;
            bSo.FindProperty("stationIndex").intValue = 5;
            bSo.FindProperty("difficultyMultiplier").floatValue = 1.5f;
            bSo.ApplyModifiedPropertiesWithoutUndo();

            EndlessRouteData route = ScriptableObject.CreateInstance<EndlessRouteData>();
            route.EditorSet(
                RouteIds.Endless,
                "test",
                new[] { normal },
                boss,
                interval: 5,
                growth: 0.08f,
                bossBonus: 0.35f,
                modifiers: null);
            return route;
        }

        private static RunResult CreateEndlessResult(string runId, int reached, int kills, int bosses, int hp)
        {
            return new RunResult(
                runId,
                RouteIds.Endless,
                isVictory: false,
                RunEndReason.Defeat,
                reachedStationIndex: reached,
                completedStationCount: Mathf.Max(0, reached - 1),
                enemiesKilled: kills,
                bossesKilled: bosses,
                mergeCount: 0,
                highestPassengerStar: 2,
                remainingTrainHp: hp,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0,
                difficultyId: DifficultyIds.Normal,
                isEndlessRun: true,
                adsUsed: false);
        }
    }
}
