using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    /// <summary>
    /// 소환 직후 전투 컨트롤러 등록이 필요한 회귀를 문서화한다.
    /// (릴리즈에서 드래그 전까지 미공격 버그)
    /// </summary>
    public class BattlePassengerRegistrationTests
    {
        private PassengerData _passengerData;
        private RunState _runState;

        [SetUp]
        public void SetUp()
        {
            _passengerData = CreatePassenger();
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_passengerData);
        }

        [Test]
        public void PlacedPassenger_NeedsControllerRegistry_ToAttack()
        {
            PassengerRuntime placed = PassengerRuntime.Create(_passengerData, starLevel: 1);
            Assert.IsTrue(_runState.TryPlacePassenger(0, placed));

            var registry = new Dictionary<string, PassengerController>();
            Assert.AreEqual(0, registry.Count);

            Sync(registry, _runState);
            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.ContainsKey(placed.InstanceId));
            Assert.AreSame(placed, registry[placed.InstanceId].Runtime);
        }

        private static PassengerData CreatePassenger()
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "test_passenger";
            so.FindProperty("displayName").stringValue = "Test";
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 5f;
            SerializedProperty starLevels = so.FindProperty("starLevels");
            starLevels.arraySize = 1;
            SerializedProperty star = starLevels.GetArrayElementAtIndex(0);
            star.FindPropertyRelative("starLevel").intValue = 1;
            star.FindPropertyRelative("attackMultiplier").floatValue = 1f;
            star.FindPropertyRelative("attackSpeedMultiplier").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void Sync(Dictionary<string, PassengerController> registry, RunState runState)
        {
            var activeIds = new HashSet<string>();
            for (int slotIndex = 0; slotIndex < RunState.GridSlotCount; slotIndex++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(slotIndex);
                if (passenger == null)
                {
                    continue;
                }

                activeIds.Add(passenger.InstanceId);
                if (!registry.TryGetValue(passenger.InstanceId, out PassengerController existing)
                    || existing == null
                    || !ReferenceEquals(existing.Runtime, passenger))
                {
                    registry[passenger.InstanceId] = PassengerFactory.CreateController(passenger);
                }
            }

            var removeKeys = new List<string>();
            foreach (KeyValuePair<string, PassengerController> pair in registry)
            {
                if (!activeIds.Contains(pair.Key))
                {
                    removeKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeKeys.Count; i++)
            {
                registry.Remove(removeKeys[i]);
            }
        }
    }
}
