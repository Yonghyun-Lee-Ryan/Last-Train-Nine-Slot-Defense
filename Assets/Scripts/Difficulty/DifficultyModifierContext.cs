using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Difficulty
{
    public sealed class DifficultyModifierContext
    {
        public DifficultyModifierContext(RunState runState, DifficultyRuntime difficulty)
        {
            RunState = runState;
            Difficulty = difficulty;
        }

        public RunState RunState { get; }
        public DifficultyRuntime Difficulty { get; }
        public int CurrentStationIndex => RunState?.Station?.CurrentStationIndex ?? 1;
    }
}
