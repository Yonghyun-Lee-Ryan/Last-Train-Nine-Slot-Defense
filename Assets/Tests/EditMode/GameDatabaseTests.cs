using LastTrain.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class GameDatabaseTests
    {
        private GameDatabase _database;
        private PassengerData _passengerA;
        private PassengerData _passengerB;
        private EnemyData _enemy;

        [SetUp]
        public void SetUp()
        {
            _passengerA = CreatePassenger("passenger_a", "A");
            _passengerB = CreatePassenger("passenger_b", "B");
            _enemy = CreateEnemy("enemy_normal", "취객 괴물");

            _database = ScriptableObject.CreateInstance<GameDatabase>();
            var so = new SerializedObject(_database);
            SetArray(so, "passengers", new Object[] { _passengerA, _passengerB });
            SetArray(so, "enemies", new Object[] { _enemy });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_database);
            Object.DestroyImmediate(_passengerA);
            Object.DestroyImmediate(_passengerB);
            Object.DestroyImmediate(_enemy);
        }

        [Test]
        public void TryGetPassenger_FindsById()
        {
            Assert.IsTrue(_database.TryGetPassenger("passenger_a", out PassengerData found));
            Assert.AreEqual(_passengerA, found);
        }

        [Test]
        public void TryGetPassenger_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_database.TryGetPassenger("missing", out _));
        }

        [Test]
        public void TryGetEnemy_FindsById()
        {
            Assert.IsTrue(_database.TryGetEnemy("enemy_normal", out EnemyData found));
            Assert.AreEqual(_enemy, found);
        }

        private static PassengerData CreatePassenger(string id, string displayName)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static EnemyData CreateEnemy(string id, string displayName)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void SetArray(SerializedObject so, string propertyName, Object[] items)
        {
            SerializedProperty array = so.FindProperty(propertyName);
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }
    }
}
