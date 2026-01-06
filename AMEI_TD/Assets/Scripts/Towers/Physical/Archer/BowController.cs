using UnityEngine;
using System.Collections;

public class BowController : MonoBehaviour
{
    [Header("Line Renderer")]
    [SerializeField] private LineRenderer bowstring;
    
    [Header("Bow Points")]
    [SerializeField] private Transform tipTop;
    [SerializeField] private Transform tipBottom;
    [SerializeField] private Transform nockPoint;
    
    [Header("Bow Limbs")]
    [SerializeField] private Transform limbTop;
    [SerializeField] private Transform limbBottom;
    [SerializeField] private float limbBendAngle = 15f;
    
    [Header("Draw Settings")]
    [SerializeField] private Transform drawAnchor;
    [SerializeField] private float snapForwardDuration = 0.05f;
    [SerializeField] private float returnDelay = 0.5f;
    [SerializeField] private float returnToHandDuration = 0.2f;
    
    private Vector3 nockRestLocalPosition;
    private float currentReturnDelay;
    private Vector3 limbTopRestRotation;
    private Vector3 limbBottomRestRotation;
    private bool isDrawing = false;
    private bool isReleasing = false;
    
    void Awake()
    {
        nockRestLocalPosition = nockPoint.localPosition;
        limbTopRestRotation = limbTop.localEulerAngles;
        limbBottomRestRotation = limbBottom.localEulerAngles;
    }
    
    void LateUpdate()
    {
        if (!isReleasing)
        {
            nockPoint.position = drawAnchor.position;
        }
        
        if (isDrawing)
        {
            limbTop.localEulerAngles = new Vector3(limbTopRestRotation.x, limbTopRestRotation.y, limbTopRestRotation.z - limbBendAngle);
            limbBottom.localEulerAngles = new Vector3(limbBottomRestRotation.x, limbBottomRestRotation.y, limbBottomRestRotation.z - limbBendAngle);
        }
        
        bowstring.SetPosition(0, tipTop.position);
        bowstring.SetPosition(1, nockPoint.position);
        bowstring.SetPosition(2, tipBottom.position);
    }
    
    public void DrawBow()
    {
        StopAllCoroutines();
        isDrawing = true;
        isReleasing = false;
    }
    
    public void ReleaseBow(float cooldownRatio = 1f)
    {
        isDrawing = false;
        currentReturnDelay = returnDelay * cooldownRatio;
        StartCoroutine(ReleaseCoroutine());
    }
    
    private IEnumerator ReleaseCoroutine()
    {
        isReleasing = true;
        
        Vector3 startNockLocal = nockPoint.localPosition;
        Vector3 startLimbTop = limbTop.localEulerAngles;
        Vector3 startLimbBottom = limbBottom.localEulerAngles;
        
        // Phase 1: Snap forward to rest position
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / snapForwardDuration;
            
            nockPoint.localPosition = Vector3.Lerp(startNockLocal, nockRestLocalPosition, t);
            limbTop.localEulerAngles = Vector3.Lerp(startLimbTop, limbTopRestRotation, t);
            limbBottom.localEulerAngles = Vector3.Lerp(startLimbBottom, limbBottomRestRotation, t);
            
            yield return null;
        }
        
        nockPoint.localPosition = nockRestLocalPosition;
        limbTop.localEulerAngles = limbTopRestRotation;
        limbBottom.localEulerAngles = limbBottomRestRotation;
        
        // Phase 2: Wait before returning to hand
        yield return new WaitForSeconds(currentReturnDelay);
        
        // Phase 3: Return to hand position
        t = 0f;
        Vector3 restWorldPos = nockPoint.position;
        while (t < 1f)
        {
            t += Time.deltaTime / returnToHandDuration;
            
            nockPoint.position = Vector3.Lerp(restWorldPos, drawAnchor.position, t);
            
            yield return null;
        }
        
        isReleasing = false;
    }
}