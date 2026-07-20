using LastTrain.UI;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class SafeAreaCalculatorTests
    {
        [Test]
        public void FullScreenSafeArea_ProducesFullAnchors()
        {
            var safeArea = new Rect(0, 0, 1080, 1920);

            bool ok = SafeAreaCalculator.TryCalculateAnchors(
                safeArea, 1080, 1920, true, true, out Vector2 min, out Vector2 max);

            Assert.IsTrue(ok);
            Assert.AreEqual(0f, min.x, 0.0001f);
            Assert.AreEqual(0f, min.y, 0.0001f);
            Assert.AreEqual(1f, max.x, 0.0001f);
            Assert.AreEqual(1f, max.y, 0.0001f);
        }

        [Test]
        public void NotchAtTop_ReducesTopAnchor()
        {
            // 상단 120px가 노치로 잘린 세로 화면(1080x1920) 가정.
            var safeArea = new Rect(0, 0, 1080, 1800);

            bool ok = SafeAreaCalculator.TryCalculateAnchors(
                safeArea, 1080, 1920, true, true, out Vector2 min, out Vector2 max);

            Assert.IsTrue(ok);
            Assert.AreEqual(0f, min.y, 0.0001f);
            Assert.AreEqual(1800f / 1920f, max.y, 0.0001f);
            Assert.Less(max.y, 1f);
        }

        [Test]
        public void ApplyVerticalDisabled_KeepsVerticalFull()
        {
            var safeArea = new Rect(0, 63, 1080, 1794);

            bool ok = SafeAreaCalculator.TryCalculateAnchors(
                safeArea, 1080, 1920, true, false, out Vector2 min, out Vector2 max);

            Assert.IsTrue(ok);
            Assert.AreEqual(0f, min.y, 0.0001f);
            Assert.AreEqual(1f, max.y, 0.0001f);
        }

        [Test]
        public void ApplyHorizontalDisabled_KeepsHorizontalFull()
        {
            // 가로 방향에서 좌우가 잘린 경우.
            var safeArea = new Rect(80, 0, 1760, 1080);

            bool ok = SafeAreaCalculator.TryCalculateAnchors(
                safeArea, 1920, 1080, false, true, out Vector2 min, out Vector2 max);

            Assert.IsTrue(ok);
            Assert.AreEqual(0f, min.x, 0.0001f);
            Assert.AreEqual(1f, max.x, 0.0001f);
        }

        [Test]
        public void ZeroScreenSize_ReturnsFalseAndFullAnchors()
        {
            var safeArea = new Rect(0, 0, 0, 0);

            bool ok = SafeAreaCalculator.TryCalculateAnchors(
                safeArea, 0, 0, true, true, out Vector2 min, out Vector2 max);

            Assert.IsFalse(ok);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }
    }
}
