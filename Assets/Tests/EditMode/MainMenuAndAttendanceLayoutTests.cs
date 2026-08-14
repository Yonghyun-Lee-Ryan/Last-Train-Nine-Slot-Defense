using System.IO;
using System.Linq;
using LastTrain.Save;
using LastTrain.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Tests.EditMode
{
    public class MainMenuAndAttendanceLayoutTests
    {
        private string _tempDir;
        private GameObject _canvasGo;
        private GameObject _hostGo;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "LastTrainMenuLayout_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            RunSaveSystem.SetServiceForTests(
                new JsonSaveService(
                    Path.Combine(_tempDir, "RunSaveData.json"),
                    Path.Combine(_tempDir, "MetaSaveData.json")));
        }

        [TearDown]
        public void TearDown()
        {
            if (_hostGo != null)
            {
                Object.DestroyImmediate(_hostGo);
                _hostGo = null;
            }

            GameObject leftover = GameObject.Find("AttendancePanel");
            if (leftover != null)
            {
                Object.DestroyImmediate(leftover);
            }

            GameObject settings = GameObject.Find("SettingsPanel");
            if (settings != null)
            {
                Object.DestroyImmediate(settings);
            }

            GameObject privacy = GameObject.Find("PrivacyConsentDialog");
            if (privacy != null)
            {
                Object.DestroyImmediate(privacy);
            }

            GameObject unlock = GameObject.Find("DifficultyUnlockPopup");
            if (unlock != null)
            {
                Object.DestroyImmediate(unlock);
            }

            if (_canvasGo != null)
            {
                Object.DestroyImmediate(_canvasGo);
                _canvasGo = null;
            }

            RunSaveSystem.SetServiceForTests(null);
            if (!string.IsNullOrEmpty(_tempDir) && Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Test]
        public void AttendancePanel_UsesVerticalDayRows_AndDoesNotDuplicateOnRefresh()
        {
            _hostGo = new GameObject("AttendanceHost", typeof(AttendancePanelController));
            AttendancePanelController panel = _hostGo.GetComponent<AttendancePanelController>();
            panel.Show();
            panel.Show();

            Transform box = GameObject.Find("AttendancePanel")?.transform.Find("SafeArea/Box");
            Assert.IsNotNull(box);
            Assert.IsNotNull(box.GetComponent<RectMask2D>());

            Transform list = box.Find("Scroll/Viewport/Content");
            Assert.IsNotNull(list);
            Assert.AreEqual(7, list.childCount);
            Assert.IsNotNull(list.GetComponent<VerticalLayoutGroup>());
            Assert.IsNull(list.GetComponent<HorizontalLayoutGroup>());

            Transform day7 = list.Find("Day7");
            Assert.IsNotNull(day7);
            Text reward = day7.Find("Reward")?.GetComponent<Text>();
            Assert.IsNotNull(reward);
            Assert.IsTrue(reward.text.Contains("조각"));

            Transform footer = box.Find("Footer");
            Assert.IsNotNull(footer);
            Assert.IsNotNull(footer.Find("ClaimButton"));
            Assert.IsNotNull(footer.Find("Close"));
            Assert.Greater(footer.Find("Close").GetSiblingIndex(), footer.Find("ClaimButton").GetSiblingIndex());

            RectTransform claimRect = footer.Find("ClaimButton") as RectTransform;
            RectTransform boxRect = box as RectTransform;
            Assert.IsNotNull(claimRect);
            Assert.IsNotNull(boxRect);
            Assert.IsNotNull(box.Find("Scroll")?.GetComponent<ScrollRect>());
            Assert.AreEqual(new Vector2(0.5f, 0.5f), boxRect.anchorMin);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), boxRect.anchorMax);
            Assert.LessOrEqual(boxRect.sizeDelta.y, 1120f);
            RectTransform titleRect = box.Find("Title") as RectTransform;
            Assert.LessOrEqual(titleRect.anchoredPosition.y, -39f);
            RectTransform footerRect = footer as RectTransform;
            Assert.GreaterOrEqual(footerRect.anchoredPosition.y, 39f);

            panel.Hide();
        }

        [Test]
        public void SettingsPanel_ScrollsContent_AndKeepsCloseInsideBox()
        {
            _hostGo = new GameObject("SettingsHost", typeof(SettingsPanelController));
            SettingsPanelController panel = _hostGo.GetComponent<SettingsPanelController>();
            panel.Show();

            Transform box = GameObject.Find("SettingsPanel")?.transform.Find("SafeArea/Box");
            Assert.IsNotNull(box);
            Assert.IsNotNull(box.GetComponent<RectMask2D>());
            Assert.IsNotNull(box.Find("Scroll")?.GetComponent<ScrollRect>());
            Assert.AreEqual("Box", box.Find("Close")?.parent.name);
            RectTransform boxRect = box as RectTransform;
            Assert.AreEqual(new Vector2(0.5f, 0.5f), boxRect.anchorMin);
            RectTransform closeRect = box.Find("Close") as RectTransform;
            Assert.GreaterOrEqual(closeRect.anchoredPosition.y, 39f);

            panel.Hide();
        }

        [Test]
        public void SettingsDeleteConfirm_StaysInFrontOfSettingsPanel()
        {
            _hostGo = new GameObject("SettingsHost", typeof(SettingsPanelController));
            SettingsPanelController panel = _hostGo.GetComponent<SettingsPanelController>();
            panel.Show();

            Transform settingsRoot = GameObject.Find("SettingsPanel")?.transform;
            Assert.IsNotNull(settingsRoot);
            Button deleteButton = settingsRoot.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.name == "DeleteData");
            Assert.IsNotNull(deleteButton);
            deleteButton.onClick.Invoke();

            Transform confirm = settingsRoot.Find("DeleteConfirm");
            Assert.IsNotNull(confirm);
            Assert.AreEqual(settingsRoot.childCount - 1, confirm.GetSiblingIndex());
            Assert.IsNotNull(confirm.Find("ConfirmBox"));
            Assert.IsNotNull(confirm.Find("ConfirmBox/Title"));
            Transform cancel = confirm.Find("ConfirmBox/Buttons/Cancel");
            Transform ok = confirm.Find("ConfirmBox/Buttons/Confirm");
            Assert.IsNotNull(cancel);
            Assert.IsNotNull(ok);
            LayoutElement cancelLayout = cancel.GetComponent<LayoutElement>();
            LayoutElement okLayout = ok.GetComponent<LayoutElement>();
            Assert.IsNotNull(cancelLayout);
            Assert.IsNotNull(okLayout);
            Assert.LessOrEqual(cancelLayout.preferredHeight, 64f);
            Assert.LessOrEqual(okLayout.preferredHeight, 64f);
            Assert.LessOrEqual(cancel.GetComponent<RectTransform>().sizeDelta.y, 64f);

            panel.Hide();
        }

        [Test]
        public void Attendance_DoesNotAutoOpen_WhenHigherPriorityOverlayExists()
        {
            _canvasGo = new GameObject("PrivacyConsentDialog");
            _hostGo = new GameObject("AttendanceHost", typeof(AttendancePanelController));
            AttendancePanelController panel = _hostGo.GetComponent<AttendancePanelController>();
            panel.TryShowIfClaimable();
            Assert.IsNull(GameObject.Find("AttendancePanel"));
            Assert.IsFalse(panel.IsOpen);
        }

        [Test]
        public void DifficultyUnlockPopup_UsesCompactCenteredBox()
        {
            _hostGo = new GameObject("UnlockHost", typeof(DifficultyUnlockPopupController));
            _hostGo.GetComponent<DifficultyUnlockPopupController>().Show(new[] { "diff_express" });

            Transform box = GameObject.Find("DifficultyUnlockPopup")?.transform.Find("SafeArea/Box");
            Assert.IsNotNull(box);
            RectTransform rect = box.GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMin);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMax);
            Assert.AreEqual(680f, rect.sizeDelta.x, 0.1f);
            Assert.LessOrEqual(rect.sizeDelta.y, 280f);

            LayoutElement message = box.Find("Content/Message")?.GetComponent<LayoutElement>();
            Assert.IsNotNull(message);
            Assert.AreEqual(0f, message.flexibleHeight);

            LayoutElement confirm = box.Find("Content/ConfirmButton")?.GetComponent<LayoutElement>();
            Assert.IsNotNull(confirm);
            Assert.LessOrEqual(confirm.preferredHeight, 56.1f);
        }
    }
}
