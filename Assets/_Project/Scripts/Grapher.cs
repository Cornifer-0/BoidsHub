using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class Grapher : Graphic
{
    [System.Serializable]
    public class LineData
    {
        public string label;
        public Color lineColor = Color.white;
        [HideInInspector] public List<float> points = new List<float>();
    }

    [Header("Lines Config")]
    public List<LineData> lines = new List<LineData>();

    [Header("Settings")]
    [SerializeField] private int maxDataPoints = 100;
    [SerializeField] private float lineThickness = 3f;
    [SerializeField] private float fixedMaxY = 0f; // >0 to lock axis height, 0 to auto-scale

    /// <summary>
    /// Add a data point to a specific line index (0 = Fish, 1 = Calamari, 2 = Crocodile)
    /// </summary>
    public void AddDataPoint(int lineIndex, float value)
    {
        if (lineIndex < 0 || lineIndex >= lines.Count) return;

        lines[lineIndex].points.Add(value);
        if (lines[lineIndex].points.Count > maxDataPoints)
        {
            lines[lineIndex].points.RemoveAt(0);
        }

        SetVerticesDirty(); // Tells Unity Canvas to redraw the mesh
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (lines.Count == 0) return;

        Rect rect = rectTransform.rect;
        float width = rect.width;
        float height = rect.height;

        // Calculate highest Y point across all lines so they share the same scale
        float maxY = fixedMaxY;
        if (maxY <= 0f)
        {
            foreach (var line in lines)
            {
                foreach (float val in line.points)
                    if (val > maxY) maxY = val;
            }
            maxY = Mathf.Max(maxY, 1f);
        }

        // Draw each line
        foreach (var line in lines)
        {
            if (line.points.Count < 2) continue;

            for (int i = 0; i < line.points.Count - 1; i++)
            {
                float x1 = rect.xMin + ((float)i / (maxDataPoints - 1)) * width;
                float y1 = rect.yMin + Mathf.Clamp01(line.points[i] / maxY) * height;

                float x2 = rect.xMin + ((float)(i + 1) / (maxDataPoints - 1)) * width;
                float y2 = rect.yMin + Mathf.Clamp01(line.points[i + 1] / maxY) * height;

                AddLineSegment(vh, new Vector2(x1, y1), new Vector2(x2, y2), line.lineColor);
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