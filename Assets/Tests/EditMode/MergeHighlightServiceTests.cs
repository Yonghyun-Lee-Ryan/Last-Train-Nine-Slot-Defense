using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Ux;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Tests.EditMode
{
    public class MergeHighlightServiceTests
    {
        private GameObject _root;
        private GridManager _grid;
        private GridSlot[] _slots;
        private RunState _runState;
        private PassengerData _passenger;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("MergeHighlightRoot");
            _grid = _root.AddComponent<GridManager>();
            _slots = new GridSlot[RunState.GridSlotCount];
            for (int i = 0; i < _slots.Length; i++)
            {
                var slotGo = new GameObject($"Slot{i}", typeof(RectTransform), typeof(Image), typeof(GridSlot));
                slotGo.transform.SetParent(_root.transform, false);
                _slots[i] = slotGo.GetComponent<GridSlot>();
                _slots[i].Configure(i);
                var slotSo = new SerializedObject(_slots[i]);
                slotSo.FindProperty("highlightImage").objectReferenceValue = slotGo.GetComponent<Image>();
                slotSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var gridSo = new SerializedObject(_grid);
            SerializedProperty slotsProp = gridSo.FindProperty("slots");
            slotsProp.arraySize = _slots.Length;
            for (int i = 0; i < _slots.Length; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = _slots[i];
            }

            gridSo.ApplyModifiedPropertiesWithoutUndo();

            _passenger = CreatePassenger("merge_pair");
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.TryPlacePassenger(0, PassengerRuntime.Create(_passenger, 1));
            _runState.TryPlacePassenger(1, PassengerRuntime.Create(_passenger, 1));
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            if (_passenger != null)
            {
                Object.DestroyImmediate(_passenger);
            }

            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void Refresh_AfterDragHighlightClear_KeepsMergeableSlotsGreen()
        {
            MergeHighlightService.Refresh(_grid, _runState);
            Assert.Greater(_slots[0].GetComponent<Image>().color.g, 0.5f);

            _slots[0].SetHighlightActive(false);
            _slots[1].SetHighlightActive(false);
            Assert.Less(_slots[0].GetComponent<Image>().color.a, 0.05f);

            MergeHighlightService.Refresh(_grid, _runState);
            Assert.Greater(_slots[0].GetComponent<Image>().color.g, 0.5f);
            Assert.Greater(_slots[1].GetComponent<Image>().color.g, 0.5f);
            Assert.Greater(_slots[0].GetComponent<Image>().color.a, 0.2f);
        }

        [Test]
        public void SameSlotDrop_DoesNotClearMergeHighlight()
        {
            MergeHighlightService.Refresh(_grid, _runState);
            _slots[0].SetHighlightActive(false);
            _slots[1].SetHighlightActive(false);

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetLocked(false);
            }

            MergeHighlightService.Refresh(_grid, _runState);
            Assert.Greater(_slots[0].GetComponent<Image>().color.g, 0.5f);
            Assert.Greater(_slots[1].GetComponent<Image>().color.g, 0.5f);
            Assert.Greater(_slots[0].GetComponent<Image>().color.a, 0.2f);
        }

        [Test]
        public void SetLockedFalse_WhenAlreadyUnlocked_KeepsMergeHighlight()
        {
            MergeHighlightService.Refresh(_grid, _runState);
            Assert.Greater(_slots[0].GetComponent<Image>().color.g, 0.5f);

            _slots[0].SetLocked(false);
            _slots[1].SetLocked(false);

            Assert.Greater(_slots[0].GetComponent<Image>().color.g, 0.5f);
            Assert.Greater(_slots[1].GetComponent<Image>().color.g, 0.5f);
        }

        private static PassengerData CreatePassenger(string id)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            SerializedProperty starLevels = so.FindProperty("starLevels");
            starLevels.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                SerializedProperty element = starLevels.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("starLevel").intValue = i + 1;
                element.FindPropertyRelative("attackMultiplier").floatValue = 1f;
                element.FindPropertyRelative("attackSpeedMultiplier").floatValue = 1f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
