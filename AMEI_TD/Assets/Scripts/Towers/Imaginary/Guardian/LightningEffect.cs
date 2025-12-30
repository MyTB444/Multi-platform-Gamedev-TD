using UnityEngine;

public class LightningEffect : MonoBehaviour
{
    [Header("Lightning Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LineRenderer coreLineRenderer;
    [SerializeField] private int segments = 15;
    [SerializeField] private float jaggedness = 2f;
    [SerializeField] private float animationSpeed = 40f;

    [Header("Width Settings")]
    [SerializeField] private float outerStartWidth = 0.4f;
    [SerializeField] private float outerEndWidth = 0.15f;
    [SerializeField] private float coreStartWidth = 0.15f;
    [SerializeField] private float coreEndWidth = 0.05f;

    [Header("Color Settings")]
    [SerializeField] private Color colorA = new Color(0.2f, 0.5f, 1f, 1f);  // Light blue
    [SerializeField] private Color colorB = new Color(0.1f, 0.3f, 0.9f, 1f); // Deeper blue
    [SerializeField] private Color coreColor = Color.white;
    [SerializeField] private float colorOscillateSpeed = 8f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.1f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Glow Effect")]
    [SerializeField] private Light lightSource;
    [SerializeField] private float maxIntensity = 5f;

    private Vector3 startPoint;
    private Vector3 endPoint;
    private float currentIntensity;
    private bool isActive = false;
    private float lifetime;
    private float maxLifetime = 1f;
    private float colorTimer;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lightSource == null)
            lightSource = GetComponentInChildren<Light>();
    }

    private void Update()
    {
        if (!isActive) return;

        AnimateLightning();
        UpdateColors();
        UpdateFade();
    }

    public void SetupLightning(Vector3 start, Vector3 end)
    {
        startPoint = start;
        endPoint = end;
        isActive = true;
        lifetime = maxLifetime;
        currentIntensity = maxIntensity;
        colorTimer = 0f;

        // Set outer line width
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = outerStartWidth;
            lineRenderer.endWidth = outerEndWidth;
        }

        // Set core line width
        if (coreLineRenderer != null)
        {
            coreLineRenderer.startWidth = coreStartWidth;
            coreLineRenderer.endWidth = coreEndWidth;
        }

        if (lightSource != null)
        {
            lightSource.intensity = maxIntensity;
            lightSource.transform.position = end;
            lightSource.range = 10f;
        }

        GenerateLightningPath();
    }

    private void UpdateColors()
    {
        colorTimer += Time.deltaTime * colorOscillateSpeed;

        // Oscillate between the two blue colors
        float t = (Mathf.Sin(colorTimer) + 1f) / 2f;
        Color currentColor = Color.Lerp(colorA, colorB, t);

        // Change material color for outer line
        if (lineRenderer != null && lineRenderer.material != null)
        {
            lineRenderer.material.color = currentColor;
        }

        // Keep core white
        if (coreLineRenderer != null && coreLineRenderer.material != null)
        {
            coreLineRenderer.material.color = coreColor;
        }

        // Match light color
        if (lightSource != null)
        {
            lightSource.color = currentColor;
        }
    }

    private void UpdateFade()
    {
        lifetime -= Time.deltaTime;
    
        float alpha;
        float timeElapsed = maxLifetime - lifetime;

        // Fade in
        if (timeElapsed < fadeInDuration)
        {
            alpha = timeElapsed / fadeInDuration;
        }
        // Fade out
        else if (lifetime < fadeOutDuration)
        {
            alpha = lifetime / fadeOutDuration;
        }
        // Full visibility
        else
        {
            alpha = 1f;
        }

        // Apply alpha to materials
        if (lineRenderer != null && lineRenderer.material != null)
        {
            Color col = lineRenderer.material.color;
            col.a = alpha;
            lineRenderer.material.color = col;
        }

        if (coreLineRenderer != null && coreLineRenderer.material != null)
        {
            Color col = coreLineRenderer.material.color;
            col.a = alpha;
            coreLineRenderer.material.color = col;
        }

        // Fade light intensity
        currentIntensity = maxIntensity * alpha;
        if (lightSource != null)
        {
            lightSource.intensity = currentIntensity * Random.Range(0.8f, 1.2f);
        }
    }

    private void GenerateLightningPath()
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = segments + 1;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(segments, endPoint);

        Vector3 direction = (endPoint - startPoint).normalized;
        float totalDistance = Vector3.Distance(startPoint, endPoint);
        float segmentLength = totalDistance / segments;

        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular == Vector3.zero)
            perpendicular = Vector3.Cross(direction, Vector3.right).normalized;

        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular).normalized;

        // Store positions for core
        Vector3[] positions = new Vector3[segments + 1];
        positions[0] = startPoint;
        positions[segments] = endPoint;

        for (int i = 1; i < segments; i++)
        {
            Vector3 basePosition = startPoint + direction * (segmentLength * i);

            float middleFactor = 1f - Mathf.Abs((float)i / segments - 0.5f) * 2f;
            float offsetStrength = jaggedness * middleFactor;

            float offsetX = Random.Range(-offsetStrength, offsetStrength);
            float offsetY = Random.Range(-offsetStrength, offsetStrength);

            Vector3 offset = perpendicular * offsetX + perpendicular2 * offsetY;
            Vector3 finalPos = basePosition + offset;

            lineRenderer.SetPosition(i, finalPos);
            positions[i] = finalPos;
        }

        // Copy to core line renderer
        if (coreLineRenderer != null)
        {
            coreLineRenderer.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                coreLineRenderer.SetPosition(i, positions[i]);
            }
        }
    }

    private void AnimateLightning()
    {
        if (Random.value < Time.deltaTime * animationSpeed)
        {
            GenerateLightningPath();
        }
    }
}