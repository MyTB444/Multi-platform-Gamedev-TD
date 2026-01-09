using UnityEngine;
using TMPro;

/// <summary>
/// Animated floating damage number that appears when enemies take damage.
/// Features: pop-in scale animation, upward float, fade-out, and color-coding
/// based on element effectiveness (super effective, not very effective, immune).
/// </summary>
public class DamageNumber : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float floatSpeed = 1f;          // Units per second upward movement
    [SerializeField] private float fadeDelay = 0.3f;         // Seconds before fade begins
    [SerializeField] private float fadeDuration = 0.5f;      // How long the fade takes
    [SerializeField] private float scalePopSize = 1.3f;      // Initial "pop" scale multiplier
    [SerializeField] private float scalePopDuration = 0.15f; // How long the pop animation lasts
    
    [Header("Colors")]
    [SerializeField] private Color superEffectiveColor = new Color(0.2f, 0.8f, 0.2f);  // Green - bonus damage
    [SerializeField] private Color normalColor = Color.white;                            // White - standard damage
    [SerializeField] private Color notVeryEffectiveColor = new Color(0.8f, 0.4f, 0.4f); // Red - reduced damage
    [SerializeField] private Color immuneColor = new Color(0.5f, 0.5f, 0.5f);           // Grey - no damage
    
    private TextMeshPro textMesh;
    private float spawnTime;       // Time.time when this number was spawned
    private Color startColor;      // Cached color for fade calculations
    private Vector3 baseScale;     // Original scale to return to after pop
    private Camera mainCamera;
    
    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        mainCamera = Camera.main;
        baseScale = transform.localScale;
    }
    
    /// <summary>
    /// Initializes the damage number with value and effectiveness indicators.
    /// Called by DamageNumberSpawner after getting from object pool.
    /// </summary>
    /// <param name="damage">The damage value to display</param>
    /// <param name="isSuperEffective">True if attacker had type advantage (2x damage)</param>
    /// <param name="isNotVeryEffective">True if attacker had type disadvantage (0.5x damage)</param>
    /// <param name="isImmune">True if defender is immune to this element (0 damage)</param>
    public void Setup(float damage, bool isSuperEffective, bool isNotVeryEffective, bool isImmune)
    {
        spawnTime = Time.time;
    
        // ===== SET DISPLAY TEXT =====
        // Arrows indicate effectiveness: ↑ for super effective, ↓ for not very effective
        if (isImmune)
        {
            textMesh.text = "IMMUNE";
        }
        else if (isSuperEffective)
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString() + "↑";
        }
        else if (isNotVeryEffective)
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString() + "↓";
        }
        else
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString();
        }
    
        // ===== SET COLOR BASED ON EFFECTIVENESS =====
        // Priority: Immune > Super Effective > Not Very Effective > Normal
        if (isImmune)
        {
            startColor = immuneColor;
        }
        else if (isSuperEffective)
        {
            startColor = superEffectiveColor;
        }
        else if (isNotVeryEffective)
        {
            startColor = notVeryEffectiveColor;
        }
        else
        {
            startColor = normalColor;
        }
    
        textMesh.color = startColor;
    
        // Start larger for "pop" effect - will shrink to normal in Update
        transform.localScale = baseScale * scalePopSize;
    }
    
    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        
        // ===== FLOAT UPWARD =====
        // Constant upward movement throughout lifetime
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        
        // ===== BILLBOARD EFFECT =====
        // Always face the camera so text is readable from any angle
        if (mainCamera != null)
        {
            transform.forward = mainCamera.transform.forward;
        }
        
        // ===== SCALE POP ANIMATION =====
        // Starts large (scalePopSize) and shrinks to normal (1.0) over scalePopDuration
        // Creates a "punch" effect that draws attention to the number
        if (elapsed < scalePopDuration)
        {
            float t = elapsed / scalePopDuration;  // 0 to 1 over duration
            float scale = Mathf.Lerp(scalePopSize, 1f, t);
            transform.localScale = baseScale * scale;
        }
        else
        {
            transform.localScale = baseScale;
        }
        
        // ===== FADE OUT =====
        // After fadeDelay, alpha decreases linearly over fadeDuration
        // Returns to pool when fully transparent
        if (elapsed > fadeDelay)
        {
            float fadeElapsed = elapsed - fadeDelay;
            float alpha = 1f - (fadeElapsed / fadeDuration);

            // Fully faded - return to object pool for reuse
            if (alpha <= 0f)
            {
                ObjectPooling.instance.Return(gameObject);
                return;
            }

            // Apply faded color while preserving RGB
            Color fadedColor = startColor;
            fadedColor.a = alpha;
            textMesh.color = fadedColor;
        }
    }

    /// <summary>
    /// Reset state when retrieved from object pool.
    /// Ensures clean state for reuse.
    /// </summary>
    private void OnEnable()
    {
        if (textMesh != null)
        {
            textMesh.color = normalColor;
        }
        transform.localScale = baseScale;
    }
}