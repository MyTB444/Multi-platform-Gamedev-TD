using UnityEngine;

/// <summary>
/// Applies a translucent ghost/spectral visual effect to all renderers on a GameObject.
/// Converts materials to transparent fade mode with emission glow.
/// Used by PhantomKnight for the spectral warrior appearance.
/// </summary>
public class GhostEffect : MonoBehaviour
{
    [Header("Ghost Settings")]
    [SerializeField] private Color ghostColor = new Color(0.5f, 0.8f, 1f, 0.6f);      // Base color with alpha
    [SerializeField] private Color emissionColor = new Color(0.3f, 0.5f, 0.7f, 1f);   // Glow color
    [SerializeField] private float emissionIntensity = 0.5f;                           // Glow brightness
    
    private Material[] ghostMaterials;  // References to created material instances
    
    private void Start()
    {
        ApplyGhostEffect();
    }
    
    /// <summary>
    /// Converts all renderers to ghost appearance by:
    /// 1. Creating material instances (to avoid modifying shared materials)
    /// 2. Setting blend mode to Fade (transparent)
    /// 3. Applying ghost color with transparency
    /// 4. Adding emission glow
    /// </summary>
    public void ApplyGhostEffect()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        ghostMaterials = new Material[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            // Create instance of material (prevents shared material modification)
            Material mat = new Material(renderers[i].material);
            
            // ===== CONFIGURE TRANSPARENCY =====
            // Set to Fade mode for proper transparency
            mat.SetFloat("_Mode", 2); // Fade mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);  // Disable depth write for transparency
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;  // Transparent render queue
            
            // Apply ghost color (includes alpha)
            mat.color = ghostColor;
            
            // ===== CONFIGURE EMISSION GLOW =====
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            
            // Assign material to renderer
            renderers[i].material = mat;
            ghostMaterials[i] = mat;
        }
    }
    
    /// <summary>
    /// Adjusts the alpha of all ghost materials.
    /// Used for fade-in/fade-out effects when phantom spawns or despawns.
    /// </summary>
    /// <param name="alpha">Alpha value (0 = invisible, 1 = fully visible based on ghostColor)</param>
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