using System.Collections.Generic;
using UnityEngine;

public class FoodManager : MonoBehaviour
{
    public static FoodManager Instance { get; private set; }

    [Header("Food Prefabs Array")]
    [Tooltip("Add different food prefabs here (e.g. Algae, Plankton, Shrimp)")]
    public GameObject[] foodPrefabs;

    [Header("Spawn Settings")]
    public int totalFoodCount = 50;
    public float spawnRadius = 75f;

    private List<FoodItem> foodPool = new List<FoodItem>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeFoodPool();
    }

    private void InitializeFoodPool()
    {
        if (foodPrefabs == null || foodPrefabs.Length == 0) return;

        for (int i = 0; i < totalFoodCount; i++)
        {
            GameObject randomPrefab = foodPrefabs[Random.Range(0, foodPrefabs.Length)];
            GameObject instance = Instantiate(randomPrefab, transform);

            FoodItem item = instance.GetComponent<FoodItem>();
            if (item == null) item = instance.AddComponent<FoodItem>();

            item.Spawn(Random.insideUnitSphere * spawnRadius);
            foodPool.Add(item);
        }
    }

    public FoodItem GetNearestAvailableFood(Vector3 seekerPos, float perceptionRadius)
    {
        FoodItem nearest = null;
        float shortestDistSqr = perceptionRadius * perceptionRadius;

        for (int i = 0; i < foodPool.Count; i++)
        {
            FoodItem food = foodPool[i];
            if (!food.IsAvailable) continue;

            float distSqr = (food.transform.position - seekerPos).sqrMagnitude;
            if (distSqr < shortestDistSqr)
            {
                shortestDistSqr = distSqr;
                nearest = food;
            }
        }

        return nearest;
    }

    public void ConsumeFood(FoodItem food)
    {
        if (food == null || !food.IsAvailable) return;

        food.Consume(() =>
        {
            // Respawn in a new position once fade-out completes
            food.Spawn(Random.insideUnitSphere * spawnRadius);
        });
    }
}