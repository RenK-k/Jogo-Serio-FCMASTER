using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PlayerRadarGraphic : Graphic
    {
        readonly float[] _values = new float[5];
        static readonly Color GridColor = new Color(0.72f, 0.86f, 0.94f, 0.8f);

        protected override void OnEnable()
        {
            base.OnEnable();
            canvasRenderer.cullTransparentMesh = false;
            SetAllDirty();
        }

        public void SetValues(float attack, float technique, float mental, float defending, float physical)
        {
            _values[0] = Mathf.Clamp01(attack);
            _values[1] = Mathf.Clamp01(technique);
            _values[2] = Mathf.Clamp01(mental);
            _values[3] = Mathf.Clamp01(defending);
            _values[4] = Mathf.Clamp01(physical);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            if (rect.width <= 1f || rect.height <= 1f)
                return;
            var center = rect.center;
            var radius = Mathf.Max(8f, Mathf.Min(rect.width, rect.height) * 0.42f);
            var grid = new Vector2[5];
            var data = new Vector2[5];
            for (var i = 0; i < 5; i++)
            {
                var angle = Mathf.PI * 0.5f - i * Mathf.PI * 2f / 5f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                grid[i] = center + direction * radius;
                data[i] = center + direction * radius * Mathf.Max(0.08f, _values[i]);
            }

            for (var ring = 1; ring <= 3; ring++)
            {
                var ringPoints = new Vector2[5];
                for (var i = 0; i < 5; i++)
                {
                    var angle = Mathf.PI * 0.5f - i * Mathf.PI * 2f / 5f;
                    ringPoints[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * ring / 3f;
                }
                for (var i = 0; i < 5; i++)
                    AddLine(vh, ringPoints[i], ringPoints[(i + 1) % 5], 2.2f, GridColor);
            }

            for (var i = 0; i < 5; i++)
                AddLine(vh, center, grid[i], 1.8f, GridColor);

            var fill = color;
            fill.a = 0.52f;
            var fillStart = vh.currentVertCount;
            AddVertex(vh, center, fill);
            for (var i = 0; i < 5; i++)
                AddVertex(vh, data[i], fill);
            for (var i = 0; i < 5; i++)
                vh.AddTriangle(fillStart, fillStart + i + 1, fillStart + ((i + 1) % 5) + 1);

            var edge = color;
            edge.a = 0.98f;
            for (var i = 0; i < 5; i++)
                AddLine(vh, data[i], data[(i + 1) % 5], 3.2f, edge);
        }

        static void AddVertex(VertexHelper vh, Vector2 point, Color tint)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = tint;
            vertex.position = point;
            vh.AddVert(vertex);
        }

        static void AddLine(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color tint)
        {
            var direction = to - from;
            if (direction.sqrMagnitude < 0.001f)
                return;
            var normal = new Vector2(-direction.y, direction.x).normalized * thickness * 0.5f;
            var start = vh.currentVertCount;
            AddVertex(vh, from - normal, tint);
            AddVertex(vh, from + normal, tint);
            AddVertex(vh, to + normal, tint);
            AddVertex(vh, to - normal, tint);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
