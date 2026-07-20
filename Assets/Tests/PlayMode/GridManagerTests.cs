using System.Collections;
using LastTrain.Grid;
using LastTrain.Run;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastTrain.Tests.PlayMode
{
    public class GridManagerTests
    {
        private GameObject _canvasGo;
        private GridManager _gridManager;
        private GridSlot[] _slots;
        private RunState _runState;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());

            _canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var gridGo = new GameObject("GridManager", typeof(RectTransform), typeof(GridManager));
            gridGo.transform.SetParent(_canvasGo.transform, false);
            _gridManager = gridGo.GetComponent<GridManager>();

            _slots = new GridSlot[RunState.GridSlotCount];
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                var slotGo = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(GridSlot));
                slotGo.transform.SetParent(gridGo.transform, false);
                var slotRect = slotGo.GetComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(100, 100);
                slotRect.anchoredPosition = new Vector2((i % 3) * 110, -(i / 3) * 110);

                _slots[i] = slotGo.GetComponent<GridSlot>();
                _slots[i].Configure(i);
            }

            _gridManager.SetReferences(canvas, _slots, null);
            _gridManager.Initialize(_runState);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _runState?.Dispose();
            if (_canvasGo != null)
            {
                Object.Destroy(_canvasGo);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FindSlotIndexAtScreenPoint_ReturnsMatchingSlot()
        {
            RectTransform slotRect = _slots[4].ContentAnchor;
            Vector3 worldCenter = slotRect.TransformPoint(slotRect.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);

            int index = _gridManager.FindSlotIndexAtScreenPoint(screenPoint, null);

            Assert.AreEqual(4, index);
            yield return null;
        }
    }
}
