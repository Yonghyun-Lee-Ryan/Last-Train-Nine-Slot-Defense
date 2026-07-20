using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class GridInteractionServiceTests
    {
        private RunState _runState;
        private PassengerData _passengerA;
        private PassengerData _passengerB;

        [SetUp]
        public void SetUp()
        {
            _passengerA = CreatePassenger("passenger_a");
            _passengerB = CreatePassenger("passenger_b");

            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.TryPlacePassenger(0, PassengerRuntime.Create(_passengerA));
            _runState.TryPlacePassenger(4, PassengerRuntime.Create(_passengerB));
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_passengerA);
            Object.DestroyImmediate(_passengerB);
        }

        [Test]
        public void TryDrop_ToEmptySlot_MovesPassenger()
        {
            GridDropResult result = GridInteractionService.TryDrop(_runState, 0, 1);

            Assert.AreEqual(GridDropResult.Moved, result);
            Assert.IsNull(_runState.GetPassengerAtSlot(0));
            Assert.AreEqual("passenger_a", _runState.GetPassengerAtSlot(1).Data.Id);
            Assert.AreEqual(1, _runState.GetPassengerAtSlot(1).GridSlotIndex);
        }

        [Test]
        public void TryDrop_ToOccupiedSlot_SwapsPassengers()
        {
            GridDropResult result = GridInteractionService.TryDrop(_runState, 0, 4);

            Assert.AreEqual(GridDropResult.Swapped, result);
            Assert.AreEqual("passenger_b", _runState.GetPassengerAtSlot(0).Data.Id);
            Assert.AreEqual("passenger_a", _runState.GetPassengerAtSlot(4).Data.Id);
        }

        [Test]
        public void TryDrop_InvalidTarget_Reverts()
        {
            GridDropResult result = GridInteractionService.TryDrop(_runState, 0, -1);

            Assert.AreEqual(GridDropResult.Reverted, result);
            Assert.AreEqual("passenger_a", _runState.GetPassengerAtSlot(0).Data.Id);
        }

        [Test]
        public void TryDrop_SameSlot_Reverts()
        {
            GridDropResult result = GridInteractionService.TryDrop(_runState, 0, 0);

            Assert.AreEqual(GridDropResult.Reverted, result);
            Assert.AreEqual("passenger_a", _runState.GetPassengerAtSlot(0).Data.Id);
        }

        [Test]
        public void TryDrop_EmptyOrigin_Reverts()
        {
            GridDropResult result = GridInteractionService.TryDrop(_runState, 2, 3);

            Assert.AreEqual(GridDropResult.Reverted, result);
        }

        private static PassengerData CreatePassenger(string id)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
