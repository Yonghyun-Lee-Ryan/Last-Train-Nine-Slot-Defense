using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.Mission;
using LastTrain.Passenger;
using LastTrain.Relic;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit49DailyRuleTests
    {
        private RunState _runState;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
        }

        [Test]
        public void GameDatabase_HasSixDailyRules()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsNotNull(db);
            Assert.AreEqual(6, db.DailyRules.Count);
            Assert.IsTrue(db.TryGetDailyRule("daily_rule_locked_seat", out _));
            Assert.IsTrue(db.TryGetDailyRule("daily_rule_summon_tax", out _));
            Assert.IsTrue(db.TryGetDailyRule("daily_rule_cheap_summon", out _));
            Assert.IsTrue(db.TryGetDailyRule("daily_rule_rush_hour", out _));
            Assert.IsTrue(db.TryGetDailyRule("daily_rule_lost_and_found", out _));
            Assert.IsTrue(db.TryGetDailyRule("daily_rule_no_dwell", out _));
        }

        [Test]
        public void SameDateAndVersion_ResolvesSameRule()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            int seedA = DailyRunService.ComputeSeed(new System.DateTime(2026, 8, 14, 1, 0, 0, System.DateTimeKind.Utc), "1.0.0");
            int seedB = DailyRunService.ComputeSeed(new System.DateTime(2026, 8, 14, 23, 0, 0, System.DateTimeKind.Utc), "1.0.0");
            int seedNext = DailyRunService.ComputeSeed(new System.DateTime(2026, 8, 15, 1, 0, 0, System.DateTimeKind.Utc), "1.0.0");

            DailyRuleData a = DailyRunService.ResolveRule(db.DailyRules, seedA);
            DailyRuleData b = DailyRunService.ResolveRule(db.DailyRules, seedB);
            DailyRuleData next = DailyRunService.ResolveRule(db.DailyRules, seedNext);

            Assert.IsNotNull(a);
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreNotEqual(seedA, seedNext);
            Assert.IsNotNull(next);
        }

        [Test]
        public void CreateDefault_DoesNotApplyDailyRule()
        {
            _runState.Initialize(RunStartConfig.CreateDefault());
            Assert.IsFalse(_runState.IsDailyRun);
            Assert.AreEqual(-1, _runState.LockedSlotIndex);
            Assert.AreEqual(1f, _runState.Difficulty.SummonCostMultiplier, 0.001f);
            Assert.IsTrue(string.IsNullOrEmpty(_runState.DailyRuleId));
        }

        [Test]
        public void LockSeat_SkipsLockedSlot()
        {
            var config = RunStartConfig.CreateDailyRun(7);
            config.DailyRuleId = "daily_rule_locked_seat";
            config.DailyRuleDisplayName = "공사 중 좌석";
            config.DailyLockedSlotIndex = 3;
            _runState.Initialize(config);

            Assert.IsTrue(_runState.IsSlotLocked(3));
            Assert.AreEqual(0, _runState.FindFirstEmptySlot());
            Assert.IsFalse(_runState.TryPlacePassenger(3, CreatePassenger("p")));
            Assert.IsTrue(_runState.TryPlacePassenger(0, CreatePassenger("q")));
            Assert.AreEqual(GridDropResult.Reverted, GridInteractionService.TryDrop(_runState, 0, 3));
        }

        [Test]
        public void SummonTax_ScalesDifficultyMultiplier()
        {
            var config = RunStartConfig.CreateDailyRun(11);
            config.DailySummonCostMultiplier = 1.25f;
            config.DailyEnemySpeedMultiplier = 1.15f;
            _runState.Initialize(config);

            Assert.AreEqual(1.25f, _runState.Difficulty.SummonCostMultiplier, 0.001f);
            Assert.AreEqual(1.15f, _runState.Difficulty.EnemyMoveSpeedMultiplier, 0.001f);
        }

        [Test]
        public void GrantRelic_AcquiresStartingRelic()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            var config = RunStartConfig.CreateDailyRun(13);
            config.DailyStartingRelicId = "relic_broken_card";
            _runState.Initialize(config);
            var relics = new RelicManager(_runState, db);
            Assert.IsTrue(relics.TryAcquire(_runState.DailyStartingRelicId));
            Assert.IsTrue(_runState.Relics.HasRelic("relic_broken_card"));
        }

        [Test]
        public void Briefing_IncludesDailyRuleName()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsTrue(db.TryGetStation("line1_station_01", out StationData station));
            var config = RunStartConfig.CreateDailyRun(3);
            config.DailyRuleDisplayName = "러시 아워";
            _runState.Initialize(config);

            StationBriefing briefing = StationBriefingBuilder.Build(
                station,
                _runState.Difficulty,
                _runState);
            Assert.IsTrue(briefing.ModifierHint.Contains("오늘 규칙: 러시 아워"));
        }

        [Test]
        public void Briefing_IncludesLockedSeatNumber()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsTrue(db.TryGetStation("line1_station_01", out StationData station));
            var config = RunStartConfig.CreateDailyRun(3);
            config.DailyRuleDisplayName = "공사 중 좌석";
            config.DailyLockedSlotIndex = 4;
            _runState.Initialize(config);

            StationBriefing briefing = StationBriefingBuilder.Build(
                station,
                _runState.Difficulty,
                _runState);
            Assert.IsTrue(briefing.ModifierHint.Contains("오늘 규칙: 공사 중 좌석"));
            Assert.IsTrue(briefing.ModifierHint.Contains("5번 칸 잠김"));
        }

        [Test]
        public void Briefing_IncludesRushHourSpeedPercent()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsTrue(db.TryGetStation("line1_station_01", out StationData station));
            var config = RunStartConfig.CreateDailyRun(3);
            config.DailyRuleDisplayName = "러시 아워";
            config.DailyEnemySpeedMultiplier = 1.3f;
            _runState.Initialize(config);

            StationBriefing briefing = StationBriefingBuilder.Build(
                station,
                _runState.Difficulty,
                _runState);
            Assert.IsTrue(briefing.ModifierHint.Contains("이속 +30%"));
        }

        [Test]
        public void Briefing_IncludesSummonCostPercent()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsTrue(db.TryGetStation("line1_station_01", out StationData station));
            var config = RunStartConfig.CreateDailyRun(3);
            config.DailyRuleDisplayName = "심야 할증";
            config.DailySummonCostMultiplier = 1.25f;
            _runState.Initialize(config);

            StationBriefing briefing = StationBriefingBuilder.Build(
                station,
                _runState.Difficulty,
                _runState);
            Assert.IsTrue(briefing.ModifierHint.Contains("소환 +25%"));
        }

        [Test]
        public void ResolveLockedSlot_WeightsCenterSeat()
        {
            int centerHits = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                int slot = DailyRunService.ResolveLockedSlot(seed, 6);
                Assert.GreaterOrEqual(slot, 0);
                Assert.Less(slot, RunState.GridSlotCount);
                if (slot == 4)
                {
                    centerHits++;
                }
            }

            Assert.Greater(centerHits, 30);
        }

        [Test]
        public void RushHourRule_UsesThirtyPercentSpeed()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsTrue(db.TryGetDailyRule("daily_rule_rush_hour", out DailyRuleData rule));
            Assert.AreEqual(1.3f, rule.Magnitude, 0.001f);
        }

        [Test]
        public void LockedSlot_ShowsKoreanLockLabel()
        {
            var go = new GameObject("LockedSlot", typeof(RectTransform), typeof(GridSlot));
            try
            {
                GridSlot slot = go.GetComponent<GridSlot>();
                slot.SetLocked(true);
                Transform label = go.transform.Find("LockLabel");
                Assert.IsNotNull(label);
                Assert.IsTrue(label.gameObject.activeSelf);
                Assert.AreEqual("잠김", label.GetComponent<UnityEngine.UI.Text>().text);

                slot.SetLocked(false);
                Assert.IsFalse(label.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static PassengerRuntime CreatePassenger(string id)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return PassengerRuntime.Create(data);
        }
    }
}
