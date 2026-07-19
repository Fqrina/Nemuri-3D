using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportShrinkEffect : MonoBehaviour
{
    [Header("Phase Durations")]
    [SerializeField] private float phase1Duration = 0.1f;
    [SerializeField] private float phase2Duration = 0.5f;
    [SerializeField] private float phase3Duration = 0.05f;

    private Vector3 originalScale;
    private Coroutine shrinkCoroutine;
    private bool isShrinking;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            StartShrink();
        }
    }

    // Call this function to trigger the shrink teleport effect
    public void StartShrink()
    {
        if (isShrinking)
        {
            return;
        }

        if (shrinkCoroutine != null)
        {
            StopCoroutine(shrinkCoroutine);
        }

        shrinkCoroutine = StartCoroutine(ShrinkRoutine());
    }

    public void ResetScale()
    {
        if (shrinkCoroutine != null)
        {
            StopCoroutine(shrinkCoroutine);
            shrinkCoroutine = null;
        }

        transform.localScale = originalScale;
        isShrinking = false;
    }

    private IEnumerator ShrinkRoutine()
    {
        isShrinking = true;

        // Phase 1: Fast shrink (100% to 90% scale)
        float elapsed = 0f;
        Vector3 startScale = originalScale;
        Vector3 phase1TargetScale = originalScale * 0.9f;

        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / phase1Duration);
            // smooth step for smooth start and transition
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(startScale, phase1TargetScale, smoothT);
            yield return null;
        }
        transform.localScale = phase1TargetScale;

        // Phase 2: Slow shrink (90% to 50% scale)
        elapsed = 0f;
        startScale = transform.localScale;
        Vector3 phase2TargetScale = originalScale * 0.5f;

        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / phase2Duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(startScale, phase2TargetScale, smoothT);
            yield return null;
        }
        transform.localScale = phase2TargetScale;

        // Phase 3: Ultra-fast shrink (50% to 0% scale)
        // uses linear interpolation to prevent slowing down at the end
        elapsed = 0f;
        startScale = transform.localScale;
        Vector3 phase3TargetScale = Vector3.zero;

        while (elapsed < phase3Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / phase3Duration);
            transform.localScale = Vector3.Lerp(startScale, phase3TargetScale, t);
            yield return null;
        }
        transform.localScale = phase3TargetScale;

        isShrinking = false;
        shrinkCoroutine = null;
    }
}
