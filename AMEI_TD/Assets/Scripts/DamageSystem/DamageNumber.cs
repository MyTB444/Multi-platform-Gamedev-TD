using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float fadeDelay = 0.3f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float scalePopSize = 1.3f;
    [SerializeField] private float scalePopDuration = 0.15f;
    
    [Header("Colors")]
    [SerializeField] private Color superEffectiveColor = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color notVeryEffectiveColor = new Color(0.8f, 0.4f, 0.4f); // Red
    [SerializeField] private Color immuneColor = new Color(0.5f, 0.5f, 0.5f); // Grey
    
    private TextMeshPro textMesh;
    private float spawnTime;
    private Color startColor;
    private Vector3 baseScale;
    private Camera mainCamera;
    
    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        mainCamera = Camera.main;
        baseScale = transform.localScale;
    }
    
    public void Setup(float damage, bool isSuperEffective, bool isNotVeryEffective, bool isImmune)
    {
        spawnTime = Time.time;
    
        // Set text
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
    
        // Set color
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
    
        // Start with pop scale
        transform.localScale = baseScale * scalePopSize;
    }
    
    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        
        // Float upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        
        // Face camera
        if (mainCamera != null)
        {
            transform.forward = mainCamera.transform.forward;
        }
        
        // Scale pop animation
        if (elapsed < scalePopDuration)
        {
            float t = elapsed / scalePopDuration;
            float scale = Mathf.Lerp(scalePopSize, 1f, t);
            transform.localScale = baseScale * scale;
        }
        else
        {
            transform.localScale = baseScale;
        }
        
        // Fade out
        if (elapsed > fadeDelay)
        {
            float fadeElapsed = elapsed - fadeDelay;
            float alpha = 1f - (fadeElapsed / fadeDuration);
            
            if (alpha <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            
            Color fadedColor = startColor;
            fadedColor.a = alpha;
            textMesh.color = fadedColor;
        }
    }
}