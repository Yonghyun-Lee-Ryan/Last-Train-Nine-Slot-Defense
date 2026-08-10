using System;
using LastTrain.Data;

namespace LastTrain.Battle
{
    public sealed class CombatStationHandler : IStationHandler
    {
        public static CombatStationHandler Instance { get; } = new();

        public bool UsesWaveManager => true;

        public void OnStationEntered(StationHandlerContext context)
        {
        }

        public bool TryActivate(StationHandlerContext context)
        {
            return false;
        }
    }

    public sealed class EventStationHandler : IStationHandler
    {
        public static EventStationHandler Instance { get; } = new();

        public bool UsesWaveManager => false;

        public void OnStationEntered(StationHandlerContext context)
        {
        }

        public bool TryActivate(StationHandlerContext context)
        {
            if (context?.RunState == null || context.Services?.Events == null)
            {
                context?.CompleteStation?.Invoke();
                return true;
            }

            if (context.RunState.Events.IsResolved)
            {
                context.CompleteStation.Invoke();
                return true;
            }

            if (context.RunState.Events.IsActive)
            {
                return true;
            }

            if (context.Services.Events.TryOpenEvent(context.Station))
            {
                return true;
            }

            // 이벤트 데이터가 없으면 해당 역을 건너뛴다(첫 클리어 경로 소프트락 방지).
            context.CompleteStation.Invoke();
            return true;
        }
    }

    public sealed class ShopStationHandler : IStationHandler
    {
        public static ShopStationHandler Instance { get; } = new();

        public bool UsesWaveManager => false;

        public void OnStationEntered(StationHandlerContext context)
        {
        }

        public bool TryActivate(StationHandlerContext context)
        {
            if (context?.RunState == null || context.Services?.Shop == null)
            {
                context?.CompleteStation?.Invoke();
                return true;
            }

            if (context.RunState.Shop.IsResolved)
            {
                context.CompleteStation.Invoke();
                return true;
            }

            if (context.RunState.Shop.IsActive)
            {
                return true;
            }

            if (context.Services.Shop.TryOpenShop(context.Station))
            {
                return true;
            }

            context.CompleteStation.Invoke();
            return true;
        }
    }

    public sealed class RestStationHandler : IStationHandler
    {
        public static RestStationHandler Instance { get; } = new();

        public bool UsesWaveManager => false;

        public void OnStationEntered(StationHandlerContext context)
        {
        }

        public bool TryActivate(StationHandlerContext context)
        {
            if (context?.RunState == null)
            {
                return false;
            }

            int heal = Math.Max(5, context.RunState.Train.MaxHp / 10);
            context.RunState.Train.Heal(heal);
            context.CompleteStation.Invoke();
            return true;
        }
    }

    public static class StationHandlerFactory
    {
        public static IStationHandler Create(StationType stationType)
        {
            return stationType switch
            {
                StationType.Event => EventStationHandler.Instance,
                StationType.Shop => ShopStationHandler.Instance,
                StationType.Rest => RestStationHandler.Instance,
                _ => CombatStationHandler.Instance,
            };
        }
    }
}
