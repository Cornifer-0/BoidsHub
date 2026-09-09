using UnityEngine;
using System.Collections.Generic;
using System.Collections;



public enum Species : byte { Fish, Calamari, Crocodile }

public struct Boid 
{ 
    public Vector3 pos;
    public Vector3 vel;
    
    public Species species; 

    public BoidTrait trait;

    public float health;
    public bool isDead;
    public bool enabled;
};


// For the evolution process
public struct BoidTrait { 

    public float size;
    public float maxHealth;
    public float contactDamage;

    public float cohesionWeight;
    public float separationWeight;
    public float alignmentWeight;

    public float maxSpeed;
    public float maxForce;

    public float huntWeight;
    public float huntRadius;
    public float fleeWeight;
    public float fleeRadius;

    // + others related to evolution
}

// Inetractions

public enum Interaction { Ignore, Flock, Hunt, Flee }


[System.Serializable]
public struct SpeciesSettings{
    public string name;

    public GameObject preFab;


    public float bodyRadius;
    
    [Header("Stats")]
    public float maxHealth;
    public float contactDamage;

    [Header("Weights")]
    public float cohesionWeight;
    public float separationWeight;
    public float alignmentWeight;

    public float attractionWeight;
    public float avoidWeight;

    [Header("Radii")]
    public float perceptionRadius;
    public float perceptionRadiusSeparation; // Radius for separation, should be lower

    [Header("Limits")]
    public float maxSpeed;
    public float maxForce;

    [Header("Interactions with other species")]
    public float huntWeight;
    public float huntRadius;
    public float fleeWeight;
    public float fleeRadius;
}


public class BoidManager : MonoBehaviour
{
    [Header("Boid Settings")]
    public int MaxNumberOfBoids = 100;
    public int numberOfCalamari = 10;
    public int numberOfCrocodiles = 10;
    public int numberOfFish = 10;

    [Space(2)]
    // 0 = fish, 1 = Calamari, 2=Crocodile ...
    public SpeciesSettings[] speciesSettings;
    [Space(2)]

    public float SpawnRadius = 10f;

    public BoidCameraFollow cam;

    //public float CohesionWeight = 1f;
    //public float SeparationWeight = 1f;
    //public float AlignmentWeight = 1f;

    //public float PerceptionRadius = 10f; // coheison + alignment
    //public float PerceptionRadiusSeparation = 3f;

    //public float MaxSpeed = 10f;
    //public float MaxForce = 5f;
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
    private Transform boundSphere;

    public bool showLineVelocity = false;
    public bool showLineVelocityAll = false;

    public bool showLinePosition = false;
    public bool showLinePositionAll = false;

    public bool showCollisionSphere = false;
    public bool showCollisionSphereAll = false;

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
    private Transform[] collisionSpheres;

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
    //public float avoidWeight = 5f;

    public float attractDistance = 5f;
    //public float attractionWeight = 5f; 

    public float fishBoundsMargin = 1.5f;

    [Header("Avoidance / Attractor Visualizers")]
    public Material obstacleWarningMaterial;
    private Transform[] marginHitSpheres; // one per fish
    private Transform[] obstacleArrows;

    public bool showObstacleAvoidance = false;
    public bool showAllObstacleAvoidance = false;

    [Header("Underwater Current Settings")]
    public bool applyCurrents = false;

    public float currentScale = 0.05f; // Frequency: lower values create large, sweeping currents
    public float currentSpeed = 0.2f;  // Time multiplier: how fast currents shift over time
    public float currentWeight = 2f;   // How strongly the current pushes the fish

    [Header("Current Visualizer (Runtime)")]
    public bool visualizeCurrent = false;
    public int gridRadiusCount = 3; // 3 in each direction = 7x7x7 grid (343 total arrows)
    public float gridStep = 4f;       // Distance between vectors
    public float arrowThickness = 0.08f;

    private Transform[] currentArrows;
    private MeshRenderer[] currentArrowRenderers;


    [Header("Evolution")]
    public bool evolution = true;

    private List<int> candidateParents = new List<int>(128);

    [Header("UI")]
    public Grapher grapher;
    private float sampleTimer = 0f;

