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
    public float positionSmoothTime = 0.3f;
    
    [Header("Camera Rotation & Framing")]
    public float lookAheadDistance = 3f;
    public float rotationSpeed = 3f;
    public float turnRelaxation = 2f;

    [Header("Overview Orbit & Zoom Settings")]
    public Vector3 centerPoint = Vector3.zero;
    public float orbitSensitivity = 3f;
    public float zoomSensitivity = 10f;
    public float minDistance = 5f;
    public float maxDistance = 150f;
    public float orbitSmoothTime = 0.15f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    // Follow Mode State
    private Vector3 currentVelocity;
    private Vector3 smoothedForward = Vector3.forward;

    // Orbit Mode State
    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;
    private float targetDistance = 30f;
    private float currentDistance = 30f;
    private float distanceVelocity;

    void Start()
    {
        InitializeOrbitFromCurrentPosition();
    }

    void LateUpdate()
    {
        if (boidManager == null) return;

        if (isFollowing)
        {
            UpdateFollowMode();
        }
        else
        {
            UpdateOverviewOrbitMode();
        }
    }

    private void UpdateFollowMode()
    {
        Vector3 targetPos = boidManager.getFishPosition(targetBoidIndex);
        Vector3 targetVel = boidManager.getFishSpeed(targetBoidIndex);

        // 1. Calculate relaxed forward direction
        if (targetVel.sqrMagnitude > 0.1f)
        {
            Vector3 actualFishForward = targetVel.normalized;
            smoothedForward = Vector3.Slerp(smoothedForward, actualFishForward, turnRelaxation * Time.deltaTime);
        }

        // 2. Calculate position using relaxed rotation
        Quaternion smoothedRotation = Quaternion.LookRotation(smoothedForward);
        Vector3 desiredPosition = targetPos + (smoothedRotation * offset);

        // 3. Smoothly interpolate position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

        // 4. Look ahead of fish
        Vector3 lookTarget = targetPos + (smoothedForward * lookAheadDistance);
        Vector3 directionToLook = lookTarget - transform.position;

        if (directionToLook.sqrMagnitude > 0.1f)
        {
            Quaternion lookAtRotation = Quaternion.LookRotation(directionToLook);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAtRotation, rotationSpeed * Time.deltaTime);
        }

        // Keep orbit variables synced so switching off follow mode transitions smoothly
        InitializeOrbitFromCurrentPosition();
    }

    private void UpdateOverviewOrbitMode()
    {
        // 1. Mouse Drag for Orbiting (Left or Right Click)
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            targetYaw += Input.GetAxis("Mouse X") * orbitSensitivity;
            targetPitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);
        }

        // 2. Mouse Wheel for Zooming
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetDistance -= scroll * zoomSensitivity;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        // 3. Smooth Orbit Angles and Zoom Distance
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime / Mathf.Max(0.001f, orbitSmoothTime));
        currentPitch = Mathf.LerpAngle(currentPitch, targetPitch, Time.deltaTime / Mathf.Max(0.001f, orbitSmoothTime));
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, orbitSmoothTime);

        // 4. Compute Position & Rotation relative to centerPoint (0,0,0)
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPosition = centerPoint + (rotation * new Vector3(0f, 0f, -currentDistance));

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(centerPoint - transform.position), rotationSpeed * Time.deltaTime);
    }

    private void InitializeOrbitFromCurrentPosition()
    {
        Vector3 dir = transform.position - centerPoint;
        if (dir.sqrMagnitude < 0.001f) dir = new Vector3(0f, 0f, -10f);

        currentDistance = dir.magnitude;
        targetDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        Quaternion rot = Quaternion.LookRotation(-dir.normalized);
        currentYaw = rot.eulerAngles.y;
        currentPitch = rot.eulerAngles.x;

        if (currentPitch > 180f) currentPitch -= 360f;

        targetYaw = currentYaw;
        targetPitch = currentPitch;
    }
}