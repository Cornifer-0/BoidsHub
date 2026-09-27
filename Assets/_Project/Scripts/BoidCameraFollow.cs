using UnityEngine;

public class BoidCameraFollow : MonoBehaviour
{
    [Header("References")]
    public BoidManager boidManager;

    [Header("Follow Settings")]
    public bool isFollowing = false;
    public int targetBoidIndex = 0;

    [Tooltip("If TRUE: Camera automatically rotates with the fish's forward heading.\nIf FALSE: Camera only follows fish position, remaining oriented to world space unless manually rotated via mouse.")]
    public bool followFishRotation = false; 
    public float turnRelaxation = 3f;

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
    private float targetPitch = 15f; 
    private float currentYaw = 0f;
    private float currentPitch = 15f;

    private float targetDistance = 10f;
    private float currentDistance = 10f;
    private float distanceVelocity;

    private bool lastIsFollowing;
    private int lastTargetBoidIndex;
    private bool lastFollowFishRotation;

    void Start()
    {
        lastIsFollowing = isFollowing;
        lastTargetBoidIndex = targetBoidIndex;
        lastFollowFishRotation = followFishRotation;

        if (isFollowing && boidManager != null)
        {
            Vector3 fishVel = boidManager.getFishSpeed(targetBoidIndex);
            if (fishVel.sqrMagnitude > 0.01f) smoothedFishForward = fishVel.normalized;
        }
    }

    void LateUpdate()
    {
        if (boidManager == null) return;

        // Process mouse and keyboard inputs
        ProcessInput();

        // Reset angles on mode, target, or rotation setting switch to prevent camera snaps
        if (isFollowing != lastIsFollowing || 
            (isFollowing && targetBoidIndex != lastTargetBoidIndex) || 
            followFishRotation != lastFollowFishRotation)
        {
            OnTargetOrModeChanged();
            lastIsFollowing = isFollowing;
            lastTargetBoidIndex = targetBoidIndex;
            lastFollowFishRotation = followFishRotation;
        }

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
        // -------------------------------------------------------------
        // KEYBOARD SHORTCUTS
        // -------------------------------------------------------------

        // 1. SPACE BAR: Toggle between Following Target and Center Overview
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isFollowing = !isFollowing;
        }

        // 2. R KEY: Toggle rotation following on/off
        if (Input.GetKeyDown(KeyCode.R))
        {
            followFishRotation = !followFishRotation;
        }

        // 3. ARROW KEYS: Cycle target fish index
        if (boidManager != null && boidManager.MaxNumberOfBoids > 0)
        {
            // Next fish (Right or Up Arrow)
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                targetBoidIndex = (targetBoidIndex + 1) % boidManager.MaxNumberOfBoids;
                isFollowing = true; // Auto-enable follow mode when selecting a new target
            }
            // Previous fish (Left or Down Arrow)
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                targetBoidIndex--;
                if (targetBoidIndex < 0)
                {
                    targetBoidIndex = boidManager.MaxNumberOfBoids - 1;
                }
                isFollowing = true;
            }
        }

        // -------------------------------------------------------------
        // MOUSE CONTROLS
        // -------------------------------------------------------------

        // Mouse Drag Orbit Input (Left or Right Click)
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * orbitSensitivity * (invertMouseX ? -1f : 1f);
            float mouseY = Input.GetAxis("Mouse Y") * orbitSensitivity * (invertMouseY ? -1f : 1f);

            targetYaw += mouseX;
            targetPitch -= mouseY;
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

        if (followFishRotation)
        {
            if (fishVel.sqrMagnitude > 0.01f)
            {
                smoothedFishForward = Vector3.Slerp(smoothedFishForward, fishVel.normalized, turnRelaxation * Time.deltaTime);
            }

            Vector3 upDir = Mathf.Abs(Vector3.Dot(smoothedFishForward, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            baseRotation = Quaternion.LookRotation(smoothedFishForward, upDir);
        }

        ApplyOrbitTransformation(fishPos, baseRotation);
    }

    private void UpdateOverviewMode()
    {
        ApplyOrbitTransformation(centerPoint, Quaternion.identity);
    }

    private void ApplyOrbitTransformation(Vector3 pivotPoint, Quaternion baseRotation)
    {
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime / Mathf.Max(0.001f, orbitSmoothTime));
        currentPitch = Mathf.LerpAngle(currentPitch, targetPitch, Time.deltaTime / Mathf.Max(0.001f, orbitSmoothTime));
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, orbitSmoothTime);

        Quaternion localOrbit = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Quaternion finalRotation = baseRotation * localOrbit;

        Vector3 desiredPosition = pivotPoint - (finalRotation * Vector3.forward * currentDistance);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
    }

    private void OnTargetOrModeChanged()
    {
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