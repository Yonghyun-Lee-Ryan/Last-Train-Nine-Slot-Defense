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

            GameObject dim = MenuOverlayUi.CreatePanel(root.transform, "Dim", new Color(0f, 0f, 0f, 0.72f));
            MenuOverlayUi.Stretch(dim.GetComponent<RectTransform>());

            GameObject box = MenuOverlayUi.CreatePanel(
                root.transform,
                "Box",
                new Color(0.12f, 0.16f, 0.24f, 0.96f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(760f, 420f);

            Text title = MenuOverlayUi.CreateText(box.transform, "Title", "새 난이도 해금!", 40, TextAnchor.MiddleCenter);
            StretchTop(title.rectTransform, 0f, -40f, 720f, 80f);

            string body = BuildBody(difficultyIds);
            Text message = MenuOverlayUi.CreateText(box.transform, "Message", body, 30, TextAnchor.UpperLeft);
            StretchTop(message.rectTransform, 0f, -140f, 680f, 180f);

            Button confirm = MenuOverlayUi.CreateButton(
                box.transform,
                "ConfirmButton",
                "확인",
                new Vector2(0f, -160f),
                new Vector2(280f, 80f),
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

        private static void StretchTop(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
