using LastTrain.Data;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class PassengerStarDataTests
    {
        [Test]
        public void CreateDefault_Star2_MatchesReadmeBalance()
        {
            PassengerStarData star2 = PassengerStarData.CreateDefault(2);

            Assert.AreEqual(2, star2.starLevel);
            Assert.AreEqual(2.2f, star2.attackMultiplier, 0.0001f);
            Assert.AreEqual(1.05f, star2.attackSpeedMultiplier, 0.0001f);
        }

        [Test]
        public void CreateDefault_Star3_MatchesReadmeBalance()
        {
            PassengerStarData star3 = PassengerStarData.CreateDefault(3);

            Assert.AreEqual(3, star3.starLevel);
            Assert.AreEqual(4.8f, star3.attackMultiplier, 0.0001f);
            Assert.AreEqual(1.1f, star3.attackSpeedMultiplier, 0.0001f);
        }
    }
}
