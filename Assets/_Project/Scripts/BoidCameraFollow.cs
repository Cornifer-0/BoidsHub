using UnityEngine;

public class BoidCameraFollow : MonoBehaviour
{
    [Header("References")]
    public BoidManager boidManager;

    [Header("Follow Settings")]
    public bool isFollowing = false;
    public int targetBoidIndex = 0;
    [Tooltip("When enabled, the camera turns automatically with the fish's forward heading.")]
    public bool followFishRotation = true;
    public float turnRelaxation = 3f; // Higher values make camera follow fish turns faster

    [Header("Controls & Inversion")]
    public bool invertMouseX = false;
    public bool invertMouseY = false;
    public float orbitSensitivity = 3f;
    public float zoomSensitivity = 10f;

    [Header("Orbit Bounds")]
    public Vector3 centerPoint = Vector3.zero;
    public float minDistance = 2f;
    public float maxDistance = 150f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.2f;
    public float rotationSpeed = 8f;
    public float orbitSmoothTime = 0.1f;

    // Internal State
    private Vector3 currentVelocity;
    private Vector3 smoothedFishForward = Vector3.forward;

    private float targetYaw = 0f;
    private float targetPitch = 15f; // Slight overhead angle by default
    private float currentYaw = 0f;
    private float currentPitch = 15f;

    private float targetDistance = 10f;
    private float currentDistance = 10f;
    private float distanceVelocity;

    private bool lastIsFollowing;
    private int lastTargetBoidIndex;

    void Start()
    {
        lastIsFollowing = isFollowing;
        lastTargetBoidIndex = targetBoidIndex;

        if (isFollowing && boidManager != null)
        {
            Vector3 fishVel = boidManager.getFishSpeed(targetBoidIndex);
            if (fishVel.sqrMagnitude > 0.01f) smoothedFishForward = fishVel.normalized;
        }
    }

    void LateUpdate()
    {
        if (boidManager == null) return;

        // Reset angles on mode/target switch to avoid camera snaps
        if (isFollowing != lastIsFollowing || (isFollowing && targetBoidIndex != lastTargetBoidIndex))
        {
            OnTargetOrModeChanged();
            lastIsFollowing = isFollowing;
            lastTargetBoidIndex = targetBoidIndex;
        }

        ProcessInput();

        if (isFollowing)
        {
            UpdateFollowMode();
        }
        else
        {
            UpdateOverviewMode();
        }
    }

    private void ProcessInput()
    {
        // Mouse Drag Orbit Input
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * orbitSensitivity * (invertMouseX ? -1f : 1f);
            float mouseY = Input.GetAxis("Mouse Y") * orbitSensitivity * (invertMouseY ? -1f : 1f);

            targetYaw += mouseX;
            targetPitch -= mouseY; // Standard orbit pitch (dragging up orbits higher)
            targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);
        }

        // Mouse Wheel Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetDistance -= scroll * zoomSensitivity;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    private void UpdateFollowMode()
    {
        Vector3 fishPos = boidManager.getFishPosition(targetBoidIndex);
        Vector3 fishVel = boidManager.getFishSpeed(targetBoidIndex);

        Quaternion baseRotation = Quaternion.identity;

        // 1. Calculate base rotation matching the fish's velocity vector
        if (followFishRotation)
        {
            if (fishVel.sqrMagnitude > 0.01f)
            {
                smoothedFishForward = Vector3.Slerp(smoothedFishForward, fishVel.normalized, turnRelaxation * Time.deltaTime);
            }

            // Keep world-up alignment to prevent nauseating camera roll when fish pitches straight up/down
            Vector3 upDir = Mathf.Abs(Vector3.Dot(smoothedFishForward, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            baseRotation = Quaternion.LookRotation(smoothedFishForward, upDir);
        }

        // 2. Apply combined orbit transform around fish position
        ApplyOrbitTransformation(fishPos, baseRotation);
    }

    private void UpdateOverviewMode()
    {
        // Overview orbits around static centerPoint with world identity heading
        ApplyOrbitTransformation(centerPoint, Quaternion.identity);
    }

    private void ApplyOrbitTransformation(Vector3 pivotPoint, Quaternion baseRotation)
    {
        // Smooth local yaw, pitch, and zoom distance
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime / Mathf.Max(0.001f, orbitSmoothTime));
        currentPitch = Mathf.LerpAngle(currentPitch, targetPitch, Time.deltaTime / Mathf.Max(0.001f, orbitSmoothTime));
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, orbitSmoothTime);

        // Combine fish heading base rotation with user pitch/yaw mouse offsets
        Quaternion localOrbit = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Quaternion finalRotation = baseRotation * localOrbit;

        // Position camera behind pivot point relative to final rotation
        Vector3 desiredPosition = pivotPoint - (finalRotation * Vector3.forward * currentDistance);

        // Apply position and rotation synchronously
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
    }

    private void OnTargetOrModeChanged()
    {
        // Reset orbit offsets to frame newly selected fish nicely from behind
        targetYaw = 0f;
        targetPitch = 15f;
        currentYaw = 0f;
        currentPitch = 15f;

        if (isFollowing)
        {
            Vector3 fishVel = boidManager.getFishSpeed(targetBoidIndex);
            if (fishVel.sqrMagnitude > 0.01f) smoothedFishForward = fishVel.normalized;
        }
    }
}