using UnityEngine;

public class GhostEffect : MonoBehaviour
{
    [Header("Ghost Settings")]
    [SerializeField] private Color ghostColor = new Color(0.5f, 0.8f, 1f, 0.6f);
    [SerializeField] private Color emissionColor = new Color(0.3f, 0.5f, 0.7f, 1f);
    [SerializeField] private float emissionIntensity = 0.5f;
    
    private Material[] ghostMaterials;
    
    private void Start()
    {
        ApplyGhostEffect();
    }
    
    public void ApplyGhostEffect()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        ghostMaterials = new Material[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            // Create instance of material
            Material mat = new Material(renderers[i].material);
            
            // Set to transparent/fade mode
            mat.SetFloat("_Mode", 2); // Fade mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            // Set ghost color
            mat.color = ghostColor;
            
            // Enable emission for glow
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            
            renderers[i].material = mat;
            ghostMaterials[i] = mat;
        }
    }
    
    public void SetAlpha(float alpha)
    {
        if (ghostMaterials == null) return;
        
        foreach (Material mat in ghostMaterials)
        {
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }
    }
}