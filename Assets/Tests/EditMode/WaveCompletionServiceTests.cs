using LastTrain.Wave;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class WaveCompletionServiceTests
    {
        [Test]
        public void IsWaveComplete_RequiresAllSpawnedAndCleared()
        {
            Assert.IsFalse(WaveCompletionService.IsWaveComplete(5, 4, 1, 0));
            Assert.IsFalse(WaveCompletionService.IsWaveComplete(5, 5, 0, 2));
            Assert.IsTrue(WaveCompletionService.IsWaveComplete(5, 5, 0, 0));
        }

        [Test]
        public void IsWaveComplete_EmptyWave_CompletesWhenNoAliveEnemies()
        {
            Assert.IsTrue(WaveCompletionService.IsWaveComplete(0, 0, 0, 0));
            Assert.IsFalse(WaveCompletionService.IsWaveComplete(0, 0, 0, 1));
        }
    }
}
