using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class MultiLineGrapher : Graphic
{
    [System.Serializable]
    public class LineSeries
    {
        public string label;
        public Color lineColor = Color.white;
        [HideInInspector] public List<float> points = new List<float>();
    }

    [Header("Graph Setup")]
    public List<LineSeries> seriesList = new List<LineSeries>();
    [SerializeField] private int maxDataPoints = 100;
    [SerializeField] private float lineThickness = 3f;
    [SerializeField] private float fixedMaxY = 0f; // Set >0 to freeze scale, 0 for auto-scale

    public void AddPoint(int seriesIndex, float value)
    {
        if (seriesIndex < 0 || seriesIndex >= seriesList.Count) return;

        var series = seriesList[seriesIndex];
        series.points.Add(value);

        if (series.points.Count > maxDataPoints)
            series.points.RemoveAt(0);

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (seriesList.Count == 0) return;

        Rect rect = rectTransform.rect;
        float width = rect.width;
        float height = rect.height;

        // Determine highest Y value across ALL series for consistent relative scaling
        float maxY = fixedMaxY;
        if (maxY <= 0f)
        {
            foreach (var series in seriesList)
            {
                foreach (float val in series.points)
                    if (val > maxY) maxY = val;
            }
            maxY = Mathf.Max(maxY, 1f);
        }

        // Render each line series
        foreach (var series in seriesList)
        {
            if (series.points.Count < 2) continue;

            for (int i = 0; i < series.points.Count - 1; i++)
            {
                float x1 = rect.xMin + ((float)i / (maxDataPoints - 1)) * width;
                float y1 = rect.yMin + Mathf.Clamp01(series.points[i] / maxY) * height;

                float x2 = rect.xMin + ((float)(i + 1) / (maxDataPoints - 1)) * width;
                float y2 = rect.yMin + Mathf.Clamp01(series.points[i + 1] / maxY) * height;

                AddLineSegment(vh, new Vector2(x1, y1), new Vector2(x2, y2), series.lineColor);
            }
        }
    }

    private void AddLineSegment(VertexHelper vh, Vector2 start, Vector2 end, Color color)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * (lineThickness * 0.5f);

        UIVertex[] verts = new UIVertex[4];
        verts[0].position = start - normal;
        verts[1].position = start + normal;
        verts[2].position = end + normal;
        verts[3].position = end - normal;

        for (int i = 0; i < 4; i++) verts[i].color = color;
        vh.AddUIVertexQuad(verts);
    }
}