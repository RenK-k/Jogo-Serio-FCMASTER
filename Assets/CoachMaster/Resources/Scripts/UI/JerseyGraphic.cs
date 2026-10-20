using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class JerseyGraphic : Graphic
    {
        static readonly Vector2[] Shape =
        {
            new Vector2(0.28f, 1f),
            new Vector2(0.08f, 0.78f),
            new Vector2(0f, 0.61f),
            new Vector2(0.23f, 0.48f),
            new Vector2(0.32f, 0.69f),
            new Vector2(0.35f, 0.15f),
            new Vector2(0.40f, 0.05f),
            new Vector2(0.60f, 0.05f),
            new Vector2(0.65f, 0.15f),
            new Vector2(0.68f, 0.69f),
            new Vector2(0.77f, 0.48f),
            new Vector2(1f, 0.61f),
            new Vector2(0.92f, 0.78f),
            new Vector2(0.72f, 1f),
            new Vector2(0.61f, 0.87f),
            new Vector2(0.39f, 0.87f)
        };

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var center = new Vector2(rect.center.x, rect.center.y);
            vertexHelper.AddVert(center, color, new Vector2(0.5f, 0.5f));

            for (var i = 0; i < Shape.Length; i++)
            {
                var point = new Vector2(
                    Mathf.Lerp(rect.xMin, rect.xMax, Shape[i].x),
                    Mathf.Lerp(rect.yMin, rect.yMax, Shape[i].y));
                vertexHelper.AddVert(point, color, Shape[i]);
            }

            for (var i = 0; i < Shape.Length; i++)
            {
                var current = i + 1;
                var next = (i + 1) % Shape.Length + 1;
                vertexHelper.AddTriangle(0, current, next);
            }
        }
    }
}
