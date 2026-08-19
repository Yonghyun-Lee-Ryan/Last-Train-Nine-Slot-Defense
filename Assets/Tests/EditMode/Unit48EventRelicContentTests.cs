using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Event;
using LastTrain.Relic;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit48EventRelicContentTests
    {
        private RunState _runState;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.Battle.StartRun();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
        }

        [Test]
        public void GameDatabase_HasTenEventsAndSixteenRelics()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsNotNull(db);
            Assert.AreEqual(10, db.Events.Count, "이벤트 카탈로그는 10종이어야 한다.");
            Assert.AreEqual(16, db.Relics.Count, "유물 카탈로그는 16종이어야 한다.");

            Assert.IsTrue(db.TryGetEvent("event_lost_wallet", out _));
            Assert.IsTrue(db.TryGetEvent("event_vending_jam", out _));
            Assert.IsTrue(db.TryGetEvent("event_last_delay", out _));
            Assert.IsTrue(db.TryGetEvent("event_ticket_gate", out _));
            Assert.IsTrue(db.TryGetEvent("event_lost_umbrella", out _));
            Assert.IsTrue(db.TryGetEvent("event_overtime_board", out _));
            Assert.IsTrue(db.TryGetEvent("event_platform_cat", out _));
            Assert.IsTrue(db.TryGetEvent("event_night_check", out _));
            Assert.IsTrue(db.TryGetEvent("event_last_call_cafe", out _));

            Assert.IsTrue(db.TryGetRelic("relic_broken_card", out _));
            Assert.IsTrue(db.TryGetRelic("relic_night_coffee", out _));
            Assert.IsTrue(db.TryGetRelic("relic_spare_fuse", out _));
            Assert.IsTrue(db.TryGetRelic("relic_coin_pouch", out _));
            Assert.IsTrue(db.TryGetRelic("relic_platform_bench", out _));
            Assert.IsTrue(db.TryGetRelic("relic_lost_umbrella", out _));
            Assert.IsTrue(db.TryGetRelic("relic_warm_pack", out _));
        }

        [Test]
        public void Line1_Station6_IsRestAndStation4_IsEvent()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsNotNull(db);

            Assert.IsTrue(db.TryGetStationByRouteIndex(RouteIds.Default, 6, out StationData rest));
            Assert.AreEqual(StationType.Rest, rest.StationType);
            Assert.IsFalse(rest.RequiresWaves);
            Assert.AreEqual(0, rest.WaveCount);

            Assert.IsTrue(db.TryGetStationByRouteIndex(RouteIds.Default, 4, out StationData eventStation));
            Assert.AreEqual(StationType.Event, eventStation.StationType);

            Assert.IsTrue(db.TryGetStationByRouteIndex(RouteIds.Default, 8, out StationData shop));
            Assert.AreEqual(StationType.Shop, shop.StationType);
        }

        [Test]
        public void Line1_RestStation_HealsOnActivate()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsTrue(db.TryGetStationByRouteIndex(RouteIds.Default, 6, out StationData rest));

            _runState.Train.ApplyDamage(20);
            int hpBefore = _runState.Train.CurrentHp;
            int completed = 0;
            var context = new StationHandlerContext(_runState, rest, () => completed++);

            Assert.IsTrue(RestStationHandler.Instance.TryActivate(context));
            Assert.Greater(_runState.Train.CurrentHp, hpBefore);
            Assert.AreEqual(1, completed);
        }

        [Test]
        public void UnusedEventEffects_GrantRelicRemoveCoinsAndNextStationBuff()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            var relicManager = new RelicManager(_runState, db);
            _runState.Currency.AddCoins(40);
            int coinsBefore = _runState.Currency.CurrentCoins;

            var effects = new[]
            {
                new EventEffectData { effectType = EventEffectType.RemoveCoins, value = -20f },
                new EventEffectData { effectType = EventEffectType.GrantRelic, targetId = "relic_night_coffee" },
                new EventEffectData { effectType = EventEffectType.NextStationEnemyBuff, value = 1.25f },
                new EventEffectData { effectType = EventEffectType.NextStationRewardBonus, value = 1.5f },
            };

            Assert.IsTrue(EventEffectApplier.ApplyAll(_runState, db, relicManager, effects, 0f));
            Assert.AreEqual(coinsBefore - 20, _runState.Currency.CurrentCoins);
            Assert.IsTrue(_runState.Relics.HasRelic("relic_night_coffee"));
            Assert.AreEqual(1.25f, _runState.NextStationModifiers.EnemyHealthMultiplier, 0.001f);
            Assert.AreEqual(1.5f, _runState.NextStationModifiers.RewardCoinMultiplier, 0.001f);
        }

        [Test]
        public void NewRelics_StackExistingEffectTypes()
        {
            RelicData hat = CreateRelic("hat", RelicEffectType.TrainMaxHpFlat, 15f);
            RelicData bench = CreateRelic("bench", RelicEffectType.TrainMaxHpFlat, 10f);
            RelicModifiers modifiers = RelicEffectAggregator.Compute(
                new[] { new RelicRuntime(hat), new RelicRuntime(bench) });

            Assert.AreEqual(25, modifiers.TrainMaxHpFlat);
            Object.DestroyImmediate(hat);
            Object.DestroyImmediate(bench);
        }

        private static RelicData CreateRelic(string id, RelicEffectType type, float value)
        {
            var data = ScriptableObject.CreateInstance<RelicData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("effectType").enumValueIndex = (int)type;
            so.FindProperty("effectValue").floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
