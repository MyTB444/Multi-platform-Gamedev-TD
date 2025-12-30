using UnityEngine;

public class LightningEffect : MonoBehaviour
{
    [Header("Lightning Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int segments = 10;
    [SerializeField] private float jaggedness = 0.5f;
    [SerializeField] private float animationSpeed = 20f;

    [Header("Glow Effect")]
    [SerializeField] private Light lightSource;
    [SerializeField] private float maxIntensity = 3f;
    [SerializeField] private float fadeSpeed = 5f;

    private Vector3 startPoint;
    private Vector3 endPoint;
    private float currentIntensity;
    private bool isActive = false;
    private float lifetime;
    private float maxLifetime = 1f;
    private Color startColor;
    private Color endColor;
    
    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lightSource == null)
        {
            lightSource = GetComponentInChildren<Light>();
        }
    }

    private void Update()
    {
        if (!isActive) return;

        AnimateLightning();
        FadeLight();
        FadeLightning();
    }

    public void SetupLightning(Vector3 start, Vector3 end)
    {
        startPoint = start;
        endPoint = end;
        isActive = true;
        currentIntensity = maxIntensity;
        lifetime = maxLifetime;

        // Store colours for fading
        startColor = lineRenderer.startColor;
        endColor = lineRenderer.endColor;

        if (lightSource != null)
        {
            lightSource.intensity = maxIntensity;
            lightSource.transform.position = end; // Light at impact point
        }

        GenerateLightningPath();
    }
    
    private void FadeLightning()
    {
        lifetime -= Time.deltaTime;
        float alpha = Mathf.Clamp01(lifetime / maxLifetime);
    
        Color newStart = startColor;
        Color newEnd = endColor;
        newStart.a = alpha;
        newEnd.a = alpha;
    
        lineRenderer.startColor = newStart;
        lineRenderer.endColor = newEnd;
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

        for (int i = 1; i < segments; i++)
        {
            Vector3 basePosition = startPoint + direction * (segmentLength * i);

            // Add random offset perpendicular to the direction
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            if (perpendicular == Vector3.zero)
            {
                perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
            }

            float offsetAmount = Random.Range(-jaggedness, jaggedness);
            Vector3 offset = perpendicular * offsetAmount;

            // Also add some depth variation
            Vector3 depthOffset = Vector3.Cross(direction, perpendicular).normalized * Random.Range(-jaggedness * 0.5f, jaggedness * 0.5f);

            lineRenderer.SetPosition(i, basePosition + offset + depthOffset);
        }
    }

    private void AnimateLightning()
    {
        // Regenerate path periodically for flickering effect
        if (Random.value < Time.deltaTime * animationSpeed)
        {
            GenerateLightningPath();
        }
    }

    private void FadeLight()
    {
        if (lightSource == null) return;

        currentIntensity -= fadeSpeed * Time.deltaTime;
        currentIntensity = Mathf.Max(0f, currentIntensity);
        lightSource.intensity = currentIntensity;
    }
}
