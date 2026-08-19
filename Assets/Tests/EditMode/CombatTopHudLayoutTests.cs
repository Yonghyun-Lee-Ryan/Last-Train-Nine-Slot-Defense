using LastTrain.Run;
using LastTrain.UI;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class CombatTopHudLayoutTests
    {
        [Test]
        public void ShouldShowSideChrome_HidesDuringRewardAndShop()
        {
            Assert.IsTrue(CombatTopHudLayout.ShouldShowSideChrome(RunPhase.Preparing));
            Assert.IsTrue(CombatTopHudLayout.ShouldShowSideChrome(RunPhase.Fighting));
            Assert.IsFalse(CombatTopHudLayout.ShouldShowSideChrome(RunPhase.RewardSelecting));
            Assert.IsFalse(CombatTopHudLayout.ShouldShowSideChrome(RunPhase.ShopOpen));
            Assert.IsFalse(CombatTopHudLayout.ShouldShowSideChrome(RunPhase.EventOpen));
            Assert.IsFalse(CombatTopHudLayout.ShouldShowSideChrome(RunPhase.RunEnded));
        }

        [Test]
        public void GetSynergyTop_ShiftsDownWhenThreatTickerVisible()
        {
            float baseline = CombatTopHudLayout.GetSynergyTop(false);
            float withThreat = CombatTopHudLayout.GetSynergyTop(true);
            Assert.Less(withThreat, baseline);
        }

        [Test]
        public void ShouldShowThreatTicker_RequiresEntriesAndVisiblePhase()
        {
            Assert.IsTrue(CombatTopHudLayout.ShouldShowThreatTicker(RunPhase.Preparing, true));
            Assert.IsFalse(CombatTopHudLayout.ShouldShowThreatTicker(RunPhase.RewardSelecting, true));
            Assert.IsFalse(CombatTopHudLayout.ShouldShowThreatTicker(RunPhase.Preparing, false));
        }

        [Test]
        public void SynergyColumn_DoesNotOverlapThreatTickerBand()
        {
            float synergyBottom = CombatTopHudLayout.GetSynergyTop(true) - CombatTopHudLayout.SynergyMaxHeight;
            float threatBottom = CombatTopHudLayout.ThreatTickerY - CombatTopHudLayout.ThreatTickerHeight;
            Assert.LessOrEqual(synergyBottom, threatBottom);
        }
    }
}
