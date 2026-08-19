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
            MainMenuHomeTabs.Active = MainMenuHomeSection.Play;
        }

        [TearDown]
        public void TearDown()
        {
            MainMenuHomeTabs.Active = MainMenuHomeSection.Play;
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
            Assert.IsNotNull(day7.GetComponent<RectMask2D>());
            LayoutElement day7Layout = day7.GetComponent<LayoutElement>();
            Assert.GreaterOrEqual(day7Layout.preferredHeight, 72f);
            Text reward = day7.Find("Reward")?.GetComponent<Text>();
            Assert.IsNotNull(reward);
            Assert.IsTrue(reward.text.Contains("조각"));
            Assert.AreEqual(VerticalWrapMode.Truncate, reward.verticalOverflow);

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
        public void MainMenuLayout_KeepsHomeButtonsUnderContent_AndSettingsClearOfScroll()
        {
            _canvasGo = CreateMenuCanvas();
            Transform safeArea = _canvasGo.transform.Find("SafeArea");
            CreateNamed(safeArea, "Title", typeof(Text));
            CreateNamed(safeArea, "SettingsButton", typeof(Image), typeof(Button));
            CreateNamed(safeArea, "AttendanceButton", typeof(Image), typeof(Button));
            CreateNamed(safeArea, "DailyRunButton", typeof(Image), typeof(Button));
            CreateNamed(safeArea, "StartButton", typeof(Image), typeof(Button));

            MainMenuHomeTabs.Active = MainMenuHomeSection.Play;
            MainMenuUiLayout.Apply(safeArea);

            Transform content = safeArea.Find("MainMenuScroll/Viewport/MainMenuContent");
            Assert.IsNotNull(content);

            Transform attendance = FindNamed(content, "AttendanceButton");
            Assert.IsNotNull(attendance);
            Assert.AreEqual("MainMenuContent", attendance.parent.name);
            Assert.IsFalse(attendance.gameObject.activeSelf);

            Transform settings = safeArea.Find("SettingsButton");
            Assert.IsNotNull(settings);
            Assert.AreEqual("SafeArea", settings.parent.name);

            RectTransform scroll = safeArea.Find("MainMenuScroll") as RectTransform;
            Assert.IsNotNull(scroll);
            Assert.Less(scroll.offsetMax.y, -100f);

            LayoutElement attendanceLayout = attendance.GetComponent<LayoutElement>();
            Assert.IsNotNull(attendanceLayout);
            Assert.Greater(attendanceLayout.preferredHeight, 80f);

            LayoutElement dailyLayout = FindNamed(content, "DailyRunButton")?.GetComponent<LayoutElement>();
            Assert.IsNotNull(dailyLayout);
            Assert.GreaterOrEqual(dailyLayout.preferredHeight, 88f);
            Assert.AreEqual(0f, dailyLayout.flexibleWidth);
            Assert.AreEqual(UiButtonStyler.MenuActionMaxWidth, dailyLayout.preferredWidth);

            LayoutElement startLayout = FindNamed(content, "StartButton")?.GetComponent<LayoutElement>();
            Assert.IsNotNull(startLayout);
            Assert.AreEqual(UiButtonStyler.MenuPrimaryHeight, startLayout.preferredHeight);
            Assert.AreEqual(UiButtonStyler.MenuActionMaxWidth, startLayout.preferredWidth);
            Assert.AreEqual(0f, startLayout.flexibleWidth);

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(contentLayout);
            Assert.AreEqual(80, contentLayout.padding.left);
            Assert.AreEqual(80, contentLayout.padding.right);
        }

        [Test]
        public void UiChromeSprites_HaveNineSliceBorders()
        {
            string[] paths =
            {
                "Assets/Art/Sprites/UI/button_normal.png",
                "Assets/Art/Sprites/UI/button_pressed.png",
                "Assets/Art/Sprites/UI/button_disabled.png",
                "Assets/Art/Sprites/UI/panel.png",
                "Assets/Art/Sprites/UI/card_frame.png",
            };

            for (int i = 0; i < paths.Length; i++)
            {
                var importer = UnityEditor.AssetImporter.GetAtPath(paths[i]) as UnityEditor.TextureImporter;
                Assert.IsNotNull(importer, paths[i]);
                Assert.AreEqual(24f, importer.spriteBorder.x, paths[i]);
                Assert.AreEqual(24f, importer.spriteBorder.y, paths[i]);
                Assert.AreEqual(24f, importer.spriteBorder.z, paths[i]);
                Assert.AreEqual(24f, importer.spriteBorder.w, paths[i]);
            }
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

        [Test]
        public void AttendanceTitle_StaysAboveDayListViewport()
        {
            _hostGo = new GameObject("AttendanceHost", typeof(AttendancePanelController));
            AttendancePanelController panel = _hostGo.GetComponent<AttendancePanelController>();
            panel.Show();

            Transform box = GameObject.Find("AttendancePanel")?.transform.Find("SafeArea/Box");
            Assert.IsNotNull(box);
            RectTransform title = box.Find("Title") as RectTransform;
            RectTransform scroll = box.Find("Scroll") as RectTransform;
            Assert.IsNotNull(title);
            Assert.IsNotNull(scroll);
            Assert.AreEqual("Box", title.parent.name);
            float titleBottom = title.anchoredPosition.y - title.sizeDelta.y;
            Assert.Less(scroll.offsetMax.y, titleBottom);
            panel.Hide();
        }

        [Test]
        public void MainMenuLayout_DoesNotStealAttendanceOverlay()
        {
            _hostGo = new GameObject("AttendanceHost", typeof(AttendancePanelController));
            AttendancePanelController panel = _hostGo.GetComponent<AttendancePanelController>();
            panel.Show();

            Transform attendanceSafe = GameObject.Find("AttendancePanel")?.transform.Find("SafeArea");
            Assert.IsNotNull(attendanceSafe);
            Assert.IsFalse(MainMenuUiLayout.IsMainMenuSafeArea(attendanceSafe));

            MainMenuUiLayout.Apply(attendanceSafe);
            Assert.IsNull(attendanceSafe.Find("MainMenuScroll"));
            Assert.AreEqual("Box", attendanceSafe.Find("Box/Title")?.parent.name);
            Assert.IsNull(attendanceSafe.Find("Box/Title")?.GetComponent<Button>());

            panel.Hide();
        }

        [Test]
        public void FindOwnedSafeArea_KeepsMenuCanvasWhenOverlayExists()
        {
            _canvasGo = CreateMenuCanvas();
            _hostGo = new GameObject("AttendanceHost", typeof(AttendancePanelController));
            _hostGo.GetComponent<AttendancePanelController>().Show();

            Transform owned = MainMenuUiLayout.FindOwnedSafeArea(_canvasGo.GetComponent<Canvas>());
            Assert.AreSame(_canvasGo.transform.Find("SafeArea"), owned);
            Assert.AreNotSame(GameObject.Find("AttendancePanel").transform.Find("SafeArea"), owned);

            _hostGo.GetComponent<AttendancePanelController>().Hide();
        }

        private static GameObject CreateMenuCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            var safeGo = new GameObject("SafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            RectTransform safeRect = safeGo.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            return canvasGo;
        }

        private static GameObject CreateNamed(Transform parent, string name, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            for (int i = 0; i < components.Length; i++)
            {
                if (go.GetComponent(components[i]) == null)
                {
                    go.AddComponent(components[i]);
                }
            }

            if (go.GetComponent<Text>() != null)
            {
                go.GetComponent<Text>().text = name;
            }

            return go;
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