    [Header("Animations")]
    private Animator[] boidAnimators;
    private string[] currentEyeState;





    void Start(){


        boids = new Boid[MaxNumberOfBoids];
        boidTransforms = new Transform[MaxNumberOfBoids];

        currentEyeState = new string[MaxNumberOfBoids];
        boidAnimators = new Animator[MaxNumberOfBoids];

        if ( simulationWithArrows ) velocityArrows = new Transform[MaxNumberOfBoids];
        if ( simulationWithArrows ) positionArrows = new Transform[MaxNumberOfBoids];
        if ( simulationWithArrows ) perceptionSpheres = new Transform[MaxNumberOfBoids];
        if ( simulationWithArrows ) marginHitSpheres = new Transform[MaxNumberOfBoids];
        if ( simulationWithArrows ) obstacleArrows = new Transform[MaxNumberOfBoids];
        if ( simulationWithArrows ) collisionSpheres = new  Transform[MaxNumberOfBoids];

        // Generate the 50 cubes for our pool
        float visualSize = cellSize * 1f; // 5% gap

        //Bound Sphere
        GameObject bSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(bSphere.GetComponent<Collider>());
        MeshRenderer bsrend = bSphere.GetComponent<MeshRenderer>();
        bsrend.material = transparentMaterialChunk;
        bSphere.transform.localScale = new Vector3(boundsRadius * 2.0f, boundsRadius * 2.0f, boundsRadius * 2.0f);
        bSphere.SetActive(false);
        boundSphere = bSphere.transform;


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


        for(int i = 0; i < MaxNumberOfBoids; i++) {


            if(numberOfCalamari > 0) {
                boids[i].species = Species.Calamari;
                numberOfCalamari--;
            }else if(numberOfCrocodiles > 0) {
                boids[i].species = Species.Crocodile;
                numberOfCrocodiles--;
            }else if(numberOfFish > 0){
                boids[i].species = Species.Fish;
            }else { 
                //already spawned everone
                boids[i].enabled = false;
                continue;
            }
            boids[i].enabled = true;




            // Ger random pos
            Vector3 spawnPos = GetSafeSpawnPosition(3.0f);
            Vector3 spawnVel = getRandomDirection() * InitialSpeed; // randomDir * initialSpeed

            boids[i].pos = spawnPos;
            boids[i].vel = spawnVel;

            SpeciesSettings settings = speciesSettings[(int)boids[i].species];
            GameObject newBoid = Instantiate(settings.preFab, spawnPos, Quaternion.LookRotation(spawnVel));


            // add their initial trait things
            boids[i].trait.size = 1;
            boids[i].trait.maxHealth = settings.maxHealth;
            boids[i].trait.contactDamage = settings.contactDamage;

            boids[i].trait.cohesionWeight = settings.cohesionWeight;
            boids[i].trait.separationWeight = settings.separationWeight;
            boids[i].trait.alignmentWeight = settings.alignmentWeight;

            boids[i].trait.maxForce = settings.maxForce;
            boids[i].trait.maxSpeed = settings.maxSpeed;

            boids[i].trait.huntWeight = settings.huntWeight;
            boids[i].trait.huntRadius = settings.huntRadius;
            boids[i].trait.fleeWeight = settings.fleeWeight;
            boids[i].trait.fleeRadius = settings.fleeRadius;


            boidTransforms[i] = newBoid.transform;
            //boidTransforms[i].GetComponent<Animator>().Play("Teardrop_L", 1); // layer 1

            boidAnimators[i] = boidTransforms[i].GetChild(0).GetComponent<Animator>();
            currentEyeState[i] = "Eyes_Blink"; 
            boidAnimators[i].Play(currentEyeState[i], 1);


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

                float marginDiameter = fishBoundsMargin * 2f;
                hitSphere.transform.localScale = new Vector3(marginDiameter, marginDiameter, marginDiameter);

                marginHitSpheres[i] = hitSphere.transform;
                marginHitSpheres[i].gameObject.SetActive(false);

                // collisionSpheres

                GameObject colSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(colSphere.GetComponent<Collider>());
                colSphere.GetComponent<MeshRenderer>().material = obstacleWarningMaterial;
                float md = settings.bodyRadius * 2f;
                colSphere.transform.localScale = new Vector3(md, md, md);
                collisionSpheres[i] = colSphere.transform;
                collisionSpheres[i].gameObject.SetActive(false);

            }
        }

