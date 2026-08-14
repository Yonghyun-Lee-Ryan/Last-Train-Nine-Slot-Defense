using LastTrain.Data;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>난이도 신규 해금 팝업.</summary>
    public sealed class DifficultyUnlockPopupController : MonoBehaviour
    {
        private GameObject _overlay;

        public void TryShowPendingUnlocks()
        {
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            string[] pending = Difficulty.DifficultyProgressService.ConsumePendingUnlocks(meta);
            if (pending == null || pending.Length == 0)
            {
                return;
            }

            MetaSaveSystem.Save(meta);
            ShowPopup(pending);
        }

        public void Show(string[] difficultyIds)
        {
            if (difficultyIds == null || difficultyIds.Length == 0)
            {
                return;
            }

            ShowPopup(difficultyIds);
        }

        private void ShowPopup(string[] difficultyIds)
        {
            if (_overlay != null)
            {
                Destroy(_overlay);
            }

            GameObject root = MenuOverlayUi.CreateRoot("DifficultyUnlockPopup", 5000);
            _overlay = root;
            MenuOverlayUi.CreateFullScreenDim(root.transform, new Color(0f, 0f, 0f, 0.72f));
            RectTransform host = MenuOverlayUi.EnsureSafeAreaHost(root.transform);

            int count = Mathf.Max(1, difficultyIds.Length);
            float boxHeight = Mathf.Clamp(220f + (count * 32f), 240f, 420f);
            GameObject box = MenuOverlayUi.CreateCenteredPanel(
                host,
                "Box",
                new Vector2(680f, boxHeight),
                new Color(0.12f, 0.16f, 0.24f, 0.96f));
            MenuOverlayUi.EnableClipping(box);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.SetParent(box.transform, false);
            MenuOverlayUi.Stretch(content);
            content.offsetMin = new Vector2(24f, 20f);
            content.offsetMax = new Vector2(-24f, -20f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text title = MenuOverlayUi.CreateText(content, "Title", "새 난이도 해금!", 34, TextAnchor.MiddleCenter);
            UiLayoutUtility.EnsureLayoutElement(title.gameObject, 44f);
            UiLayoutUtility.ResetForVerticalLayout(title.rectTransform, 44f);

            string body = BuildBody(difficultyIds);
            Text message = MenuOverlayUi.CreateText(content, "Message", body, 28, TextAnchor.MiddleCenter);
            float messageHeight = Mathf.Max(40f, 32f * count);
            LayoutElement messageLayout = UiLayoutUtility.EnsureLayoutElement(message.gameObject, messageHeight);
            messageLayout.flexibleHeight = 0f;
            UiLayoutUtility.ResetForVerticalLayout(message.rectTransform, messageHeight);

            Button confirm = MenuOverlayUi.CreateLayoutButton(
                content,
                "ConfirmButton",
                "확인",
                56f,
                Close);
            UiButtonStyler.ApplyStandardTheme(confirm);
        }

        private void Close()
        {
            Dismiss(notifyAttendance: true);
        }

        private void OnDestroy()
        {
            Dismiss(notifyAttendance: false);
        }

        private void Dismiss(bool notifyAttendance)
        {
            if (_overlay == null)
            {
                return;
            }

            GameObject root = _overlay;
            _overlay = null;
            MenuOverlayUi.DestroyRoot(root);
            if (!notifyAttendance)
            {
                return;
            }

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu != null && menu.isActiveAndEnabled)
            {
                menu.TryShowQueuedAttendance();
            }
        }

        private static string BuildBody(string[] difficultyIds)
        {
            GameDatabase database = GameDatabaseLocator.Load();
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < difficultyIds.Length; i++)
            {
                string id = difficultyIds[i];
                string name = id;
                if (database != null && database.TryGetDifficulty(id, out Difficulty.DifficultyData data))
                {
                    name = data.DisplayName;
                }

                lines.Append("• ").Append(name).Append('\n');
            }

            return lines.ToString().TrimEnd();
        }
    }
}
