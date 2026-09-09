using System.IO;
using System.Text;
using UnityEngine;

public class SimulationLogger : MonoBehaviour
{
    [Header("References")]
    public MultiLineGrapher populationGraph;

    [Header("Logging Settings")]
    [SerializeField] private float sampleInterval = 1.0f;
    [SerializeField] private string csvFileName = "BoidsSimulationStats.csv";

    // Species indices: 0 = Fish, 1 = Calamari, 2 = Crocodile
    private readonly int[] deathCounts = new int[3];
    private readonly int[] mutationCounts = new int[3];

    private string filePath;
    private float timer;

    private void Awake()
    {
        filePath = Path.Combine(Application.dataPath, csvFileName);
        InitializeCSV();
    }

    private void InitializeCSV()
    {
        StringBuilder header = new StringBuilder();
        header.Append("Time,");
        
        // Per-species headers
        string[] speciesNames = { "Fish", "Calamari", "Croc" };
        foreach (var name in speciesNames)
        {
            header.Append($"{name}_Pop,{name}_Deaths,{name}_Mutations,");
            header.Append($"{name}_AvgCohesion,{name}_AvgSeparation,{name}_AvgAlignment,");
        }
        
        header.Length--; // Trim trailing comma
        File.WriteAllText(filePath, header.ToString() + "\n");
    }

    public void RegisterDeath(int speciesIndex)
    {
        if (speciesIndex >= 0 && speciesIndex < 3) deathCounts[speciesIndex]++;
    }

    public void RegisterMutation(int speciesIndex)
    {
        if (speciesIndex >= 0 && speciesIndex < 3) mutationCounts[speciesIndex]++;
    }

    public void SampleAndLog(Boid[] boids)
    {
        timer += Time.deltaTime;
        if (timer < sampleInterval) return;
        timer = 0f;

        // Metric arrays for the 3 species
        int[] pops = new int[3];
        float[] sumCohesion = new float[3];
        float[] sumSeparation = new float[3];
        float[] sumAlignment = new float[3];

        // Aggregate live stats
        for (int i = 0; i < boids.Length; i++)
        {
            if (boids[i].isDead) continue;

            int s = (int)boids[i].species; // Assumes species enum maps to 0, 1, 2
            if (s < 0 || s >= 3) continue;

            pops[s]++;
            sumCohesion[s] += boids[i].trait.cohesionWeight;
            sumSeparation[s] += boids[i].trait.separationWeight;
            sumAlignment[s] += boids[i].trait.alignmentWeight;
        }

        // 1. Update Live UI Graph
        if (populationGraph != null)
        {
            for (int s = 0; s < 3; s++)
                populationGraph.AddPoint(s, pops[s]);
        }

        // 2. Append row to CSV
        StringBuilder row = new StringBuilder();
        row.Append($"{Time.timeSinceLevelLoad:F1},");

        for (int s = 0; s < 3; s++)
        {
            int count = pops[s];
            float avgCoh = count > 0 ? sumCohesion[s] / count : 0f;
            float avgSep = count > 0 ? sumSeparation[s] / count : 0f;
            float avgAlign = count > 0 ? sumAlignment[s] / count : 0f;

            row.Append($"{count},{deathCounts[s]},{mutationCounts[s]},");
            row.Append($"{avgCoh:F2},{avgSep:F2},{avgAlign:F2},");
        }

        row.Length--; // Trim trailing comma
        File.AppendAllText(filePath, row.ToString() + "\n");
    }
}