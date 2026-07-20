namespace LastTrain.Core
{
    /// <summary>시드 기반 난수. 동일 시드에서 동일 결과를 보장한다.</summary>
    public sealed class RandomService
    {
        private System.Random _random;

        public int Seed { get; private set; }

        public RandomService(int? seed = null)
        {
            Reseed(seed ?? EnvironmentTickSeed());
        }

        public void Reseed(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                return 0;
            }

            return _random.Next(maxExclusive);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            return _random.Next(minInclusive, maxExclusive);
        }

        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        private static int EnvironmentTickSeed()
        {
            unchecked
            {
                return System.Environment.TickCount ^ (System.Guid.NewGuid().GetHashCode());
            }
        }
    }
}
