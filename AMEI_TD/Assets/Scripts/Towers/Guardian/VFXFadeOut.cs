using UnityEngine;

/// <summary>
/// Automatically shrinks a VFX object to zero scale after a visible duration.
/// Attach to any VFX that should scale out and disappear.
/// 
/// Timeline:
/// 1. OnEnable: Reset to original scale, start timer
/// 2. Wait visibleDuration seconds at full scale
/// 3. Shrink to zero over shrinkDuration seconds
/// 4. OnDisable: Reset scale for pool reuse
/// </summary>
public class VFXScaleOut : MonoBehaviour
{
    [SerializeField] private float visibleDuration = 1.5f;  // How long to stay at full scale
    [SerializeField] private float shrinkDuration = 1f;     // How long the shrink animation takes

    private Vector3 originalScale;  // Cached scale from OnEnable
    private float timer;            // Time since OnEnable

    /// <summary>
    /// Called when object is enabled (e.g., retrieved from pool).
    /// Caches original scale and resets timer.
    /// </summary>
    private void OnEnable()
    {
        originalScale = transform.localScale;
        timer = 0f;
    }

    /// <summary>
    /// Handles the shrink animation after visible duration expires.
    /// </summary>
    private void Update()
    {
        timer += Time.deltaTime;

        // After visible duration, start shrinking
        if (timer > visibleDuration)
        {
            // Calculate shrink progress (0 to 1)
            float shrinkProgress = (timer - visibleDuration) / shrinkDuration;
            
            // Invert progress to get scale (1 to 0)
            float scale = 1f - Mathf.Clamp01(shrinkProgress);
            
            transform.localScale = originalScale * scale;
        }
    }

    /// <summary>
    /// Called when object is disabled (e.g., returned to pool).
    /// Restores original scale for next use.
    /// </summary>
    private void OnDisable()
    {
        transform.localScale = originalScale;
    }
}