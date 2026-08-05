using LastTrain.Battle;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class BattleCombatSpaceTests
    {
        private GameObject _root;
        private RectTransform _space;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("CombatSpaceRoot", typeof(RectTransform), typeof(Canvas));
            _space = _root.GetComponent<RectTransform>();
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _space.anchorMin = new Vector2(0.5f, 0.5f);
            _space.anchorMax = new Vector2(0.5f, 0.5f);
            _space.pivot = new Vector2(0.5f, 0.5f);
            _space.sizeDelta = new Vector2(1080f, 1920f);
            _space.position = Vector3.zero;
            _space.localScale = Vector3.one;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void WorldToLocal_RoundTrip_PreservesPoint()
        {
            var world = new Vector3(120f, 340f, 0f);
            Vector2 local = BattleCombatSpace.WorldToLocal(_space, world);
            Vector3 back = BattleCombatSpace.LocalToWorld(_space, local);

            Assert.AreEqual(world.x, back.x, 0.01f);
            Assert.AreEqual(world.y, back.y, 0.01f);
        }

        [Test]
        public void DistanceLocal_IgnoresParentScale()
        {
            var childA = new GameObject("A", typeof(RectTransform)).GetComponent<RectTransform>();
            var childB = new GameObject("B", typeof(RectTransform)).GetComponent<RectTransform>();
            childA.SetParent(_space, false);
            childB.SetParent(_space, false);
            childA.anchoredPosition = new Vector2(0f, 0f);
            childB.anchoredPosition = new Vector2(220f, 0f);

            _space.localScale = Vector3.one;
            float unscaled = BattleCombatSpace.DistanceLocal(_space, childA.position, childB.position);

            _space.localScale = new Vector3(2.4f, 2.4f, 1f);
            float scaled = BattleCombatSpace.DistanceLocal(_space, childA.position, childB.position);

            Assert.AreEqual(220f, unscaled, 0.1f);
            Assert.AreEqual(unscaled, scaled, 0.1f);
            Object.DestroyImmediate(childA.gameObject);
            Object.DestroyImmediate(childB.gameObject);
        }

        [Test]
        public void ToWorldRange_DoesNotDependOnUiScale()
        {
            Assert.AreEqual(5f * BattleConstants.RangeToWorldScale, BattleConstants.ToWorldRange(5f), 0.001f);
        }

        [Test]
        public void LongestRange_FromSecondCell_ReachesEarlyPath()
        {
            // SafeArea 중심 기준 레이아웃(Game 씬과 동일 앵커 값).
            var secondCell = new Vector2(0f, -68f);
            var spawn = BattleConstants.SpawnAnchoredPosition;
            var firstWaypoint = BattleConstants.EnemyWaypointAnchoredPositions[0];
            float longest = BattleConstants.ToWorldRange(6f);

            Assert.Greater(longest, Vector2.Distance(secondCell, spawn) - 40f);
            Assert.GreaterOrEqual(longest, Vector2.Distance(secondCell, firstWaypoint) - 30f);
        }

        [Test]
        public void MeleeRange_FromFrontRight_ReachesLowerRightLane()
        {
            var frontRight = new Vector2(232f, -68f);
            var lowerRightLane = new Vector2(BattleConstants.EnemyPathRightX, BattleConstants.EnemyPathLaneYs[^1]);
            float melee = BattleConstants.ToWorldRange(2.5f);

            Assert.GreaterOrEqual(melee, Vector2.Distance(frontRight, lowerRightLane));
        }
    }
}
