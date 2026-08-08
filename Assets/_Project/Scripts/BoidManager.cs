using UnityEngine;
using System.Collections.Generic;


public struct Boid 
{ 
    public Vector3 pos;
    public Vector3 vel;
    public Vector3 acc;
};


public class BoidManager : MonoBehaviour
{
    [Header("Boid Settings")]
    public int InitialNumberOfBoids = 100;
    public float SpawnRadius = 10f;
    public GameObject BoidPrefab;

    public float CohesionWeight = 1f;
    public float SeparationWeight = 1f;
    public float AlignmentWeight = 1f;

    public float PerceptionRadius = 10f;

    public float MaxSpeed = 10f;
    public float MaxForce = 5f;
    public float InitialSpeed = 5f;

    [Header("Boundary Settings")]
    public Vector3 boundsCenter = Vector3.zero;
    public float boundsRadius = 25f;
    public float boundaryWeight = 2.0f;

    [Header("Grid Settings")]
    public  float cellSize = 5f;

    private Dictionary<Vector3Int, List<int>> grid = new Dictionary<Vector3Int, List<int>>();

    private Boid[] boids;
    private Transform[] boidTransforms;


    void Start(){
        boids = new Boid[InitialNumberOfBoids];
        boidTransforms = new Transform[InitialNumberOfBoids]; 

        for(int i = 0; i < InitialNumberOfBoids; i++) {
            // Ger random pos
            Vector3 spawnPos = getRandomSpawn();
            Vector3 spawnVel = getRandomDirection() * InitialSpeed; // randomDir * initialSpeed

            GameObject newBoid = Instantiate(BoidPrefab, spawnPos, Quaternion.LookRotation(spawnVel));

            boidTransforms[i] = newBoid.transform;

            boids[i].pos = spawnPos;
            boids[i].vel = spawnVel;
            boids[i].acc = Vector3.zero;
        }
    }

    private Vector3 getRandomDirection(){
        return Random.onUnitSphere;
    }

    private Vector3 getRandomSpawn(){
        return transform.position + Random.insideUnitSphere * SpawnRadius;
    }

    void Update()
    { 
        UpdateGrid();

        for(int i = 0; i < InitialNumberOfBoids; i++) {
            Boid boid = boids[i];
            Vector3Int myCell = GetCellCoord(boid.pos);
            List<int> neighbors = getNeighbors(myCell, i);

            //Adding the magic of Craig Reynolds
            //Fsteer = Vdesired - Vcurrent


            // Cohesion
            // Avarage center of mass = 1/N ( sum of positions )
            // Dcoheision = Pcenter - Pself
            
            
            Vector3 centerOfMass = Vector3.zero;
            Vector3 avgVelocity = Vector3.zero;
            Vector3 separationVector = Vector3.zero;

            int neighborCount = 0;

            foreach ( int neighborIdx in neighbors)  {
                Boid neighbor = boids[neighborIdx];
                float dist = Vector3.Distance(boid.pos, neighbor.pos);
            
                if(dist > 0 && dist < PerceptionRadius) { 
                    centerOfMass += neighbor.pos;
                    avgVelocity += neighbor.vel;

                    separationVector += (boid.pos - neighbor.pos).normalized / dist;
                    neighborCount++;
                }

            }
            Vector3 accel = Vector3.zero;

            if ( neighborCount > 0 ) { 
                centerOfMass /= neighborCount;
                avgVelocity /= neighborCount;

                Vector3 cohesionForce = SteerTowards(boid, centerOfMass - boid.pos);
                Vector3 alignmentForce = SteerTowards(boid, avgVelocity);
                Vector3 separationForce = SteerTowards(boid, separationVector);

                accel += cohesionForce * CohesionWeight;
                accel += alignmentForce * AlignmentWeight;
                accel += separationForce * SeparationWeight;
            }

            Vector3 offsetToCenter = boundsCenter - boid.pos;
            float distFromCenter = offsetToCenter.magnitude;

            if (distFromCenter > boundsRadius)
            {
                float excessDistance = distFromCenter - boundsRadius;
                
                // Steering force directed to center, scaled linearly by excess distance
                Vector3 boundaryForce = SteerTowards(boid, offsetToCenter) * excessDistance;
                
                accel += boundaryForce * boundaryWeight;
            }

            // Apply physics
            boid.vel = Vector3.ClampMagnitude(boid.vel + accel * Time.deltaTime, MaxSpeed);
            boid.pos += boid.vel * Time.deltaTime;

            boids[i] = boid;

            boidTransforms[i].position = boid.pos;
            if(boid.vel != Vector3.zero) boidTransforms[i].forward = boid.vel.normalized;

        }

    }

    private Vector3 SteerTowards(Boid boid, Vector3 targetDirection) { 
        if (targetDirection == Vector3.zero) return Vector3.zero;

        Vector3 desiredVelocity = targetDirection.normalized * MaxSpeed;
        Vector3 steer = desiredVelocity - boid.vel;
        return Vector3.ClampMagnitude(steer, MaxForce);
    }

    private void UpdateGrid(){
        grid.Clear();

        for(int i = 0; i < InitialNumberOfBoids; i++){
            Boid b = boids[i];
            Vector3Int cellCoord = GetCellCoord(b.pos);

            if ( !grid.ContainsKey(cellCoord) ) {
                grid.Add(cellCoord , new List<int>());
            }

            grid[cellCoord].Add(i);
        }
    }

    public List<int> getNeighbors(Vector3Int coord, int currentBoidIndex) {
        
        List<int> neighbors = new List<int>();
        // we check all 26 neighbors + 1 ( the current coord ) = 27
        for(int x = -1; x <= 1; x++) {
            for(int y = -1; y <= 1; y++) {
                for(int z = -1; z <= 1; z++) { 
                    Vector3Int NeighborCell = coord + new Vector3Int(x, y, z);

                    if(grid.ContainsKey(NeighborCell)) {
                        neighbors.AddRange(grid[NeighborCell]); // add them all ? 
                    }
                }
            }
        }
        neighbors.Remove(currentBoidIndex);
        return neighbors;
    }

    private Vector3Int GetCellCoord(Vector3 pos){
        int x = Mathf.FloorToInt(pos.x/cellSize);
        int y = Mathf.FloorToInt(pos.y/cellSize);
        int z = Mathf.FloorToInt(pos.z/cellSize);

        return new Vector3Int(x, y, z);
    }
}