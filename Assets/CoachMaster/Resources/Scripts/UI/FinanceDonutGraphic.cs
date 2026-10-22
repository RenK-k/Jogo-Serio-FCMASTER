using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class FinanceDonutGraphic : MaskableGraphic
    {
        [SerializeField] float thickness = 24f;
        float[] _values = { 1f };
        Color[] _segmentColors = { new Color(0.2f, 0.85f, 0.42f) };
        Color _emptyColor = new Color(1f, 1f, 1f, 0.12f);

        public void SetValues(float[] values, Color[] colors, Color emptyColor)
        {
            _values = values ?? new float[0];
            _segmentColors = colors ?? new Color[0];
            _emptyColor = emptyColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var radius = Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            if (radius < 2f) return;
            var inner = Mathf.Max(0f, radius - Mathf.Clamp(thickness, 1f, radius * 0.8f));
            var total = 0f;
            for (var i = 0; i < _values.Length; i++) total += Mathf.Max(0f, _values[i]);
            if (total <= 0.001f)
            {
                AddArc(vh, rect.center, radius, inner, 0f, 360f, _emptyColor);
                return;
            }

            var start = -90f;
            for (var i = 0; i < _values.Length; i++)
            {
                var value = Mathf.Max(0f, _values[i]);
                if (value <= 0f) continue;
                var sweep = value / total * 360f;
                var color = i < _segmentColors.Length ? _segmentColors[i] : this.color;
                AddArc(vh, rect.center, radius, inner, start, sweep, color);
                start += sweep;
            }
        }

        static void AddArc(VertexHelper vh, Vector2 center, float outer, float inner, float start, float sweep, Color color)
        {
            var segments = Mathf.Max(2, Mathf.CeilToInt(sweep / 3f));
            for (var i = 0; i < segments; i++)
            {
                var a0 = (start + sweep * i / segments) * Mathf.Deg2Rad;
                var a1 = (start + sweep * (i + 1) / segments) * Mathf.Deg2Rad;
                var v0 = UIVertex.simpleVert;
                var v1 = UIVertex.simpleVert;
                var v2 = UIVertex.simpleVert;
                var v3 = UIVertex.simpleVert;
                v0.color = v1.color = v2.color = v3.color = color;
                v0.position = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * outer;
                v1.position = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * outer;
                v2.position = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * inner;
                v3.position = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * inner;
                var index = vh.currentVertCount;
                vh.AddVert(v0);
                vh.AddVert(v1);
                vh.AddVert(v2);
                vh.AddVert(v3);
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
