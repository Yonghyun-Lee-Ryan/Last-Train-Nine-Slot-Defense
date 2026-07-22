using System;
using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Passenger;
using LastTrain.Relic;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Shop
{
    public sealed class ShopService
    {
        public const int OfferCount = 4;

        private readonly RunState _runState;
        private readonly GameDatabase _database;
        private readonly RelicManager _relicManager;
        private readonly RandomService _random;

        public ShopService(
            RunState runState,
            GameDatabase database,
            RelicManager relicManager,
            RandomService random)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _database = database;
            _relicManager = relicManager;
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public static int CreateSeed(RunState runState, int stationIndex)
        {
            if (runState == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (runState.RunId?.GetHashCode() ?? 0);
                hash = hash * 31 + stationIndex;
                return hash;
            }
        }

        public bool TryOpenShop(StationData station)
        {
            if (station == null || _runState.Shop.IsActive)
            {
                return false;
            }

            var offers = GenerateOffers(station);
            _runState.Shop.Begin(station.Id, station.StationIndex, offers);
            _runState.Battle.SetPhase(RunPhase.ShopOpen);
            return true;
        }

        public ShopPurchaseResult TryPurchase(int offerIndex)
        {
            if (!_runState.Shop.IsActive || _runState.Shop.IsResolved)
            {
                return ShopPurchaseResult.ShopNotActive;
            }

            if (offerIndex < 0 || offerIndex >= _runState.Shop.Offers.Count)
            {
                return ShopPurchaseResult.InvalidOffer;
            }

            ShopOffer offer = _runState.Shop.Offers[offerIndex];
            if (offer.purchased)
            {
                return ShopPurchaseResult.AlreadyPurchased;
            }

            int price = ResolvePrice(offer.price);
            if (!_runState.Currency.CanAfford(price))
            {
                return ShopPurchaseResult.NotEnoughCoins;
            }

            if (!TryApplyReward(offer))
            {
                return ShopPurchaseResult.RewardFailed;
            }

            if (price > 0 && !_runState.Currency.TrySpend(price))
            {
                return ShopPurchaseResult.NotEnoughCoins;
            }

            _runState.Shop.MarkPurchased(offerIndex);
            return ShopPurchaseResult.Success;
        }

        public void LeaveShop()
        {
            if (!_runState.Shop.IsActive)
            {
                return;
            }

            _runState.Shop.Resolve();
            _runState.Battle.SetPhase(RunPhase.Preparing);
        }

        public List<ShopOffer> GenerateOffers(StationData station)
        {
            _random.Reseed(CreateSeed(_runState, station?.StationIndex ?? 0));
            var offers = new List<ShopOffer>(OfferCount);
            var usedTypes = new HashSet<ShopItemType>();

            ShopItemType[] pool =
            {
                ShopItemType.RandomPassengerStar1,
                ShopItemType.SpecificPassenger,
                ShopItemType.TrainHeal,
                ShopItemType.RandomAbility,
                ShopItemType.Relic,
                ShopItemType.FreeSummonToken,
                ShopItemType.DuplicatePassenger,
                ShopItemType.SummonCostReduction,
            };

            for (int i = 0; i < OfferCount; i++)
            {
                ShopItemType type = PickType(pool, usedTypes);
                usedTypes.Add(type);
                offers.Add(CreateOffer(type, i, station?.StationIndex ?? 0));
            }

            return offers;
        }

        private ShopItemType PickType(ShopItemType[] pool, HashSet<ShopItemType> used)
        {
            for (int attempt = 0; attempt < pool.Length * 2; attempt++)
            {
                ShopItemType candidate = pool[_random.Next(pool.Length)];
                if (used.Add(candidate))
                {
                    return candidate;
                }
            }

            return pool[_random.Next(pool.Length)];
        }

        private ShopOffer CreateOffer(ShopItemType type, int index, int stationIndex)
        {
            var offer = new ShopOffer
            {
                offerId = $"shop_{stationIndex}_{index}",
                itemType = type,
            };

            switch (type)
            {
                case ShopItemType.RandomPassengerStar1:
                    offer.price = 25;
                    offer.payloadId = PickRandomPassengerId();
                    break;

                case ShopItemType.SpecificPassenger:
                    offer.price = 35;
                    offer.payloadId = PickRandomPassengerId();
                    break;

                case ShopItemType.TrainHeal:
                    offer.price = 20;
                    offer.payloadValue = Math.Max(15, _runState.Train.MaxHp / 5);
                    break;

                case ShopItemType.RandomAbility:
                    offer.price = 40;
                    offer.payloadId = PickRandomAbilityId();
                    break;

                case ShopItemType.Relic:
                    offer.price = 55;
                    offer.payloadId = PickRandomRelicId();
                    break;

                case ShopItemType.FreeSummonToken:
                    offer.price = 30;
                    offer.payloadValue = 1;
                    break;

                case ShopItemType.DuplicatePassenger:
                    offer.price = 45;
                    offer.payloadId = PickGridPassengerId();
                    break;

                case ShopItemType.SummonCostReduction:
                    offer.price = 28;
                    offer.payloadValue = 1;
                    break;
            }

            return offer;
        }

        private int ResolvePrice(int basePrice)
        {
            return DifficultyCalculator.ApplyShopPrice(
                basePrice,
                _runState.Difficulty,
                _runState.DifficultyModifiers.SellPriceMultiplier);
        }

        private bool TryApplyReward(ShopOffer offer)
        {
            switch (offer.itemType)
            {
                case ShopItemType.RandomPassengerStar1:
                case ShopItemType.SpecificPassenger:
                    return TryGrantPassenger(offer.payloadId, starLevel: 1);

                case ShopItemType.TrainHeal:
                    _runState.Train.Heal(offer.payloadValue);
                    return true;

                case ShopItemType.RandomAbility:
                    return TryGrantAbility(offer.payloadId);

                case ShopItemType.Relic:
                    return _relicManager != null && _relicManager.TryAcquire(offer.payloadId);

                case ShopItemType.FreeSummonToken:
                    _runState.ShopTokens.AddFreeSummon(offer.payloadValue);
                    return true;

                case ShopItemType.DuplicatePassenger:
                    return TryDuplicatePassenger(offer.payloadId);

                case ShopItemType.SummonCostReduction:
                    _runState.ShopTokens.AddSummonCostReduction(offer.payloadValue);
                    return true;

                default:
                    return false;
            }
        }

        private bool TryGrantPassenger(string passengerId, int starLevel)
        {
            if (string.IsNullOrWhiteSpace(passengerId)
                || _database == null
                || !_database.TryGetPassenger(passengerId, out PassengerData data))
            {
                return false;
            }

            int slot = _runState.FindFirstEmptySlot();
            if (slot < 0)
            {
                return false;
            }

            return _runState.TryPlacePassenger(slot, PassengerRuntime.Create(data, starLevel));
        }

        private bool TryGrantAbility(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId)
                || _database == null
                || !_database.TryGetAbility(abilityId, out AbilityData ability)
                || !_runState.Abilities.CanSelect(ability))
            {
                return false;
            }

            _runState.Abilities.AddSelected(ability);
            AbilityEffectApplier.Refresh(_runState, _runState.BaseTrainMaxHp);
            return true;
        }

        private bool TryDuplicatePassenger(string passengerId)
        {
            if (string.IsNullOrWhiteSpace(passengerId))
            {
                return false;
            }

            PassengerRuntime source = null;
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = _runState.GetPassengerAtSlot(i);
                if (passenger?.Data?.Id == passengerId)
                {
                    source = passenger;
                    break;
                }
            }

            if (source == null)
            {
                return TryGrantPassenger(passengerId, 1);
            }

            return TryGrantPassenger(passengerId, source.StarLevel);
        }

        private string PickRandomPassengerId()
        {
            if (_database?.Passengers == null || _database.Passengers.Count == 0)
            {
                return string.Empty;
            }

            IReadOnlyList<PassengerData> pool = _database.Passengers;
            for (int attempt = 0; attempt < pool.Count; attempt++)
            {
                PassengerData data = pool[_random.Next(pool.Count)];
                if (data != null && !string.IsNullOrWhiteSpace(data.Id))
                {
                    return data.Id;
                }
            }

            return string.Empty;
        }

        private string PickRandomAbilityId()
        {
            if (_database?.Abilities == null || _database.Abilities.Count == 0)
            {
                return string.Empty;
            }

            IReadOnlyList<AbilityData> pool = _database.Abilities;
            for (int attempt = 0; attempt < pool.Count; attempt++)
            {
                AbilityData data = pool[_random.Next(pool.Count)];
                if (data != null
                    && !string.IsNullOrWhiteSpace(data.Id)
                    && _runState.Abilities.CanSelect(data))
                {
                    return data.Id;
                }
            }

            return string.Empty;
        }

        private string PickRandomRelicId()
        {
            if (_database?.Relics == null || _database.Relics.Count == 0)
            {
                return string.Empty;
            }

            IReadOnlyList<RelicData> pool = _database.Relics;
            for (int attempt = 0; attempt < pool.Count * 2; attempt++)
            {
                RelicData data = pool[_random.Next(pool.Count)];
                if (data != null
                    && !string.IsNullOrWhiteSpace(data.Id)
                    && (_relicManager == null || !_relicManager.HasRelic(data.Id)))
                {
                    return data.Id;
                }
            }

            return string.Empty;
        }

        private string PickGridPassengerId()
        {
            var ids = new List<string>();
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = _runState.GetPassengerAtSlot(i);
                if (!string.IsNullOrWhiteSpace(passenger?.Data?.Id))
                {
                    ids.Add(passenger.Data.Id);
                }
            }

            if (ids.Count == 0)
            {
                return PickRandomPassengerId();
            }

            return ids[_random.Next(ids.Count)];
        }
    }
}
