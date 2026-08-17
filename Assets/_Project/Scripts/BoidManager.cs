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
    public BoidCameraFollow cam;

    public float CohesionWeight = 1f;
    public float SeparationWeight = 1f;
    public float AlignmentWeight = 1f;

    public float PerceptionRadius = 10f; // coheison + alignment
    public float PerceptionRadiusSeparation = 3f;

    public float MaxSpeed = 10f;
    public float MaxForce = 5f;
    public float InitialSpeed = 5f;

    [Header("Boundary Settings")]
    public Vector3 boundsCenter = Vector3.zero;
    public float boundsRadius = 25f;
    public float boundaryWeight = 2.0f;

    [Header("Visuals")]
    public float rotationSpeed = 540f;

    [Header("Arrows")]
    public bool simulationWithArrows = false;

    public bool showBoundRadius = false;

    public bool showLineVelocity = false;
    public bool showLineVelocityAll = false;

    public bool showLinePosition = false;
    public bool showLinePositionAll = false;

    [Header("Extra Visuals")]

    public Material transparentMaterialSphere;
    public Material transparentMaterialChunk;
    private Transform[] perceptionSpheres;
    private MeshRenderer[] activeChunkCubes = new MeshRenderer[27];
    

    public bool showVisualRange = false;
    public bool showVisualRangeAll = true;
    public bool showChunks = false;

    private Transform[] velocityArrows;
    private Transform[] positionArrows;

    [Header("Organic Grid Trail")]
    public int maxTrailChunks = 50;

    private Queue<MeshRenderer> chunkPool = new Queue<MeshRenderer>();
    private Queue<Vector3Int> activeChunkHistory = new Queue<Vector3Int>();
    private Dictionary<Vector3Int, MeshRenderer> activeChunksMap = new Dictionary<Vector3Int, MeshRenderer>();

    [Header("Grid Settings")]
    public  float cellSize = 5f;

    private Dictionary<Vector3Int, List<int>> grid = new Dictionary<Vector3Int, List<int>>();

    private Boid[] boids;
    private Transform[] boidTransforms;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public LayerMask attractLayer;
    public float avoidDistance = 5f;
    public float avoidWeight = 5f;

    public float attractDistance = 5f;
    public float attractionWeight = 5f;

    public float fishBoundsMargin = 1.5f;

    [Header("Avoidance / Attractor Visualizers")]
    public Material obstacleWarningMaterial;
    private Transform[] marginHitSpheres; // one per fish
    private Transform[] obstacleArrows;

    public bool showObstacleAvoidance = false;
    public bool showAllObstacleAvoidance = false;


    void Start(){
        boids = new Boid[InitialNumberOfBoids];
        boidTransforms = new Transform[InitialNumberOfBoids];
        if ( simulationWithArrows ) velocityArrows = new Transform[InitialNumberOfBoids];
        if ( simulationWithArrows ) positionArrows = new Transform[InitialNumberOfBoids];
        if ( simulationWithArrows ) perceptionSpheres = new Transform[InitialNumberOfBoids];
        if ( simulationWithArrows ) marginHitSpheres = new Transform[InitialNumberOfBoids];
        if ( simulationWithArrows ) obstacleArrows = new Transform[InitialNumberOfBoids];

        // Generate the 50 cubes for our pool
        float visualSize = cellSize * 0.95f; // 5% gap

        for (int i = 0; i < maxTrailChunks; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cube.GetComponent<Collider>()); 
            
            MeshRenderer rend = cube.GetComponent<MeshRenderer>();
            rend.material = transparentMaterialChunk;
            
            cube.transform.localScale = new Vector3(visualSize, visualSize, visualSize);
            cube.SetActive(false);
            
            // Add to our available pool
            chunkPool.Enqueue(rend);
        }


        for(int i = 0; i < InitialNumberOfBoids; i++) {
            // Ger random pos
            Vector3 spawnPos = getRandomSpawn();
            Vector3 spawnVel = getRandomDirection() * InitialSpeed; // randomDir * initialSpeed

            GameObject newBoid = Instantiate(BoidPrefab, spawnPos, Quaternion.LookRotation(spawnVel));

            boidTransforms[i] = newBoid.transform;

            boids[i].pos = spawnPos;
            boids[i].vel = spawnVel;
            boids[i].acc = Vector3.zero;

            // if( simulationWithArrows ) {
            //     LineRenderer lr = newBoid.AddComponent<LineRenderer>();
            //     lr.useWorldSpace = true;
            //     lr.positionCount = 2;
            //     lr.material = new Material(Shader.Find("Sprites/Default"));

            //     lr.startWidth = 0.1f;
            //     lr.endWidth = 0.025f;


            //     velocityArrows[i] = lr;
            // }

            if(simulationWithArrows) { 
                // Velcoity
                GameObject cy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

                Destroy(cy.GetComponent<Collider>());

                MeshRenderer renderer = cy.GetComponent<MeshRenderer>();
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = Color.red;

                velocityArrows[i] = cy.transform;

                // Position
                GameObject cy2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

                Destroy(cy2.GetComponent<Collider>());

                MeshRenderer renderer2 = cy2.GetComponent<MeshRenderer>();
                renderer2.material = new Material(Shader.Find("Sprites/Default"));
                renderer2.material.color = Color.green;

                positionArrows[i] = cy2.transform;

                // Obstacle
                GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

                Destroy(obs.GetComponent<Collider>());

                MeshRenderer renderer3 = obs.GetComponent<MeshRenderer>();
                renderer3.material = new Material(Shader.Find("Sprites/Default"));
                renderer3.material.color = Color.yellow;

                obstacleArrows[i] = obs.transform;
                obstacleArrows[i].gameObject.SetActive(false);

                //Spheres of perceptions
                GameObject ps = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(ps.GetComponent<Collider>());
                ps.GetComponent<MeshRenderer>().material = transparentMaterialSphere;

                perceptionSpheres[i] = ps.transform;
                perceptionSpheres[i].gameObject.SetActive(false);


                // Obstacle avoidance
                GameObject hitSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(hitSphere.GetComponent<Collider>());

                // 2. Apply the warning material
                hitSphere.GetComponent<MeshRenderer>().material = obstacleWarningMaterial;

                // 3. Set the scale perfectly to your margin
                // Remember: Scale is diameter, so we multiply the radius by 2!
                float marginDiameter = fishBoundsMargin * 2f;
                hitSphere.transform.localScale = new Vector3(marginDiameter, marginDiameter, marginDiameter);

                // 4. Save reference and hide it by default
                marginHitSpheres[i] = hitSphere.transform;
                marginHitSpheres[i].gameObject.SetActive(false);

            }
        }
    }

    private Vector3 getRandomDirection(){
        return Random.onUnitSphere;
    }

    private Vector3 getRandomSpawn(){
        return transform.position + Random.insideUnitSphere * SpawnRadius;
    }

    public Vector3 getFishPosition(int i) {
        return boids[i].pos;
    }

    public Vector3 getFishSpeed(int i) { 
        return boids[i].vel;
    }

    void Update()
    { 

        //simulation

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

                    if (dist < PerceptionRadiusSeparation){
                        separationVector += (boid.pos - neighbor.pos).normalized / dist;
                    }
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

                // Add the new Obstacle Avoidance rule!
                Vector3 avoidance = CalculateObstacleAvoidance(boid.pos, boid.vel);
                Vector3 attraction = CalculateAttractionForce(boid.pos);
                //Vector3 attraction = CalculateObstacleAvoidance(boid.pos, boid.vel);
                accel += avoidance * avoidWeight + attraction * attractionWeight;
                //accel += attraction * attractWeight;
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
            //if(boid.vel != Vector3.zero) boidTransforms[i].forward = boid.vel.normalized;

            if (boid.vel != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(boid.vel.normalized);
                boidTransforms[i].rotation = Quaternion.RotateTowards(
                    boidTransforms[i].rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }

            // if (simulationWithArrows)
            // {
            //     // 1. Determine if THIS specific fish should have an arrow this frame
            //     bool shouldDraw = (cam.targetBoidIndex == i && showLineVelocity) || showLineVelocityAll;

            //     // 2. Turn the LineRenderer ON or OFF based on that check
            //     velocityArrows[i].enabled = shouldDraw;

            //     // 3. If it's ON, update its position
            //     if (shouldDraw) 
            //     { 

            //         //Vector3 margin = new Vector3( 0.0f, 0.0f, 0.0f); // not needed actually, or very very little 

            //         DrawArrow( boid.pos, boid.pos + boid.vel , Color.white, i );
            //     }
            // }

            if ( simulationWithArrows ) { 
                // velocity
                bool shouldDraw = (cam.targetBoidIndex == i && showLineVelocity) || showLineVelocityAll;

                velocityArrows[i].gameObject.SetActive(shouldDraw);

                if (shouldDraw) 
                { 
                    DrawArrow(boid.pos, boid.pos + boid.vel, i, velocityArrows[i]);
                }

                // position

                shouldDraw = (cam.targetBoidIndex == i && showLinePosition) || showLinePositionAll;

                positionArrows[i].gameObject.SetActive(shouldDraw);

                if ( shouldDraw) { 
                    DrawArrow(Vector3.zero, boid.pos, i, positionArrows[i]);
                }

            }

            bool isTargetFish = (cam.targetBoidIndex == i);

            if ( showVisualRange && isTargetFish || showVisualRangeAll ) { 
                perceptionSpheres[i].gameObject.SetActive(true);
                perceptionSpheres[i].position = boid.pos;

                float diamater = PerceptionRadius * 2f;
                perceptionSpheres[i].localScale = new Vector3(diamater, diamater, diamater);
            }

            if (isTargetFish && showChunks)
            {
                int fishX = Mathf.FloorToInt(boid.pos.x / cellSize);
                int fishY = Mathf.FloorToInt(boid.pos.y / cellSize);
                int fishZ = Mathf.FloorToInt(boid.pos.z / cellSize);

                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                    {
                        for (int zOffset = -1; zOffset <= 1; zOffset++)
                        {
                            Vector3Int targetCoord = new Vector3Int(fishX + xOffset, fishY + yOffset, fishZ + zOffset);

                            if (activeChunksMap.ContainsKey(targetCoord)) continue;

                            if (chunkPool.Count == 0)
                            {
                                Vector3Int oldestCoord = activeChunkHistory.Dequeue();
                                MeshRenderer oldestCube = activeChunksMap[oldestCoord];
                                
                                activeChunksMap.Remove(oldestCoord);
                                chunkPool.Enqueue(oldestCube); // Put it back in the bin
                            }

                            MeshRenderer newCube = chunkPool.Dequeue();
                            
                            Vector3 chunkCenter = new Vector3(
                                targetCoord.x * cellSize + (cellSize / 2f),
                                targetCoord.y * cellSize + (cellSize / 2f),
                                targetCoord.z * cellSize + (cellSize / 2f)
                            );
                            
                            newCube.transform.position = chunkCenter;
                            newCube.gameObject.SetActive(true);

                            newCube.material.color = new Color(0f, 0.5f, 1f, 0.2f);

                            activeChunksMap.Add(targetCoord, newCube);
                            activeChunkHistory.Enqueue(targetCoord);
                        }
                    }
                }
            }

            if ( showAllObstacleAvoidance || (showObstacleAvoidance && isTargetFish) ) 
            { 
                Vector3 forward = boid.vel.normalized;

                // Cast the exact same sphere as your physics logic
                if (Physics.SphereCast(boid.pos, fishBoundsMargin, forward, out RaycastHit hit, avoidDistance, obstacleLayer))
                {
                    // 1. SHOW THE MARGIN SPHERE
                    marginHitSpheres[i].gameObject.SetActive(true);
                    
                    // Position the sphere exactly where the SphereCast touched the rock
                    marginHitSpheres[i].position = hit.point + (hit.normal * fishBoundsMargin);

                    // 2. DRAW THE ESCAPE FORCE ARROW
                    float urgency = 1.0f - (hit.distance / avoidDistance);
                    float panicMultiplier = urgency * urgency * 5f;
                    Vector3 visualForce = hit.normal * (urgency + panicMultiplier);

                    obstacleArrows[i].gameObject.SetActive(true);
                    DrawArrow(boid.pos, boid.pos + visualForce, i, obstacleArrows[i]); 
                }
                else
                {
                    // Hide both if the water ahead is clear!
                    marginHitSpheres[i].gameObject.SetActive(false);
                    obstacleArrows[i].gameObject.SetActive(false); // FIXED!
                }
            }
            else 
            {
                // Hide both if the visualizer toggle is turned off!
                marginHitSpheres[i].gameObject.SetActive(false);
                obstacleArrows[i].gameObject.SetActive(false); // ADDED!
            }


        }


    }

    private void DrawArrow(Vector3 origin, Vector3 destination, int boidNumber, Transform arrow) 
    { 
        // LineRenderer arrow = velocityArrows[boidNumber];

        // arrow.startColor = col;
        // arrow.endColor = col;

        // arrow.SetPosition(0, origin);
        // arrow.SetPosition(1, destination);

        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if ( distance < 0.001f)  {
            arrow.localScale = Vector3.zero;
            return;
        }

        arrow.position = origin +  (direction / 2f);

        float thickness = 0.1f;
        arrow.localScale = new Vector3(thickness, distance / 2f, thickness);
        arrow.rotation = Quaternion.FromToRotation(Vector3.up, direction);
    }

    private Vector3 SteerTowards(Boid boid, Vector3 targetDirection) { 
        if (targetDirection == Vector3.zero) return Vector3.zero;

        Vector3 currentDir = boid.vel.normalized;
        Vector3 desiredDir = targetDirection.normalized;

        // Check if the target is almost ( ~155 - 180 degrees turn) behind 
        if ( Vector3.Dot(currentDir, desiredDir) <= -0.9f ) 
        {
            Vector3 sideVector = Vector3.Cross( currentDir, Vector3.up);
            
            if ( sideVector == Vector3.zero)
                sideVector = Vector3.right;

            desiredDir = ( desiredDir + sideVector * 0.5f).normalized;
        }

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

    private Vector3 CalculateObstacleAvoidance(Vector3 position, Vector3 velocity)
    {
        Vector3 avoidForce = Vector3.zero;
        
        // Only proceed if the fish is actually moving
        if (velocity.sqrMagnitude < 0.01f) return avoidForce;

        Vector3 forward = velocity.normalized;

        // We use fishBoundsMargin as the radius of the SphereCast.
        // This creates an invisible tube of protection exactly the width of your 3D model.
        if (Physics.SphereCast(position, fishBoundsMargin, forward, out RaycastHit hit, avoidDistance, obstacleLayer))
        {
            // 1. Get the direction away from the wall
            Vector3 awayFromWall = hit.normal;
            
            // 2. Base urgency (0 to 1 based on how close the wall is)
            float urgency = 1.0f - (hit.distance / avoidDistance);
            
            // 3. The Anti-Clipping Multiplier!
            // We square the urgency so the force spikes exponentially the closer it gets.
            // This ensures a smooth turn at a distance, but a violently sharp turn if it's about to crash.
            float panicMultiplier = urgency * urgency * 5f; 
            
            // 4. Apply the force
            avoidForce = awayFromWall * (urgency + panicMultiplier);
        }

        // same with attraction

        return avoidForce;
    }

    private Vector3 CalculateAttractionForce(Vector3 position)
    {
        Vector3 attractForce = Vector3.zero;

        // 1. Detect all attractors in a 360-degree sphere around the fish
        Collider[] attractors = Physics.OverlapSphere(position, PerceptionRadius, attractLayer);

        if (attractors.Length > 0)
        {
            // Find the closest point on the nearest attractor
            Vector3 closestPoint = attractors[0].ClosestPoint(position);
            Vector3 vectorToAttractor = closestPoint - position;
            float distance = vectorToAttractor.magnitude;

            if (distance > 0.001f)
            {
                // Direction toward the attractor
                Vector3 direction = vectorToAttractor / distance;

                // Stronger pull when further away, easing up as it arrives
                float pullStrength = distance / PerceptionRadius;

                attractForce = direction * pullStrength;
            }
        }

        return attractForce;
    }
}