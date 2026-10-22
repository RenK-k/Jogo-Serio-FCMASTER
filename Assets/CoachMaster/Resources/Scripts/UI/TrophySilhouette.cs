using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TrophySilhouette : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var scale = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (scale < 2f) return;
            var center = rect.center;
            AddPoly(vh, center, scale, new[]
            {
                new Vector2(-0.25f, 0.34f), new Vector2(0.25f, 0.34f), new Vector2(0.2f, 0.02f),
                new Vector2(0.08f, -0.13f), new Vector2(-0.08f, -0.13f), new Vector2(-0.2f, 0.02f)
            }, color);
            AddRect(vh, center, scale, -0.39f, 0.1f, -0.22f, -0.11f, color);
            AddRect(vh, center, scale, 0.22f, 0.1f, 0.39f, -0.11f, color);
            AddRect(vh, center, scale, -0.065f, -0.14f, 0.065f, -0.31f, color);
            AddRect(vh, center, scale, -0.2f, -0.33f, 0.2f, -0.4f, color);
            AddRect(vh, center, scale, -0.29f, -0.43f, 0.29f, -0.5f, color);
        }

        static void AddRect(VertexHelper vh, Vector2 center, float scale, float x0, float y0, float x1, float y1, Color tint)
        {
            AddPoly(vh, center, scale, new[]
            {
                new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1)
            }, tint);
        }

        static void AddPoly(VertexHelper vh, Vector2 center, float scale, Vector2[] points, Color tint)
        {
            var first = vh.currentVertCount;
            var middle = UIVertex.simpleVert;
            middle.position = center + points[0] * scale;
            middle.color = tint;
            vh.AddVert(middle);
            for (var i = 1; i < points.Length; i++)
            {
                var vertex = UIVertex.simpleVert;
                vertex.position = center + points[i] * scale;
                vertex.color = tint;
                vh.AddVert(vertex);
            }
            for (var i = 1; i < points.Length - 1; i++)
                vh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
