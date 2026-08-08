using LastTrain.Battle;
using LastTrain.UI;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class EnemyPathDirectionViewTests
    {
        private GameObject _root;
        private RectTransform _space;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PathDirectionTestRoot", typeof(RectTransform));
            _space = _root.GetComponent<RectTransform>();
            _space.sizeDelta = new Vector2(1080f, 1920f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Ensure_BuildsArrowsAlongZigzagPath()
        {
            EnemyPathDirectionView view = EnemyPathDirectionView.Ensure(_space);

            Assert.IsNotNull(view);
            Assert.Greater(view.ArrowCount, 4);
            Assert.AreEqual(BattleConstants.GetEnemyPathPoints().Length, 8);
        }

        [Test]
        public void SetVisible_TogglesActiveState()
        {
            EnemyPathDirectionView view = EnemyPathDirectionView.Ensure(_space);
            view.SetVisible(true);
            Assert.IsTrue(view.IsShowing);

            view.SetVisible(false);
            Assert.IsFalse(view.IsShowing);

            view.SetVisible(true);
            Assert.IsTrue(view.IsShowing);
            Assert.Greater(view.ArrowCount, 0);
        }

        [Test]
        public void GetEnemyPathPoints_StartsRightAndZigzags()
        {
            Vector2[] points = BattleConstants.GetEnemyPathPoints();

            Assert.AreEqual(BattleConstants.EnemyPathRightX, points[0].x, 0.001f);
            Assert.AreEqual(BattleConstants.EnemyPathLeftX, points[1].x, 0.001f);
            Assert.AreEqual(BattleConstants.EnemyPathRightX, points[3].x, 0.001f);
            Assert.AreEqual(BattleConstants.TrainTargetAnchoredPosition, points[^1]);
        }
    }
}