        // current vector arrows
        if(simulationWithArrows) {
            int totalArrows = (gridRadiusCount * 2 + 1) * (gridRadiusCount * 2 + 1) * (gridRadiusCount * 2 + 1);
            currentArrows = new Transform[totalArrows];
            currentArrowRenderers = new MeshRenderer[totalArrows];

            for (int i = 0; i < totalArrows; i++)
            {
                GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(cyl.GetComponent<Collider>());
                
                MeshRenderer rend = cyl.GetComponent<MeshRenderer>();
                rend.material = new Material(Shader.Find("Sprites/Default")); // Unlit shader
                
                cyl.SetActive(false);
                currentArrows[i] = cyl.transform;
                currentArrowRenderers[i] = rend;
            }
        }
    }

    private Vector3 getRandomDirection(){
        return Random.onUnitSphere;
    }

    private Vector3 getRandomSpawn(){
        return transform.position + Random.insideUnitSphere * SpawnRadius;
    }

    private Vector3 GetSafeSpawnPosition(float safeRadius, int maxAttempts = 15)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 candidatePos = boundsCenter + (Random.insideUnitSphere * (boundsRadius * 0.8f));
            bool isSafe = true;

            // Check against boids we have already spawned
            for (int i = 0; i < boids.Length; i++) 
            {
                // Skip uninitialized boids at position (0,0,0)
                if (boids[i].pos == Vector3.zero) continue; 

                if (Vector3.Distance(candidatePos, boids[i].pos) < safeRadius)
                {
                    isSafe = false;
                    break; // Too close, try again!
                }
            }

            if (isSafe) return candidatePos;
        }
        
        // Fallback if the space is too crowded
        return boundsCenter + (Random.insideUnitSphere * boundsRadius); 
    }

    public Vector3 getFishPosition(int i) {
        return boids[i].pos;
    }

    public Vector3 getFishSpeed(int i) { 
        return boids[i].vel;
    }

    void Update(){ 

        //simulation

        UpdateGrid();

        for(int i = 0; i < MaxNumberOfBoids; i++) {
            Boid boid = boids[i];
            SpeciesSettings settings = speciesSettings[(int)boid.species];

            if ( !boid.enabled ) { 
                continue;
            }


            if ( boid.isDead ) { 
                // Apply currents
                Vector3 accelD = Vector3.zero;
                if(applyCurrents) {
                    Vector3 currentForce = GetCurrentForce(boid.pos);
                    accelD += currentForce * currentWeight;
                }

                // Apply physics
                boid.vel = Vector3.ClampMagnitude(boid.vel + accelD * Time.deltaTime, settings.maxSpeed);
                boid.pos += boid.vel * Time.deltaTime;

                boids[i] = boid;

                boidTransforms[i].position = boid.pos;
                //if(boid.vel != Vector3.zero) boidTransforms[i].forward = boid.vel.normalized;

                if ( boid.vel.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(boid.vel.normalized);
                    boidTransforms[i].rotation = Quaternion.RotateTowards(
                        boidTransforms[i].rotation, 
                        targetRotation, 
                        rotationSpeed * Time.deltaTime
                    );
                }
                continue;
            }

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

            Vector3 fleeVector = Vector3.zero;
            Vector3 closestPreyPos = Vector3.zero;
            float closestPreyDist = float.MaxValue;

            int sameSpeciesCount = 0;
            bool foundPrey = false;

            //int neighborCount = 0;

            foreach ( int neighborIdx in neighbors)  {
                Boid neighbor = boids[neighborIdx];

                if ( boid.isDead || neighbor.isDead ) continue;

                float dist = Vector3.Distance(boid.pos, neighbor.pos);
                if ( dist < 0.001f ) dist = 0.001f;

                SpeciesSettings neighborSettings = speciesSettings[(int)neighbor.species];

                float overlapDistance = (settings.bodyRadius + neighborSettings.bodyRadius) - dist;

                if ( overlapDistance > 0 ) {
                    HandleBoidCollision(i, neighborIdx);
                } 

            
                Interaction action = GetInteraction(boid.species, neighbor.species);

                // if(dist > 0 && dist < settings.perceptionRadius) { 
                //     centerOfMass += neighbor.pos;
                //     avgVelocity += neighbor.vel;

                //     if (dist < settings.perceptionRadiusSeparation){
                //         separationVector += (boid.pos - neighbor.pos).normalized / dist;
                //     }
                //     neighborCount++;
                // }

                if ( action == Interaction.Flock ) { 
                    //just flock normally
                    if ( dist < settings.perceptionRadius ) { 
                        centerOfMass += neighbor.pos;
                        avgVelocity += neighbor.vel;
                        sameSpeciesCount++;
                    }
                    if(dist < settings.perceptionRadiusSeparation) { 
                        //for separation
                        separationVector += (boid.pos - neighbor.pos).normalized / dist;
                    }
                }else if(action == Interaction.Flee && dist < settings.fleeRadius) { 
                    // exponentially get away, having a hunter very close should scary you
                    // exponentially more
                    fleeVector += (boid.pos - neighbor.pos).normalized / (dist * dist);
                }else if( action == Interaction.Hunt && dist < settings.huntRadius) { 
                    if(dist < closestPreyDist) { 
                        closestPreyDist = dist;
                        closestPreyPos = neighbor.pos;
                        foundPrey = true;
                    }
                }

                // Don't clip into other species, even if u are ignoring them
                if ( action != Interaction.Flock && dist < settings.perceptionRadiusSeparation ){
                    separationVector += (boid.pos - neighbor.pos).normalized / dist;
                }

                


            }
            Vector3 accel = Vector3.zero;

            if ( sameSpeciesCount > 0 ) { 
                centerOfMass /= sameSpeciesCount;
                avgVelocity /= sameSpeciesCount;

                Vector3 cohesionForce = SteerTowards(boid, centerOfMass - boid.pos);
                Vector3 alignmentForce = SteerTowards(boid, avgVelocity);

                accel += cohesionForce * boid.trait.cohesionWeight;
                accel += alignmentForce * boid.trait.alignmentWeight;

            }

            Vector3 separationForce = SteerTowards(boid, separationVector);
            Vector3 avoidance = CalculateObstacleAvoidance(boid.pos, boid.vel);
            Vector3 attraction = CalculateAttractionForce(boid.pos, i);

            accel += separationForce * boid.trait.separationWeight;
            accel += avoidance * settings.avoidWeight + attraction * settings.attractionWeight;

            // also change eye animations depending no what action the boid is taking
            string targetEyeAnimation = "Eyes_Blink";

            if ( fleeVector != Vector3.zero ) { 
                accel += SteerTowards(boid, fleeVector) * settings.fleeWeight;
                targetEyeAnimation = "Eyes_Trauma";
            }

            if ( foundPrey ) { 
                Vector3 vectorToPrey = closestPreyPos - boid.pos;
                accel += SteerTowards(boid, vectorToPrey) * settings.huntWeight;
                targetEyeAnimation = "Eyes_Excited";
            }

            //change animation only if behaviour has changed
            if(currentEyeState[i] != targetEyeAnimation) {
                currentEyeState[i] = targetEyeAnimation;
                boidAnimators[i].Play(targetEyeAnimation, 1);
            }



            accel += CalculateBoundaryDeflection(boid);

            // Apply currents
            if(applyCurrents) {
                Vector3 currentForce = GetCurrentForce(boid.pos);
                accel += currentForce * currentWeight;
            }

            if (boids[i].isDead) continue;

            // Apply physics
            boid.vel = Vector3.ClampMagnitude(boid.vel + accel * Time.deltaTime, settings.maxSpeed);
            boid.pos += boid.vel * Time.deltaTime;

            boids[i] = boid;

            boidTransforms[i].position = boid.pos;
            //if(boid.vel != Vector3.zero) boidTransforms[i].forward = boid.vel.normalized;

            if (boid.vel.sqrMagnitude > 0.01f)
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

                float diamater = settings.perceptionRadius * 2f;
                perceptionSpheres[i].localScale = new Vector3(diamater, diamater, diamater);
            }else{
                perceptionSpheres[i].gameObject.SetActive(false);
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

            // collisionSpheres
            if ( simulationWithArrows && !boid.isDead) { 
                bool shouldDraw = (cam.targetBoidIndex == i && showCollisionSphere) || showCollisionSphereAll;

                collisionSpheres[i].gameObject.SetActive(shouldDraw);
                collisionSpheres[i].position = boid.pos;
            }
        }


        // Current arrows
        if(simulationWithArrows) UpdateCurrentVisualizer();

        // bounds sphere
        if(showBoundRadius) boundSphere.gameObject.SetActive(true);
        else boundSphere.gameObject.SetActive(false);



        // UI 

        sampleTimer += Time.deltaTime;
        if (sampleTimer >= 1.0f)
        {
            sampleTimer = 0f;
            SendGraphData();
        }

    }


    private void SendGraphData()
    {
        if (grapher == null) return;

        float totalSpeed = 0f;
        int activeCount = 0;

        for (int i = 0; i < boids.Length; i++)
        {
            if (!boids[i].isDead)
            {
                totalSpeed += boids[i].vel.magnitude;
                activeCount++;
            }
        }

        float averageSpeed = activeCount > 0 ? (totalSpeed / activeCount) : 0f;

        int f = 0;
        int c = 0;
        int cr = 0;

        for(int i = 0; i < MaxNumberOfBoids; i++) { 
            if (boids[i].species == Species.Fish) f++;
            if (boids[i].species == Species.Calamari) c++;
            if (boids[i].species == Species.Crocodile) cr++; 
        }

        grapher.AddDataPoint(0, f);      // Blue line
        grapher.AddDataPoint(1, c);  // Cyan line
        grapher.AddDataPoint(2, cr); // Red line
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

        SpeciesSettings settings = speciesSettings[(int)boid.species];

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

        Vector3 desiredVelocity = targetDirection.normalized * settings.maxSpeed;
        Vector3 steer = desiredVelocity - boid.vel;
        return Vector3.ClampMagnitude(steer, settings.maxForce);
    }

    private void UpdateGrid(){
        grid.Clear();

        for(int i = 0; i < MaxNumberOfBoids; i++){
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

        if (Physics.SphereCast(position, fishBoundsMargin, forward, out RaycastHit hit, avoidDistance, obstacleLayer))
        {

            Vector3 awayFromWall = hit.normal;


            // If going straight to the obstacle, pick a side ( up ) a slide through the obstacle
            if ( Vector3.Dot(forward, awayFromWall) < -0.9f ) { 
                awayFromWall =  ( awayFromWall + Vector3.up * 0.5f).normalized;
            }

            Vector3 slideDirection = Vector3.ProjectOnPlane(forward, hit.normal).normalized;

            // blend sliding with avoiding it.
            Vector3 escapeDirection = (awayFromWall + slideDirection).normalized;

            float urgency = 1.0f - (hit.distance / avoidDistance);
            
            float panicMultiplier = urgency * urgency * 5f; 
            
            // 4. Apply the force
            avoidForce = escapeDirection * (urgency + panicMultiplier);
        }

        // same with attraction

        return avoidForce;
    }

    private Vector3 CalculateAttractionForce(Vector3 position, int boidIndex)
    {
        SpeciesSettings settings = speciesSettings[(int)boids[boidIndex].species];

        Vector3 attractForce = Vector3.zero;

        // 1. Detect all attractors in a 360-degree sphere around the fish
        Collider[] attractors = Physics.OverlapSphere(position, settings.perceptionRadius, attractLayer);

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
                float pullStrength = distance / settings.perceptionRadius;

                attractForce = direction * pullStrength;
            }
        }

        return attractForce;
    }

    // For the currents
    private Vector3 GetCurrentForce(Vector3 position)
    {
        float time = Time.time * currentSpeed;

        // Sample 2D Perlin noise across offsetting coordinate pairs.
        // Subtract 0.5f and multiply by 2 to map the standard [0,1] noise range to [-1, 1].
        float x = (Mathf.PerlinNoise(position.y * currentScale + 10f, position.z * currentScale + time) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(position.z * currentScale + 20f, position.x * currentScale + time) - 0.5f) * 2f;
        float z = (Mathf.PerlinNoise(position.x * currentScale + 30f, position.y * currentScale + time) - 0.5f) * 2f;

        return new Vector3(x, y, z);
    }

    private void UpdateCurrentVisualizer()
    {
        // Turn off arrows if the toggle is disabled
        if (!visualizeCurrent)
        {
            if (currentArrows != null && currentArrows.Length > 0 && currentArrows[0].gameObject.activeSelf)
            {
                for (int i = 0; i < currentArrows.Length; i++)
                    currentArrows[i].gameObject.SetActive(false);
            }
            return;
        }

        // Target the hero boid position
        Vector3 center = Vector3.zero;
        
        // Snap the grid center to stepped increments so arrows don't slide with the fish
        center.x = Mathf.Floor(center.x / gridStep) * gridStep;
        center.y = Mathf.Floor(center.y / gridStep) * gridStep;
        center.z = Mathf.Floor(center.z / gridStep) * gridStep;

        int index = 0;
        for (int x = -gridRadiusCount; x <= gridRadiusCount; x++)
        {
            for (int y = -gridRadiusCount; y <= gridRadiusCount; y++)
            {
                for (int z = -gridRadiusCount; z <= gridRadiusCount; z++)
                {
                    Vector3 samplePos = center + new Vector3(x * gridStep, y * gridStep, z * gridStep);
                    Vector3 force = GetCurrentForce(samplePos);
                    float strength = force.magnitude;

                    Transform arrow = currentArrows[index];
                    MeshRenderer rend = currentArrowRenderers[index];

                    if (strength > 0.01f)
                    {
                        arrow.gameObject.SetActive(true);
                        
                        // Rotate cylinder along the noise force direction
                        arrow.rotation = Quaternion.FromToRotation(Vector3.up, force.normalized);
                        
                        // Scale length proportional to noise strength
                        float length = strength * 1.5f;
                        arrow.localScale = new Vector3(arrowThickness, length / 2f, arrowThickness);
                        
                        // Center cylinder along vector direction
                        arrow.position = samplePos + (force.normalized * (length / 2f));

                        // Heatmap transition: Cyan (weak force) to Magenta (strong force)
                        rend.material.color = Color.Lerp(Color.cyan, Color.magenta, strength);
                    }
                    else
                    {
                        arrow.gameObject.SetActive(false);
                    }

                    index++;
                }
            }
        }
    }

    public Interaction GetInteraction(Species me, Species other) { 
        if ( me == other ) return Interaction.Ignore;

        if ( me == Species.Crocodile && other != Species.Crocodile ) return Interaction.Hunt;
        if ( me != Species.Crocodile && other == Species.Crocodile ) return Interaction.Flee;

        return Interaction.Ignore;
    }

    private void HandleBoidCollision(int indexA, int indexB) { 
        Boid boidA = boids[indexA];
        Boid boidB = boids[indexB];

        Interaction relation = GetInteraction(boidA.species, boidB.species);

        switch (relation)
        {
            case Interaction.Hunt:
                ExecutePredation(indexA, indexB);
                break;

            case Interaction.Flee:
                ExecutePredation(indexB, indexA);
                break;

            case Interaction.Flock:
                ApplyImpactDamage(indexA, indexB);
                break;

            case Interaction.Ignore:
                ApplyImpactDamage(indexA, indexB);
                break;
        }
    }

    private void ExecutePredation(int predatorIdx, int preyIdx)
    {
        Boid prey = boids[preyIdx];
        prey.isDead = true;
        boids[preyIdx] = prey;

        boidAnimators[predatorIdx].SetTrigger("Attack");

        Boid predator = boids[predatorIdx];
        predator.health = Mathf.Min(predator.health + 25f, speciesSettings[(int)predator.species].maxHealth);
        boids[predatorIdx] = predator;

        KillBoid(preyIdx);
    }

    private void ApplyImpactDamage(int indexA, int indexB)
    {
        Boid a = boids[indexA];
        Boid b = boids[indexB];

        float impactSpeed = (a.vel - b.vel).magnitude;

        if (impactSpeed > 2f) 
        {
            a.health -= speciesSettings[(int)b.species].contactDamage;
            b.health -= speciesSettings[(int)a.species].contactDamage;

            boids[indexA] = a;
            boids[indexB] = b;

            if (a.health <= 0) KillBoid(indexA);
            if (b.health <= 0) KillBoid(indexB);
        }
    }

    // Respawns a boid to a random position;
    private void RespawnBoid(int index)
    {
        Boid boid = boids[index];
        SpeciesSettings settings = speciesSettings[(int)boid.species];

        boid.isDead = false;
        boid.health = settings.maxHealth;

        boid.pos = boundsCenter + (Random.insideUnitSphere * (boundsRadius * 0.8f));

        boid.vel = Random.onUnitSphere * settings.maxSpeed;

        boids[index] = boid;
        boidTransforms[index].position = boid.pos;
    }

    // Just kill the boid
    private void KillBoid(int index)
    {
        Boid boid = boids[index];

        boid.isDead = true;
        boid.health = 0f;
        
        // Slow momentum down slightly, for a lifeless drift
        boid.vel *= 0.4f; 
        
        boids[index] = boid;

        boidAnimators[index].SetTrigger("Die"); 
        boidAnimators[index].Play("Eyes_Dead", 1);
        currentEyeState[index] = "Eyes_Dead";

        velocityArrows[index].gameObject.SetActive(false);
        positionArrows[index].gameObject.SetActive(false);

        StartCoroutine(DisableThisBoid(index)); // disable in 10s
    }

    private IEnumerator DisableThisBoid(int boid) 
    {
        
        Transform fishTransform = boidTransforms[boid];
        Vector3 originalScale = fishTransform.localScale;
        float timer = 0f;
        float fadeDuration = 3f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            fishTransform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);

            yield return null;
        }
        
        yield return new WaitForSeconds(3f);
        
        //boids[boid].enabled = false;
        //boidTransforms[boid].gameObject.SetActive(false);

        // Instead of disabling it, spawn another with evolved traits.


        //yield return new WaitForSeconds(5f);

        if (evolution) { 
            respawn(boid);
        }
    }

    private void selectRandomParents(int child, out int father, out int mother) { 
        
        candidateParents.Clear();
        Species bs = boids[child].species;


        for(int i = 0; i < MaxNumberOfBoids; i++) { 
            Boid b = boids[i];
            Species s = b.species;
            if(s == bs && i != child && !boids[i].isDead) {
                candidateParents.Add(i);
            }
        }

        Debug.Log("I see this many parents: " + candidateParents.Count);

        if(candidateParents.Count < 2 ) {
            father = -1;
            mother = -1;   
            return;
        }
        father = candidateParents[Random.Range(0, candidateParents.Count)];
        
        do {
            mother = candidateParents[Random.Range(0, candidateParents.Count)];
        }while ( father == mother );
    }

    // Generates a Gaussian random number centered at 0
    private float GetGaussian(float standardDeviation)
    {
        float u1 = 1.0f - UnityEngine.Random.value;
        float u2 = 1.0f - UnityEngine.Random.value;
        // Box-Muller transform
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2); 
        return standardDeviation * randStdNormal;
    }

    private BoidTrait mutateTrait(int father, int mother) {
        BoidTrait mutatedTrait;
        BoidTrait fatherTrait = boids[father].trait;
        BoidTrait motherTrait = boids[mother].trait;

        float sd = 0.05f;

        mutatedTrait.size = ((fatherTrait.size + motherTrait.size) / 2.0f ) * ( 1f + GetGaussian(sd));
        mutatedTrait.maxHealth = ((fatherTrait.maxHealth + motherTrait.maxHealth) / 2.0f ) * ( 1f + GetGaussian(sd));
        mutatedTrait.contactDamage = ((fatherTrait.contactDamage + motherTrait.contactDamage) / 2.0f ) * ( 1f + GetGaussian(sd));

        mutatedTrait.cohesionWeight = ((fatherTrait.cohesionWeight + motherTrait.cohesionWeight) / 2.0f ) * ( 1f + GetGaussian(sd));
        mutatedTrait.separationWeight = ((fatherTrait.separationWeight + motherTrait.separationWeight) / 2.0f ) * ( 1f + GetGaussian(sd));
        mutatedTrait.alignmentWeight = ((fatherTrait.alignmentWeight + motherTrait.alignmentWeight) / 2.0f ) * ( 1f + GetGaussian(sd));

        mutatedTrait.maxForce = ((fatherTrait.maxForce + motherTrait.maxForce) / 2.0f ) * ( 1f + GetGaussian(sd));
        mutatedTrait.maxSpeed = ((fatherTrait.maxSpeed + motherTrait.maxSpeed) / 2.0f ) * ( 1f + GetGaussian(sd));

        mutatedTrait.huntWeight = ((fatherTrait.huntWeight + motherTrait.huntWeight) / 2.0f ) * ( 1f + GetGaussian(sd));
        mutatedTrait.huntRadius = ((fatherTrait.huntRadius + motherTrait.huntRadius) / 2.0f ) * ( 1f + GetGaussian(sd));
        mutatedTrait.fleeWeight = ((fatherTrait.fleeWeight + motherTrait.fleeWeight) / 2.0f ) * ( 1f + GetGaussian(sd));
        mutatedTrait.fleeRadius = ((fatherTrait.fleeRadius + motherTrait.fleeRadius) / 2.0f ) * ( 1f + GetGaussian(sd));

        return mutatedTrait;

    }

    // Respawns a boid with the traits slightly mutaed from two randomly selected parents
    private void respawn(int boid) {
        
        int father = -1;
        int mother = -1;

        // select random parents
        selectRandomParents(boid, out father, out mother);

        Debug.Log("Trying to ");
        if (father == -1 || mother == -1) return; // Can't spawn new one ( extinct species )

        BoidTrait evolvedTrait = mutateTrait(father, mother);

        boids[boid].trait = evolvedTrait;
        boids[boid].isDead = false;
        boids[boid].health = evolvedTrait.maxHealth;
        boids[boid].enabled = true;

        boidTransforms[boid].gameObject.SetActive(true);
        boidTransforms[boid].localScale = new Vector3(evolvedTrait.size, evolvedTrait.size, evolvedTrait.size);

        //boidAnimators[index].SetTrigger("Die"); 
        boidAnimators[boid].Play("Eyes_Blink", 1);
        currentEyeState[boid] = "Eyes_Blink";

        boidAnimators[boid].Play("Swim");

        //velocityArrows[boid].gameObject.SetActive(false);
        //positionArrows[boid].gameObject.SetActive(false);

        Debug.Log("Mutated and spawned");


    }


    // With tangential project on plane, this way the boid doesn't bounce, but smoothly work around the border
    private Vector3 CalculateBoundaryDeflection(Boid boid)
    {
        Vector3 offsetToCenter = boundsCenter - boid.pos;
        float distFromCenter = offsetToCenter.magnitude;

        if (distFromCenter > boundsRadius)
        {
            float excessDistance = distFromCenter - boundsRadius;
            
            // The direction pushing back toward the center of the map
            Vector3 inwardNormal = offsetToCenter / distFromCenter; 
            Vector3 forward = boid.vel.normalized;

            // Prevent a deadlock if the fish is swimming perfectly straight out
            if (Vector3.Dot(forward, inwardNormal) < -0.9f)
            {
                inwardNormal = (inwardNormal + Vector3.up * 0.5f).normalized;
            }

            // Project the forward velocity to slide ALONG the spherical boundary
            Vector3 slideDirection = Vector3.ProjectOnPlane(forward, inwardNormal).normalized;
            
            // Blend sliding along the curve with a push back inside
            Vector3 curveDirection = (slideDirection + (inwardNormal * 0.5f)).normalized;

            // Return the force, scaling intensity by how far out they drifted
            return SteerTowards(boid, curveDirection) * (excessDistance * boundaryWeight);
        }

        return Vector3.zero;
    }
}