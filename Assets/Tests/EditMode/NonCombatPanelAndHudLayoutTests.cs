using LastTrain.Run;
using LastTrain.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Tests.EditMode
{
    public class NonCombatPanelAndHudLayoutTests
    {
        private GameObject _canvasGo;
        private GameObject _hostGo;

        [TearDown]
        public void TearDown()
        {
            if (_hostGo != null)
            {
                Object.DestroyImmediate(_hostGo);
            }

            if (_canvasGo != null)
            {
                Object.DestroyImmediate(_canvasGo);
            }
        }

        [Test]
        public void ShopPanel_BuildsHidden_WithoutLoadingPlaceholders()
        {
            _canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _hostGo = new GameObject("NonCombatPanelHost", typeof(RectTransform), typeof(NonCombatPanelController));
            _hostGo.transform.SetParent(_canvasGo.transform, false);

            var panel = _hostGo.GetComponent<NonCombatPanelController>();
            panel.EnsureBuiltHiddenForTests();

            Assert.IsFalse(panel.IsPanelActive);
            Transform choice = _canvasGo.transform.Find("NonCombatPanel/Box/Choice0");
            Assert.IsNotNull(choice);
            Assert.IsFalse(choice.gameObject.activeSelf);
            Text label = choice.GetComponentInChildren<Text>();
            Assert.IsNotNull(label);
            Assert.IsFalse(string.Equals(label.text, "불러오는 중…"));
            Assert.IsFalse(string.Equals(label.text, "불러오는 중..."));
        }

        [Test]
        public void UndoButton_DoesNotOverlapSummonButton()
        {
            Vector2 ready = new Vector2(-190f, 220f);
            Vector2 undo = BattleHudLayout.UndoMergeAnchoredPosition(ready);
            Assert.IsFalse(
                BattleHudLayout.OverlapsVertically(
                    undo.y,
                    BattleHudLayout.ActionButtonHeight,
                    BattleHudLayout.SummonButtonY,
                    BattleHudLayout.SummonButtonHeight));
            Assert.Less(undo.x, ready.x);
            Assert.AreEqual(ready.y, undo.y, 0.01f);
        }

        [Test]
        public void FreeSummonAdButton_DoesNotOverlapSpeedButton()
        {
            Assert.IsFalse(
                BattleHudLayout.OverlapsVertically(
                    BattleHudLayout.FreeSummonAdY,
                    BattleHudLayout.FreeSummonAdHeight,
                    BattleHudLayout.SpeedButtonY,
                    BattleHudLayout.SpeedButtonHeight));
            Assert.Greater(
                Mathf.Abs(BattleHudLayout.FreeSummonAdX),
                (BattleHudLayout.SummonButtonWidth * 0.5f) + 20f);
        }

        [Test]
        public void SummonButton_StaysBelowActionRow()
        {
            Assert.IsFalse(
                BattleHudLayout.OverlapsVertically(
                    BattleHudLayout.SummonButtonY,
                    BattleHudLayout.SummonButtonHeight,
                    BattleHudLayout.SpeedButtonY,
                    BattleHudLayout.SpeedButtonHeight));
        }

        [Test]
        public void PreparingPhase_ShouldNotKeepShopOverlay()
        {
            Assert.AreNotEqual(RunPhase.ShopOpen, RunPhase.Preparing);
            Assert.AreNotEqual(RunPhase.EventOpen, RunPhase.Preparing);
        }
    }
}
