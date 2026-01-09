using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles interactive path selection for spell targeting. Paths highlight when hovered
/// and can be clicked to set as spell target location. Uses Unity's EventSystem for pointer events.
/// </summary>
public class SelectedPath : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
{
    // ==================== Visual Components ====================
    private Material PathMat;
    private Color InitialColor;
    private MeshRenderer pathMeshRenderer;

    // Semi-transparent red highlight when path is selectable
    private Color selectedColor = new Color(1, 0, 0, 0.5f);
    
    public bool isHoveringOnPotentialPaths { get; private set; }
    
    // Determines how flames are distributed along the path (X-axis vs Z-axis)
    [SerializeField] private bool isOnXAxis;

    /// <summary>
    /// Exposes path orientation for flame positioning calculations.
    /// </summary>
    public bool isOnAxisProperty => isOnXAxis;
    

    private void OnEnable()
    {
        // Cache renderer and material references
        pathMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        PathMat = pathMeshRenderer.material;
        InitialColor = PathMat.color;
        
        // FlameArea is a child mesh that defines the bounds for flame spawning
        FlameArea = gameObject.transform.GetChild(0).GetComponent<MeshRenderer>();

        // Paths are invisible by default - only show when spell selection is active
        pathMeshRenderer.enabled = false;
        FlameArea.enabled = false;
    }

    /// <summary>
    /// Called when mouse enters this path's collider.
    /// Shows path highlight if spell targeting is active.
    /// </summary>
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (SpellAbility.instance != null)
        {
            // Only highlight if player is currently selecting a path for a spell
            if (SpellAbility.instance.CanSelectPaths)
            {
                // Make path visible with red highlight
                pathMeshRenderer.enabled = true;
                FlameArea.enabled = true;
                PathMat.color = new Color(1, 0, 0, 0.5f);
                PathMat.color = selectedColor;
                
                // Notify spell system that cursor is over valid target
                SpellAbility.instance.IsHoveringOnPotentialPaths(true);
            }
        }
    }
    
    /// <summary>
    /// Called when mouse leaves this path's collider.
    /// Hides the path highlight.
    /// </summary>
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        // Reset to original color and hide the path
        PathMat.color = InitialColor;
        
        if (SpellAbility.instance != null)
        {
            SpellAbility.instance.IsHoveringOnPotentialPaths(false);
        }
        
        // Make path invisible again
        pathMeshRenderer.enabled = false;
        FlameArea.enabled = false;
    }

    /// <summary>
    /// Smoothly fades the path color from selected (red) back to original color.
    /// Called after a spell is cast on this path.
    /// </summary>
    public IEnumerator ChangePathMatToOriginalColor()
    {
        float elapsed = 0;
        float duration = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Lerp between red highlight and original color using Vector4 for RGBA interpolation
            Color newColor = Vector4.Lerp(
                new Vector4(selectedColor.r, selectedColor.g, selectedColor.b, selectedColor.a),
                new Vector4(InitialColor.r, InitialColor.g, InitialColor.b, InitialColor.a), 
                t
            );
            PathMat.color = newColor;
            
            yield return null;
        }
    }

    /// <summary>
    /// Called when player clicks on this path.
    /// Performs a raycast to get exact world position and sends selection to SpellAbility.
    /// </summary>
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        // Only process clicks when spell targeting is active
        if (SpellAbility.instance.CanSelectPaths)
        {
            // Get mouse position using new Input System
            Vector2 mousepos = Mouse.current.position.ReadValue();

            // Create ray from camera through mouse position
            Ray ray = Camera.main.ScreenPointToRay(mousepos);

            RaycastHit hit;
            Vector3 mouseWorldPos = Vector3.zero;
           
            // Raycast to find exact world position where player clicked
            // Note: Using single & instead of && evaluates both conditions
            if (Physics.Raycast(ray.origin, ray.direction, out hit, 300f) & hit.collider != null)
            {
                mouseWorldPos = hit.point;
                
                // Slight Y offset to ensure VFX spawns above the surface
                mouseWorldPos.y += 0.1f;
                
                print($"<color=green> mouseWorldpos </color>" + mouseWorldPos);
                
                // Send this path and click position to the spell system
                SpellAbility.instance.SelectedPathFromPlayer(this, mouseWorldPos);
                
                // Debug visualization of the raycast
                Debug.DrawRay(ray.origin, ray.direction * 300f, Color.red, 10f);
            }
        }
    }

    /// <summary>
    /// Child mesh renderer that defines the area where flames can spawn.
    /// Bounds are used to calculate random flame positions.
    /// </summary>
    public MeshRenderer FlameArea { get; private set; }
}