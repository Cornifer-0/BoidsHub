using System.IO;
using UnityEngine;

public class CSVExporter : MonoBehaviour
{
    public static CSVExporter Instance { get; private set; }

    private StreamWriter writer;
    private string filePath;
    private int rowCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Save directly in Assets folder: "Assets/evolution_data.csv"
        filePath = Path.Combine(Application.dataPath, "evolution_data.csv");

        try
        {
            writer = new StreamWriter(filePath, false);
            writer.WriteLine("time,activeFish,avgSpeed,avgSep,avgCoh,avgAli,avgMaxSpeed,avgMaxForce,avgHuntWeight,avgHuntRadius,avgFleeWeight,avgFleeRadius");
            writer.Flush();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CSVExporter] Error creating CSV file: {e.Message}");
        }
    }

    private void Start()
    {
        Debug.Log($"<color=cyan>[CSVExporter] Initialized! File path: {filePath}</color>");
    }

    public void LogDataRow(
        float time, 
        int activeFish, 
        float avgSpeed, 
        float sep, 
        float coh, 
        float ali, 
        float maxSpeed, 
        float maxForce, 
        float huntWeight, 
        float huntRadius, 
        float fleeWeight, 
        float fleeRadius)
    {
        if (writer == null) return;

        writer.WriteLine(
            $"{time:F2},{activeFish},{avgSpeed:F4},{sep:F4},{coh:F4},{ali:F4}," +
            $"{maxSpeed:F4},{maxForce:F4},{huntWeight:F4},{huntRadius:F4},{fleeWeight:F4},{fleeRadius:F4}"
        );

        writer.Flush();
        rowCount++;
    }

    private void OnDestroy() => CloseStream();
    private void OnApplicationQuit() => CloseStream();

    private void CloseStream()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
            Debug.Log($"<color=green>[CSVExporter] Successfully saved {rowCount} data rows to: {filePath}</color>");

#if UNITY_EDITOR
            // Refresh Unity Project Window so the file appears immediately in Assets
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}