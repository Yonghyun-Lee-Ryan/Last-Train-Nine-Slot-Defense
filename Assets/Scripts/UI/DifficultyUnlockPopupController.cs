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

            GameObject box = MenuOverlayUi.CreatePanel(
                host,
                "Box",
                new Color(0.12f, 0.16f, 0.24f, 0.96f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.1f, 0.32f);
            boxRect.anchorMax = new Vector2(0.9f, 0.68f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.SetParent(box.transform, false);
            MenuOverlayUi.Stretch(content);
            content.offsetMin = new Vector2(28f, 28f);
            content.offsetMax = new Vector2(-28f, -28f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 16f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text title = MenuOverlayUi.CreateText(content, "Title", "새 난이도 해금!", 40, TextAnchor.MiddleCenter);
            UiLayoutUtility.EnsureLayoutElement(title.gameObject, 56f);
            UiLayoutUtility.ResetForVerticalLayout(title.rectTransform, 56f);

            string body = BuildBody(difficultyIds);
            Text message = MenuOverlayUi.CreateText(content, "Message", body, 30, TextAnchor.UpperCenter);
            LayoutElement messageLayout = UiLayoutUtility.EnsureLayoutElement(message.gameObject, 120f);
            messageLayout.flexibleHeight = 1f;
            UiLayoutUtility.ResetForVerticalLayout(message.rectTransform, 120f);

            Button confirm = MenuOverlayUi.CreateLayoutButton(
                content,
                "ConfirmButton",
                "확인",
                80f,
                () => Destroy(root));
            UiButtonStyler.ApplyStandardTheme(confirm);
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
