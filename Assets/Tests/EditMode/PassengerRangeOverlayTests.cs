using LastTrain.Battle;
using LastTrain.UI;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class PassengerRangeOverlayTests
    {
        private GameObject _root;
        private RectTransform _space;
        private PassengerRangeOverlay _overlay;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RangeOverlayTestRoot", typeof(RectTransform));
            _space = _root.GetComponent<RectTransform>();
            _space.sizeDelta = new Vector2(1080f, 1920f);
            _overlay = PassengerRangeOverlay.Ensure(_space);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void RadiusForEffectiveRange_MatchesBattleConstants()
        {
            Assert.AreEqual(
                BattleConstants.ToWorldRange(6f),
                PassengerRangeOverlay.RadiusForEffectiveRange(6f),
                0.001f);
            Assert.AreEqual(
                BattleConstants.ToWorldRange(2.5f),
                PassengerRangeOverlay.RadiusForEffectiveRange(2.5f),
                0.001f);
        }

        [Test]
        public void Show_SetsDiameterToTwiceRadius()
        {
            const float radius = 450f;
            _overlay.Show(new Vector2(10f, -20f), radius);

            Assert.IsTrue(_overlay.IsVisible);
            Assert.AreEqual(radius, _overlay.VisibleRadius, 0.001f);

            Transform circle = _overlay.transform.Find("RangeCircle");
            Assert.IsNotNull(circle);
            var circleRect = circle as RectTransform;
            Assert.AreEqual(radius * 2f, circleRect.sizeDelta.x, 0.001f);
            Assert.AreEqual(radius * 2f, circleRect.sizeDelta.y, 0.001f);
            Assert.AreEqual(10f, circleRect.anchoredPosition.x, 0.001f);
            Assert.AreEqual(-20f, circleRect.anchoredPosition.y, 0.001f);
        }

        [Test]
        public void Hide_ClearsVisibleRadius()
        {
            _overlay.Show(Vector2.zero, 300f);
            _overlay.Hide();

            Assert.IsFalse(_overlay.IsVisible);
            Assert.AreEqual(0f, _overlay.VisibleRadius, 0.001f);
        }
    }
}
