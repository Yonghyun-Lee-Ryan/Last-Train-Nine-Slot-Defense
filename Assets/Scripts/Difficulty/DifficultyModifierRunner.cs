using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Difficulty
{
    /// <summary>회차 중 활성 난이도 Modifier 런타임 상태.</summary>
    public sealed class DifficultyModifierState
    {
        private float _sellPriceMultiplier = 1f;
        private float _preparationTimeSeconds = -1f;
        private float _enemyHealthBonusMultiplier = 1f;

        public float SellPriceMultiplier => _sellPriceMultiplier;
        public float PreparationTimeSeconds => _preparationTimeSeconds;
        public float EnemyHealthBonusMultiplier => _enemyHealthBonusMultiplier;

        public void Reset()
        {
            _sellPriceMultiplier = 1f;
            _preparationTimeSeconds = -1f;
            _enemyHealthBonusMultiplier = 1f;
        }

        public void SetSellPriceMultiplier(float multiplier)
        {
            _sellPriceMultiplier = multiplier > 0f ? multiplier : 1f;
        }

        public void SetPreparationTimeSeconds(float seconds)
        {
            _preparationTimeSeconds = seconds >= 0f ? seconds : -1f;
        }

        public void SetEnemyHealthBonusMultiplier(float multiplier)
        {
            _enemyHealthBonusMultiplier = multiplier > 0f ? multiplier : 1f;
        }

        public float ResolvePreparationTime(DifficultyRuntime difficulty)
        {
            if (_preparationTimeSeconds >= 0f)
            {
                return _preparationTimeSeconds;
            }

            return difficulty?.PreparationTimeSeconds ?? 5f;
        }
    }

    public sealed class DifficultyModifierRunner
    {
        private readonly List<IDifficultyModifier> _active = new();
        private DifficultyModifierContext _context;

        public void BeginRun(RunState runState)
        {
            _active.Clear();
            if (runState?.Difficulty == null)
            {
                return;
            }

            runState.DifficultyModifiers.Reset();
            _context = new DifficultyModifierContext(runState, runState.Difficulty);
            List<IDifficultyModifier> modifiers = DifficultyModifierFactory.CreateActiveModifiers(
                runState.Difficulty,
                runState.Station?.CurrentStationIndex ?? 1);

            for (int i = 0; i < modifiers.Count; i++)
            {
                _active.Add(modifiers[i]);
                modifiers[i].OnRunStarted(_context);
            }
        }

        public void OnStationStarted(StationData station)
        {
            if (_context?.RunState == null || station == null)
            {
                return;
            }

            _context.RunState.DifficultyModifiers.Reset();
            List<IDifficultyModifier> modifiers = DifficultyModifierFactory.CreateActiveModifiers(
                _context.Difficulty,
                station.StationIndex);

            _active.Clear();
            for (int i = 0; i < modifiers.Count; i++)
            {
                _active.Add(modifiers[i]);
                modifiers[i].OnStationStarted(_context, station);
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].Tick(deltaTime, _context);
            }
        }
    }
}
