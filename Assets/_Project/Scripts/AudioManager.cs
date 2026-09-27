using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientAudioSource; // Set Loop = true in Inspector
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip swimmingLoopClip;
    [SerializeField] private AudioClip eatClip;
    [SerializeField] private AudioClip collisionClip;

    [Header("Proximity Settings")]
    [SerializeField] private float maxProximityDistance = 40f; // Max distance where swimming is audible
    [SerializeField] private float maxAmbientVolume = 0.8f;

    [Header("SFX Performance Rate Limit")]
    [SerializeField] private float collisionCooldown = 0.06f; // Min time between collision sounds
    private float lastCollisionTime;

    private Transform mainCameraTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        // Initialize Ambient Loop
        if (ambientAudioSource != null && swimmingLoopClip != null)
        {
            ambientAudioSource.clip = swimmingLoopClip;
            ambientAudioSource.loop = true;
            ambientAudioSource.volume = 0f;
            ambientAudioSource.Play();
        }
    }

    // Call from BoidManager Update to fade volume based on nearest fish
    public void UpdateSwimmingAmbient(Vector3 closestFishPosition)
    {
        if (ambientAudioSource == null || mainCameraTransform == null) return;

        float distance = Vector3.Distance(mainCameraTransform.position, closestFishPosition);

        // Volume scales from 1.0 (close) to 0.0 (farther than maxProximityDistance)
        float targetVolume = Mathf.Clamp01(1f - (distance  / maxProximityDistance)) * maxAmbientVolume;

        // Smooth volume transitions
        ambientAudioSource.volume = Mathf.Lerp(ambientAudioSource.volume, targetVolume, Time.deltaTime * 4f);
    }

    public void PlayEatSound()
    {
        if (sfxAudioSource == null || eatClip == null) return;

        // Vary pitch slightly so repetitive eats sound natural
        sfxAudioSource.pitch = Random.Range(0.9f, 1.15f);
        sfxAudioSource.PlayOneShot(eatClip, 0.6f);
    }

    public void PlayCollisionSound()
    {
        if (sfxAudioSource == null || collisionClip == null) return;

        // Cooldown prevents audio clipping when 30 fish collide simultaneously
        if (Time.time - lastCollisionTime < collisionCooldown) return;
        lastCollisionTime = Time.time;

        sfxAudioSource.pitch = Random.Range(0.85f, 1.15f);
        sfxAudioSource.PlayOneShot(collisionClip, 0.35f);
    }
}