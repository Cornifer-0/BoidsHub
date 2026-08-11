using UnityEngine;

public class BoidCameraFollow : MonoBehaviour
{
    [Header("References")]
    public BoidManager boidManager;

    [Header("Follow Settings")]
    public bool isFollowing = false;
    public int targetBoidIndex = 0;

    [Header("Camera Offset & Motion")]
    public Vector3 offset = new Vector3(0f, 2f, -5f); // Position relative to fish
    public float positionSmoothTime = 0.3f; // Increased slightly for a more relaxed follow
    
    [Header("Camera Rotation & Framing")]
    public float lookAheadDistance = 3f; // How far ahead of the fish the camera looks
    public float rotationSpeed = 3f; // Lowered to make panning smoother
    public float turnRelaxation = 2f; // How slowly the camera offset reacts to fish turns

    private Vector3 currentVelocity;
    private Vector3 smoothedForward = Vector3.forward;

    void LateUpdate()
    {
        if (!isFollowing || boidManager == null) return;

        Vector3 targetPos = boidManager.getFishPosition(targetBoidIndex);
        Vector3 targetVel = boidManager.getFishSpeed(targetBoidIndex);

        // 1. Calculate a RELAXED forward direction.
        // Instead of snapping to the fish's exact velocity, we slowly rotate a virtual "forward" vector.
        // This prevents the camera from whipping around if the fish does a sharp 180-degree turn.
        if (targetVel.sqrMagnitude > 0.1f)
        {
            Vector3 actualFishForward = targetVel.normalized;
            smoothedForward = Vector3.Slerp(smoothedForward, actualFishForward, turnRelaxation * Time.deltaTime);
        }

        // 2. Calculate offset position using the RELAXED rotation, not the exact fish rotation
        Quaternion smoothedRotation = Quaternion.LookRotation(smoothedForward);
        Vector3 desiredPosition = targetPos + (smoothedRotation * offset);

        // 3. Smoothly interpolate position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

        // 4. Calculate the Look Target (Framing)
        // Instead of looking at the fish (targetPos), look at a point ahead of the fish
        Vector3 lookTarget = targetPos + (smoothedForward * lookAheadDistance);
        
        // 5. Smoothly rotate camera to look at the framed target
        Vector3 directionToLook = lookTarget - transform.position;
        if (directionToLook.sqrMagnitude > 0.1f)
        {
            Quaternion lookAtRotation = Quaternion.LookRotation(directionToLook);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAtRotation, rotationSpeed * Time.deltaTime);
        }
    }
}