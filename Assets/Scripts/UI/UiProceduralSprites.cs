using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>런타임 UI용 단순 스프라이트(얇은 원·방향 화살표).</summary>
    public static class UiProceduralSprites
    {
        private static Sprite _thinRing;
        private static Sprite _directionArrow;

        /// <summary>얇은 테두리 + 아주 옅은 채움의 사거리 원.</summary>
        public static Sprite SoftCircle()
        {
            if (_thinRing != null)
            {
                return _thinRing;
            }

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "UiThinRangeRing",
            };

            float center = (size - 1) * 0.5f;
            float outer = center - 1.5f;
            // ~2px 테두리 (256 기준) — 기존 5px보다 훨씬 얇음.
            float ringInner = outer - 2.2f;
            float softOuter = outer + 0.8f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > softOuter)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > outer)
                    {
                        float a = 1f - Mathf.InverseLerp(outer, softOuter, dist);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.55f * a));
                    }
                    else if (dist >= ringInner)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.92f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.14f));
                    }
                }
            }

            tex.Apply(false, false);
            _thinRing = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            _thinRing.name = "UiThinRangeRing";
            return _thinRing;
        }

        /// <summary>오른쪽을 가리키는 채운 삼각형 화살표.</summary>
        public static Sprite Chevron()
        {
            if (_directionArrow != null)
            {
                return _directionArrow;
            }

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "UiDirectionArrow",
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size;
                    float ny = (y + 0.5f) / size;
                    // 왼쪽 중앙에서 오른쪽 끝으로 벌어지는 삼각형.
                    float halfHeight = Mathf.Lerp(0.08f, 0.42f, 1f - nx);
                    bool inside = nx >= 0.12f && nx <= 0.92f && Mathf.Abs(ny - 0.5f) <= halfHeight;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            tex.Apply(false, false);
            _directionArrow = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            _directionArrow.name = "UiDirectionArrow";
            return _directionArrow;
        }
    }
}
