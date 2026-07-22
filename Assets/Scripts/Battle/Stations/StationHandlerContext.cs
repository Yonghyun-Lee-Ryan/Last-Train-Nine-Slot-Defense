using System;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Battle
{
    public sealed class StationHandlerContext
    {
        public StationHandlerContext(
            RunState runState,
            StationData station,
            Action completeStation,
            NonCombatStationServices services = null)
        {
            RunState = runState ?? throw new ArgumentNullException(nameof(runState));
            Station = station ?? throw new ArgumentNullException(nameof(station));
            CompleteStation = completeStation ?? throw new ArgumentNullException(nameof(completeStation));
            Services = services;
        }

        public RunState RunState { get; }
        public StationData Station { get; }
        public Action CompleteStation { get; }
        public NonCombatStationServices Services { get; }
    }

    public interface IStationHandler
    {
        bool UsesWaveManager { get; }
        void OnStationEntered(StationHandlerContext context);
        bool TryActivate(StationHandlerContext context);
    }
}
