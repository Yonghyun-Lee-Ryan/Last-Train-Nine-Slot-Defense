using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class AbilityOfferServiceTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void GenerateOffers_SameSeed_IsDeterministic()
        {
            var pool = CreatePool();
            var progress = new AbilityProgress();

            var a = new AbilityOfferService(pool, new RandomService(42), 3).GenerateOffers(progress);
            var b = new AbilityOfferService(pool, new RandomService(42), 3).GenerateOffers(progress);

            Assert.AreEqual(3, a.Count);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Id, b[i].Id);
            }
        }

        [Test]
        public void GenerateOffers_ExcludesNonDuplicateWhenAlreadySelected()
        {
            AbilityData unique = CreateAbility("unique", Rarity.Legendary, allowDuplicate: false);
            AbilityData common = CreateAbility("common", Rarity.Common, allowDuplicate: true);
            var pool = new List<AbilityData> { unique, common };
            var progress = new AbilityProgress();
            progress.AddSelected(unique);

            var offers = new AbilityOfferService(pool, new RandomService(7), 3).GenerateOffers(progress);
            Assert.AreEqual(3, offers.Count);
            for (int i = 0; i < offers.Count; i++)
            {
                Assert.AreNotEqual("unique", offers[i].Id);
            }
        }

        private List<AbilityData> CreatePool()
        {
            return new List<AbilityData>
            {
                CreateAbility("c1", Rarity.Common),
                CreateAbility("c2", Rarity.Common),
                CreateAbility("r1", Rarity.Rare),
                CreateAbility("l1", Rarity.Legendary)
            };
        }

        private AbilityData CreateAbility(string id, Rarity rarity, bool allowDuplicate = true)
        {
            var data = ScriptableObject.CreateInstance<AbilityData>();
            _created.Add(data);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("effectType").enumValueIndex = (int)AbilityEffectType.TrainMaxHpFlat;
            so.FindProperty("effectValue").floatValue = 1f;
            so.FindProperty("allowDuplicate").boolValue = allowDuplicate;
            so.FindProperty("maxStack").intValue = allowDuplicate ? 99 : 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
