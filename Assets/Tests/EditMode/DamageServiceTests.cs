using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class DamageServiceTests
    {
        private EnemyData _enemyData;

        [SetUp]
        public void SetUp()
        {
            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
            var so = new UnityEditor.SerializedObject(_enemyData);
            so.FindProperty("id").stringValue = "test";
            so.FindProperty("defense").floatValue = 0.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void CalculateFinalDamage_AppliesDefensePercent()
        {
            float damage = DamageService.CalculateFinalDamage(100f, 0.2f);
            Assert.AreEqual(80f, damage, 0.001f);
        }

        [Test]
        public void ApplyDamage_ReducesEnemyHealth()
        {
            var enemy = new EnemyRuntime(_enemyData, 100f, Vector2.zero);
            float applied = DamageService.ApplyDamage(enemy, 50f);

            Assert.AreEqual(40f, applied, 0.001f);
            Assert.AreEqual(60f, enemy.CurrentHealth, 0.001f);
        }
    }
}
