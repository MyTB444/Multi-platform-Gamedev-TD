using UnityEngine;
using System.Collections;

/// <summary>
/// Controls bow animation: drawing, releasing, and bowstring physics.
/// Manages the visual bow state including limb bending and bowstring position.
/// 
/// Three-phase release animation:
/// 1. Snap forward (bowstring snaps to rest position)
/// 2. Wait delay (brief pause before next draw)
/// 3. Return to hand (nock point returns to draw anchor)
/// </summary>
public class BowController : MonoBehaviour
{
    // ==================== LINE RENDERER ====================
    [Header("Line Renderer")]
    [SerializeField] private LineRenderer bowstring;  // Visual bowstring (3 points: top, nock, bottom)
    
    // ==================== BOW POINTS ====================
    [Header("Bow Points")]
    [SerializeField] private Transform tipTop;        // Top limb tip (bowstring attaches here)
    [SerializeField] private Transform tipBottom;     // Bottom limb tip (bowstring attaches here)
    [SerializeField] private Transform nockPoint;     // Where arrow nocks (middle of bowstring)
    
    // ==================== BOW LIMBS ====================
    [Header("Bow Limbs")]
    [SerializeField] private Transform limbTop;       // Top limb bone (bends when drawn)
    [SerializeField] private Transform limbBottom;    // Bottom limb bone (bends when drawn)
    [SerializeField] private float limbBendAngle = 15f;  // How much limbs rotate when drawn
    
    // ==================== DRAW SETTINGS ====================
    [Header("Draw Settings")]
    [SerializeField] private Transform drawAnchor;          // Where hand holds the arrow (follows animation)
    [SerializeField] private float snapForwardDuration = 0.05f;  // Time for bowstring to snap forward
    [SerializeField] private float returnDelay = 0.5f;           // Wait before returning to draw position
    [SerializeField] private float returnToHandDuration = 0.2f;  // Time to return nock to hand
    
    // ==================== STATE ====================
    private Vector3 nockRestLocalPosition;       // Nock position when bow is at rest
    private float currentReturnDelay;            // Adjusted delay based on attack speed
    private Vector3 limbTopRestRotation;         // Limb rotations at rest
    private Vector3 limbBottomRestRotation;
    private bool isDrawing = false;              // Currently drawing bow
    private bool isReleasing = false;            // In release animation sequence
    
    void Awake()
    {
        // Cache rest positions for reset
        nockRestLocalPosition = nockPoint.localPosition;
        limbTopRestRotation = limbTop.localEulerAngles;
        limbBottomRestRotation = limbBottom.localEulerAngles;
    }
    
    /// <summary>
    /// Updates bowstring and limb positions every frame.
    /// Runs in LateUpdate to follow animation bone positions.
    /// </summary>
    void LateUpdate()
    {
        // Follow hand position while not in release animation
        if (!isReleasing)
        {
            nockPoint.position = drawAnchor.position;
        }
        
        // Bend limbs when drawing
        if (isDrawing)
        {
            limbTop.localEulerAngles = new Vector3(limbTopRestRotation.x, limbTopRestRotation.y, limbTopRestRotation.z - limbBendAngle);
            limbBottom.localEulerAngles = new Vector3(limbBottomRestRotation.x, limbBottomRestRotation.y, limbBottomRestRotation.z - limbBendAngle);
        }
        
        // Update bowstring line renderer (3 points: top tip → nock → bottom tip)
        bowstring.SetPosition(0, tipTop.position);
        bowstring.SetPosition(1, nockPoint.position);
        bowstring.SetPosition(2, tipBottom.position);
    }
    
    /// <summary>
    /// Called when archer starts drawing bow.
    /// </summary>
    public void DrawBow()
    {
        StopAllCoroutines();
        isDrawing = true;
        isReleasing = false;
    }
    
    /// <summary>
    /// Called when archer releases arrow.
    /// </summary>
    /// <param name="cooldownRatio">Scales return delay based on attack speed upgrades</param>
    public void ReleaseBow(float cooldownRatio = 1f)
    {
        isDrawing = false;
        currentReturnDelay = returnDelay * cooldownRatio;
        StartCoroutine(ReleaseCoroutine());
    }
    
    /// <summary>
    /// Three-phase release animation:
    /// 1. Bowstring snaps forward to rest position
    /// 2. Brief pause before next draw
    /// 3. Nock point smoothly returns to hand
    /// </summary>
    private IEnumerator ReleaseCoroutine()
    {
        isReleasing = true;
        
        Vector3 startNockLocal = nockPoint.localPosition;
        Vector3 startLimbTop = limbTop.localEulerAngles;
        Vector3 startLimbBottom = limbBottom.localEulerAngles;
        
        // ===== PHASE 1: Snap forward to rest position =====
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / snapForwardDuration;
            
            // Lerp nock and limbs to rest position
            nockPoint.localPosition = Vector3.Lerp(startNockLocal, nockRestLocalPosition, t);
            limbTop.localEulerAngles = Vector3.Lerp(startLimbTop, limbTopRestRotation, t);
            limbBottom.localEulerAngles = Vector3.Lerp(startLimbBottom, limbBottomRestRotation, t);
            
            yield return null;
        }
        
        // Ensure exact rest position
        nockPoint.localPosition = nockRestLocalPosition;
        limbTop.localEulerAngles = limbTopRestRotation;
        limbBottom.localEulerAngles = limbBottomRestRotation;
        
        // ===== PHASE 2: Wait before returning to hand =====
        yield return new WaitForSeconds(currentReturnDelay);
        
        // ===== PHASE 3: Return to hand position =====
        t = 0f;
        Vector3 restWorldPos = nockPoint.position;
        while (t < 1f)
        {
            t += Time.deltaTime / returnToHandDuration;
            
            // Lerp nock back to hand (draw anchor)
            nockPoint.position = Vector3.Lerp(restWorldPos, drawAnchor.position, t);
            
            yield return null;
        }
        
        isReleasing = false;
    }
}