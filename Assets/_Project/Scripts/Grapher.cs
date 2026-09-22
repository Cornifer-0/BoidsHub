using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasRenderer))]
public class Grapher : Graphic
{
    // Fixes transparent/invisible UI mesh rendering
    public override Texture mainTexture => Texture2D.whiteTexture;

    [System.Serializable]
    public class LineData
    {
        public string label;
        public Color lineColor = Color.white;
        public TMP_Text endLabel; // <-- Assign your TextMeshPro label here
        [HideInInspector] public List<float> points = new List<float>();
    }

    [Header("Lines Config")]
    public List<LineData> lines = new List<LineData>();

    [Header("Graph Appearance")]
    [SerializeField] private int maxDataPoints = 100;
    [SerializeField] private float lineThickness = 4f;
    [SerializeField] private float fixedMaxY = 0f; // >0 locks height, 0 auto-scales
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.7f);
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private int horizontalGridLines = 4;
    [SerializeField] private Vector2 labelOffset = new Vector2(10f, 0f); // Pixel offset right/left of line end

    [Header("UI Text Bindings (Optional)")]
    public string graphTitle = "Population Size";
    public TMP_Text titleText;
    public TMP_Text yMaxText;
    public TMP_Text yMinText;

    public void AddDataPoint(int lineIndex, float value)
    {
        if (lineIndex < 0 || lineIndex >= lines.Count) return;

        lines[lineIndex].points.Add(value);
        if (lines[lineIndex].points.Count > maxDataPoints)
        {
            lines[lineIndex].points.RemoveAt(0);
        }

        SetVerticesDirty();
    }

    private void LateUpdate()
    {
        UpdateLabelsAndPositions();
    }

    private void UpdateLabelsAndPositions()
    {
        if (lines.Count == 0) return;

        Rect rect = rectTransform.rect;

        // Calculate highest Y scale across all lines
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

        // Global Axis Text Updates
        if (titleText != null) titleText.text = graphTitle;
        if (yMaxText != null) yMaxText.text = maxY.ToString("F2");
        if (yMinText != null) yMinText.text = "0";

        // Update each line's floating end label
        foreach (var line in lines)
        {
            if (line.endLabel == null) continue;

            int pointCount = line.points.Count;
            if (pointCount == 0)
            {
                line.endLabel.gameObject.SetActive(false);
                continue;
            }

            line.endLabel.gameObject.SetActive(true);

            float latestValue = line.points[pointCount - 1];

            // 1. Format String
            line.endLabel.text = string.IsNullOrEmpty(line.label)
                ? $"{latestValue:F2}"
                : $"{line.label}: {latestValue:F2}";

            line.endLabel.color = line.lineColor;

            // 2. Calculate local endpoint position inside Grapher Rect
            float x = rect.xMax; // Last point always reaches the right border
            float y = rect.yMin + Mathf.Clamp01(latestValue / maxY) * rect.height;
            Vector3 localEndpoint = new Vector3(x + labelOffset.x, y + labelOffset.y, 0f);

            // 3. Convert local graph position to World Space for accurate placement
            line.endLabel.transform.position = transform.TransformPoint(localEndpoint);
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;

        // 1. Render Background, Grid Lines, and Border Frame
        DrawBackground(vh, rect);
        DrawGridAndBorder(vh, rect);

        if (lines.Count == 0) return;

        // 2. Calculate Highest Y Value across all lines
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

        // 3. Render Data Lines & Smooth Joint Caps
        foreach (var line in lines)
        {
            int pointCount = line.points.Count;
            if (pointCount < 2) continue;

            Vector2 prevPos = Vector2.zero;

            for (int i = 0; i < pointCount; i++)
            {
                float x = rect.xMin + ((float)i / (pointCount - 1)) * rect.width;
                float y = rect.yMin + Mathf.Clamp01(line.points[i] / maxY) * rect.height;
                Vector2 currentPos = new Vector2(x, y);

                DrawJointCap(vh, currentPos, line.lineColor);

                if (i > 0)
                {
                    DrawLineSegment(vh, prevPos, currentPos, line.lineColor, lineThickness);
                }

                prevPos = currentPos;
            }
        }
    }

    private void DrawBackground(VertexHelper vh, Rect rect)
    {
        UIVertex[] verts = new UIVertex[4];
        verts[0].position = new Vector2(rect.xMin, rect.yMin);
        verts[1].position = new Vector2(rect.xMin, rect.yMax);
        verts[2].position = new Vector2(rect.xMax, rect.yMax);
        verts[3].position = new Vector2(rect.xMax, rect.yMin);

        for (int i = 0; i < 4; i++) verts[i].color = backgroundColor;
        vh.AddUIVertexQuad(verts);
    }

    private void DrawGridAndBorder(VertexHelper vh, Rect rect)
    {
        for (int i = 1; i <= horizontalGridLines; i++)
        {
            float y = rect.yMin + (rect.height / (horizontalGridLines + 1)) * i;
            DrawLineSegment(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), gridColor, 1f);
        }

        DrawLineSegment(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax), borderColor, 1.5f);
        DrawLineSegment(vh, new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMax), borderColor, 1.5f);
        DrawLineSegment(vh, new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin), borderColor, 1.5f);
        DrawLineSegment(vh, new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMin, rect.yMin), borderColor, 1.5f);
    }

    private void DrawLineSegment(VertexHelper vh, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

        UIVertex[] verts = new UIVertex[4];
        verts[0].position = start - normal;
        verts[1].position = start + normal;
        verts[2].position = end + normal;
        verts[3].position = end - normal;

        for (int i = 0; i < 4; i++) verts[i].color = color;
        vh.AddUIVertexQuad(verts);
    }

    private void DrawJointCap(VertexHelper vh, Vector2 center, Color color)
    {
        float radius = lineThickness * 0.5f;
        int segments = 8;
        int startIndex = vh.currentVertCount;

        vh.AddVert(center, color, Vector2.zero);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * (Mathf.PI * 2f / segments);
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vh.AddVert(pos, color, Vector2.zero);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i == segments - 1) ? 1 : i + 2;
            vh.AddTriangle(startIndex, startIndex + 1 + i, startIndex + next);
        }
    }
}