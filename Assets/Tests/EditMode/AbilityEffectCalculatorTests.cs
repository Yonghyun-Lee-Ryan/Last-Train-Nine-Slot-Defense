using LastTrain.Ability;
using LastTrain.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class AbilityEffectCalculatorTests
    {
        [Test]
        public void Compute_SumsAttackTrainCoinAndSummonModifiers()
        {
            AbilityData attack = CreateAbility(
                "a_atk",
                AbilityEffectType.PassengerAttackPercent,
                20f,
                "passenger_office_worker");
            AbilityData speed = CreateAbility(
                "a_spd",
                AbilityEffectType.PassengerAttackSpeedPercent,
                10f,
                null);
            AbilityData train = CreateAbility("a_hp", AbilityEffectType.TrainMaxHpFlat, 20f, null);
            AbilityData coin = CreateAbility("a_coin", AbilityEffectType.CoinOnKillPercent, 15f, null);
            AbilityData summon = CreateAbility(
                "a_summon",
                AbilityEffectType.SummonCostIncreaseReduction,
                1f,
                null);

            var modifiers = AbilityEffectCalculator.Compute(
                new[] { attack, speed, train, coin, summon });

            Assert.AreEqual(20f, modifiers.GetPassengerAttackPercent("passenger_office_worker"));
            Assert.AreEqual(10f, modifiers.GlobalAttackSpeedPercent);
            Assert.AreEqual(20, modifiers.TrainMaxHpFlat);
            Assert.AreEqual(15f, modifiers.CoinOnKillPercent);
            Assert.AreEqual(1, modifiers.SummonCostIncreaseReduction);

            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(speed);
            Object.DestroyImmediate(train);
            Object.DestroyImmediate(coin);
            Object.DestroyImmediate(summon);
        }

        [Test]
        public void ApplyPercentBonus_RoundsCoinReward()
        {
            Assert.AreEqual(3, AbilityEffectCalculator.ApplyPercentBonus(3, 0f));
            Assert.AreEqual(3, AbilityEffectCalculator.ApplyPercentBonus(3, 15f));
            Assert.AreEqual(5, AbilityEffectCalculator.ApplyPercentBonus(4, 15f));
        }

        [Test]
        public void CalculateSummonCost_AppliesIncreaseReduction()
        {
            Assert.AreEqual(10, AbilityEffectCalculator.CalculateSummonCost(10, 2, 0, 1));
            Assert.AreEqual(11, AbilityEffectCalculator.CalculateSummonCost(10, 2, 1, 1));
            Assert.AreEqual(10, AbilityEffectCalculator.CalculateSummonCost(10, 2, 3, 5));
        }

        private static AbilityData CreateAbility(
            string id,
            AbilityEffectType type,
            float value,
            string targetPassengerId)
        {
            var data = ScriptableObject.CreateInstance<AbilityData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("effectType").enumValueIndex = (int)type;
            so.FindProperty("effectValue").floatValue = value;
            so.FindProperty("targetPassengerId").stringValue = targetPassengerId ?? string.Empty;
            so.FindProperty("allowDuplicate").boolValue = true;
            so.FindProperty("maxStack").intValue = 99;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
