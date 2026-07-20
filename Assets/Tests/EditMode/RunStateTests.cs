using LastTrain.Run;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class RunStateTests
    {
        private RunState _runState;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
        }

        [Test]
        public void Initialize_SetsDefaultValues()
        {
            Assert.IsFalse(string.IsNullOrEmpty(_runState.RunId));
            Assert.AreEqual(100, _runState.Train.MaxHp);
            Assert.AreEqual(100, _runState.Train.CurrentHp);
            Assert.AreEqual(0, _runState.Currency.CurrentCoins);
            Assert.AreEqual(1, _runState.Station.CurrentStationIndex);
            Assert.AreEqual(RunPhase.None, _runState.Battle.CurrentPhase);
        }

        [Test]
        public void TryPlacePassenger_OnEmptySlot_Succeeds()
        {
            var passenger = CreateTestPassenger();

            bool placed = _runState.TryPlacePassenger(4, passenger);

            Assert.IsTrue(placed);
            Assert.AreSame(passenger, _runState.GetPassengerAtSlot(4));
            Assert.AreEqual(4, passenger.GridSlotIndex);
        }

        [Test]
        public void TrySwapSlots_ExchangesPassengers()
        {
            var passengerA = CreateTestPassenger();
            var passengerB = CreateTestPassenger();
            _runState.TryPlacePassenger(0, passengerA);
            _runState.TryPlacePassenger(8, passengerB);

            _runState.TrySwapSlots(0, 8);

            Assert.AreSame(passengerB, _runState.GetPassengerAtSlot(0));
            Assert.AreSame(passengerA, _runState.GetPassengerAtSlot(8));
        }

        [Test]
        public void RecordEnemyKill_UpdatesHistoryAndCurrency()
        {
            _runState.RecordEnemyKill(5);

            Assert.AreEqual(1, _runState.History.EnemiesKilled);
            Assert.AreEqual(5, _runState.Currency.CurrentCoins);
        }

        [Test]
        public void BuildResult_ContainsExpectedStats()
        {
            _runState.RecordEnemyKill(10);
            _runState.History.RecordMerge(2);
            _runState.Train.ApplyDamage(30);

            RunResult result = _runState.BuildResult(RunEndReason.Defeat, isVictory: false);

            Assert.AreEqual(_runState.RunId, result.RunId);
            Assert.IsFalse(result.IsVictory);
            Assert.AreEqual(RunEndReason.Defeat, result.EndReason);
            Assert.AreEqual(1, result.EnemiesKilled);
            Assert.AreEqual(1, result.MergeCount);
            Assert.AreEqual(2, result.HighestPassengerStar);
            Assert.AreEqual(70, result.RemainingTrainHp);
            Assert.AreEqual(10, result.FinalCoins);
        }

        private static PassengerRuntime CreateTestPassenger()
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<Data.PassengerData>();
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("id").stringValue = "passenger_temp";
            so.FindProperty("displayName").stringValue = "Temp";
            so.ApplyModifiedPropertiesWithoutUndo();
            return PassengerRuntime.Create(data);
        }
    }
}
