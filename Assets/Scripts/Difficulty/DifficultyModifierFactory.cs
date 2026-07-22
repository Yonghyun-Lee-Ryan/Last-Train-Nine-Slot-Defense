using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Difficulty
{
    public static class DifficultyModifierFactory
    {
        public static IDifficultyModifier Create(DifficultyModifierData data)
        {
            if (data == null)
            {
                return null;
            }

            return data.ModifierKind switch
            {
                DifficultyModifierKind.ReducedSellPrice => new ReducedSellPriceModifier(data),
                DifficultyModifierKind.ReducedPreparationTime => new ReducedPreparationTimeModifier(data),
                _ => new NoOpDifficultyModifier(data.Id),
            };
        }

        public static List<IDifficultyModifier> CreateActiveModifiers(
            DifficultyRuntime runtime,
            int stationIndex)
        {
            var result = new List<IDifficultyModifier>();
            if (runtime?.Modifiers == null)
            {
                return result;
            }

            for (int i = 0; i < runtime.Modifiers.Count; i++)
            {
                DifficultyModifierData data = runtime.Modifiers[i];
                if (data == null || stationIndex < data.StationIndexMin)
                {
                    continue;
                }

                IDifficultyModifier modifier = Create(data);
                if (modifier != null)
                {
                    result.Add(modifier);
                }
            }

            return result;
        }

        private sealed class NoOpDifficultyModifier : IDifficultyModifier
        {
            public NoOpDifficultyModifier(string id)
            {
                ModifierId = id ?? string.Empty;
            }

            public string ModifierId { get; }

            public void OnRunStarted(DifficultyModifierContext context)
            {
            }

            public void OnStationStarted(DifficultyModifierContext context, StationData station)
            {
            }

            public void Tick(float deltaTime, DifficultyModifierContext context)
            {
            }
        }
    }
}
