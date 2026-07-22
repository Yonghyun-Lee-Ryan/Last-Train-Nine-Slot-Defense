using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    internal static class UiLayoutUtility
    {
        public static void ResetForVerticalLayout(RectTransform rect, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            rect.localScale = Vector3.one;
        }

        public static LayoutElement EnsureLayoutElement(GameObject target, float preferredHeight, float flexibleWidth = 1f)
        {
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = preferredHeight;
            layout.minHeight = preferredHeight;
            layout.flexibleWidth = flexibleWidth;
            return layout;
        }

        public static void ForceRebuild(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
    }
}
