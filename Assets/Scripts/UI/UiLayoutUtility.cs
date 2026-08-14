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

        public static void DestroyChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

        public static void DestroyRoot(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            root.SetActive(false);
            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
