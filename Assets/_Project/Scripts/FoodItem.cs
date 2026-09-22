using System.Collections;
using UnityEngine;

public class FoodItem : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.4f;
    
    private Vector3 baseScale;
    private Renderer foodRenderer;
    private MaterialPropertyBlock propBlock;
    private Coroutine currentFadeRoutine;

    public bool IsAvailable { get; private set; }

    private void Awake()
    {
        baseScale = transform.localScale;
        foodRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    public void Spawn(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
        IsAvailable = true;

        if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
        currentFadeRoutine = StartCoroutine(AnimateFade(0f, 1f, fadeDuration, null));
    }

    public void Consume(System.Action onConsumed)
    {
        if (!IsAvailable) return;
        IsAvailable = false; // Immediately disable targeting while playing death fade

        if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
        currentFadeRoutine = StartCoroutine(AnimateFade(1f, 0f, fadeDuration * 0.75f, () =>
        {
            gameObject.SetActive(false);
            onConsumed?.Invoke();
        }));
    }

    private IEnumerator AnimateFade(float startAlpha, float endAlpha, float duration, System.Action onComplete)
    {
        float time = 0f;
        Vector3 startScale = baseScale * startAlpha;
        Vector3 targetScale = baseScale * endAlpha;

        while (time < duration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / duration);

            // Scale interpolation
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

            // Material Alpha interpolation via MaterialPropertyBlock
            if (foodRenderer != null)
            {
                foodRenderer.GetPropertyBlock(propBlock);
                Color c = foodRenderer.sharedMaterial.color;
                c.a = Mathf.Lerp(startAlpha, endAlpha, progress);
                propBlock.SetColor("_Color", c);
                foodRenderer.SetPropertyBlock(propBlock);
            }

            yield return null;
        }

        transform.localScale = targetScale;
        onComplete?.Invoke();
    }
}