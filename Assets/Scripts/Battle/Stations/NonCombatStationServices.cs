using LastTrain.Data;
using LastTrain.Event;
using LastTrain.Relic;
using LastTrain.Run;
using LastTrain.Shop;

namespace LastTrain.Battle
{
    public sealed class NonCombatStationServices
    {
        public NonCombatStationServices(
            ShopService shopService,
            EventService eventService,
            RelicManager relicManager)
        {
            Shop = shopService;
            Events = eventService;
            Relics = relicManager;
        }

        public ShopService Shop { get; }
        public EventService Events { get; }
        public RelicManager Relics { get; }
    }
}
