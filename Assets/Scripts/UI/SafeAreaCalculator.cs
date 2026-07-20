using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>
    /// Safe Area anchor 계산을 담당하는 순수 함수 모음.
    /// Screen 같은 런타임 전역에 의존하지 않아 EditMode 테스트가 가능하다.
    /// </summary>
    public static class SafeAreaCalculator
    {
        /// <summary>
        /// Safe Area(픽셀)와 화면 크기(픽셀)로부터 anchorMin/anchorMax(0~1)를 계산한다.
        /// </summary>
        /// <returns>계산이 유효하면 true. 잘못된 입력이면 false를 반환하고 out 값은 전체 화면(0~1)이 된다.</returns>
        public static bool TryCalculateAnchors(
            Rect safeArea,
            int screenWidth,
            int screenHeight,
            bool applyHorizontal,
            bool applyVertical,
            out Vector2 anchorMin,
            out Vector2 anchorMax)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;

            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return false;
            }

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;

            min.x /= screenWidth;
            min.y /= screenHeight;
            max.x /= screenWidth;
            max.y /= screenHeight;

            if (!applyHorizontal)
            {
                min.x = 0f;
                max.x = 1f;
            }

            if (!applyVertical)
            {
                min.y = 0f;
                max.y = 1f;
            }

            if (!IsAnchorValid(min, max))
            {
                return false;
            }

            anchorMin = min;
            anchorMax = max;
            return true;
        }

        private static bool IsAnchorValid(Vector2 min, Vector2 max)
        {
            return !float.IsNaN(min.x) && !float.IsNaN(min.y)
                   && !float.IsNaN(max.x) && !float.IsNaN(max.y)
                   && max.x > min.x && max.y > min.y;
        }
    }
}
