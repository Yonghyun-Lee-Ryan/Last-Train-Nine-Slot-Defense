using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>
    /// Overlay Canvas 월드(픽셀)과 SafeArea 로컬(설계 단위) 변환.
    /// Canvas localScale이 0으로 직렬화되는 Overlay에서도 RectTransformUtility / world corners를 쓴다.
    /// </summary>
    public static class BattleCombatSpace
    {
        public static Vector2 WorldToLocal(RectTransform space, Vector3 worldPosition)
        {
            if (space == null)
            {
                return worldPosition;
            }

            Camera camera = ResolveCamera(space);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    space,
                    screenPoint,
                    camera,
                    out Vector2 local))
            {
                return local;
            }

            return space.InverseTransformPoint(worldPosition);
        }

        public static Vector3 LocalToWorld(RectTransform space, Vector2 localPosition)
        {
            if (space == null)
            {
                return localPosition;
            }

            Rect rect = space.rect;
            float width = rect.width;
            float height = rect.height;
            if (width <= 0.0001f || height <= 0.0001f)
            {
                return space.TransformPoint(localPosition);
            }

            float u = (localPosition.x - rect.xMin) / width;
            float v = (localPosition.y - rect.yMin) / height;
            var corners = new Vector3[4];
            space.GetWorldCorners(corners);
            Vector3 bottom = Vector3.LerpUnclamped(corners[0], corners[3], u);
            Vector3 top = Vector3.LerpUnclamped(corners[1], corners[2], u);
            return Vector3.LerpUnclamped(bottom, top, v);
        }

        public static float DistanceLocal(RectTransform space, Vector3 worldA, Vector3 worldB)
        {
            return Vector2.Distance(WorldToLocal(space, worldA), WorldToLocal(space, worldB));
        }

        private static Camera ResolveCamera(RectTransform space)
        {
            Canvas canvas = space.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }
    }
}
