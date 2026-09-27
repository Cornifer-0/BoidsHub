using UnityEngine;

public class WorldCoordinatesVisualizer : MonoBehaviour
{
    [Header("Visibility Toggle")]
    [Tooltip("Check or uncheck to show/hide the axes in the simulation")]
    public bool showAxes = true;

    [Header("Axis Dimensions")]
    [SerializeField] private float axisLength = 60f;
    [SerializeField] private float axisLineWidth = 0.08f;
    [SerializeField] private bool dualDirection = true; // Extends into both positive and negative directions

    [Header("Subtle Tick Marks ('Dents')")]
    [SerializeField] private float tickInterval = 5f;   // Distance between each dent
    [SerializeField] private float tickLength = 0.5f;   // Length of perpendicular tick mark
    [SerializeField] private float tickLineWidth = 0.04f;

    [Header("Monochrome Style")]
    [SerializeField] private Color axisColor = new Color(0.85f, 0.85f, 0.85f, 0.25f); // Subtle, semi-transparent off-white
    [SerializeField] private bool showOriginMarker = true;
    [SerializeField] private float originRadius = 0.3f;

    private GameObject axesContainer;
    private bool lastShowAxesState;

    private void Start()
    {
        BuildAxes();
        lastShowAxesState = showAxes;
    }

    private void Update()
    {
        // Handle dynamic toggle from Inspector during Play mode
        if (showAxes != lastShowAxesState)
        {
            lastShowAxesState = showAxes;
            if (axesContainer != null)
            {
                axesContainer.SetActive(showAxes);
            }
        }
    }

    private void BuildAxes()
    {
        if (axesContainer != null)
        {
            Destroy(axesContainer);
        }

        axesContainer = new GameObject("AxesContainer");
        axesContainer.transform.SetParent(transform);
        axesContainer.transform.localPosition = Vector3.zero;

        // Shared Unlit Sprites Material for clean alpha blending
        Material lineMat = new Material(Shader.Find("Sprites/Default"));

        // 1. Origin Marker
        if (showOriginMarker)
        {
            GameObject origin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            origin.name = "Origin";
            origin.transform.SetParent(axesContainer.transform);
            origin.transform.position = Vector3.zero;
            origin.transform.localScale = Vector3.one * originRadius;

            Destroy(origin.GetComponent<Collider>());
            Renderer r = origin.GetComponent<Renderer>();
            r.material = lineMat;
            r.material.color = axisColor;
        }

        // 2. Main Axes (X, Y, Z)
        CreateSingleAxis("Axis_X", Vector3.right, Vector3.up, lineMat);
        CreateSingleAxis("Axis_Y", Vector3.up, Vector3.right, lineMat);
        CreateSingleAxis("Axis_Z", Vector3.forward, Vector3.up, lineMat);

        axesContainer.SetActive(showAxes);
    }

    private void CreateSingleAxis(string axisName, Vector3 dir, Vector3 perpendicular, Material mat)
    {
        GameObject axisObj = new GameObject(axisName);
        axisObj.transform.SetParent(axesContainer.transform);

        float minDist = dualDirection ? -axisLength : 0f;
        float maxDist = axisLength;

        // Main Axis Line
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.SetParent(axisObj.transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, dir * minDist);
        lr.SetPosition(1, dir * maxDist);
        lr.startWidth = axisLineWidth;
        lr.endWidth = axisLineWidth;
        lr.material = mat;
        lr.startColor = axisColor;
        lr.endColor = axisColor;

        // Perpendicular Tick Marks ("Dents")
        if (tickInterval <= 0) return;

        GameObject ticksContainer = new GameObject("Ticks");
        ticksContainer.transform.SetParent(axisObj.transform);

        for (float pos = minDist; pos <= maxDist; pos += tickInterval)
        {
            if (Mathf.Abs(pos) < 0.01f) continue; // Skip zero to keep origin clean

            Vector3 tickCenter = dir * pos;
            Vector3 tickStart = tickCenter - (perpendicular * (tickLength * 0.5f));
            Vector3 tickEnd = tickCenter + (perpendicular * (tickLength * 0.5f));

            GameObject tickObj = new GameObject($"Tick_{pos}");
            tickObj.transform.SetParent(ticksContainer.transform);

            LineRenderer tickLr = tickObj.AddComponent<LineRenderer>();
            tickLr.useWorldSpace = true;
            tickLr.positionCount = 2;
            tickLr.SetPosition(0, tickStart);
            tickLr.SetPosition(1, tickEnd);
            tickLr.startWidth = tickLineWidth;
            tickLr.endWidth = tickLineWidth;
            tickLr.material = mat;
            tickLr.startColor = axisColor;
            tickLr.endColor = axisColor;
        }
    }

    private void OnValidate()
    {
        // Rebuild axes automatically if tweaking values in Inspector during Play Mode
        if (Application.isPlaying && axesContainer != null)
        {
            BuildAxes();
        }
    }
}