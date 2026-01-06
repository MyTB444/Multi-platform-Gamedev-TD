using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectedPath : MonoBehaviour
{
    private Material PathMat;
    private Color InitialColor;
    private MeshRenderer pathMeshRenderer;

    [SerializeField] private bool isOnXAxis;

    public bool isOnAxisProperty => isOnXAxis;

    private void OnEnable()
    {
        pathMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        PathMat = pathMeshRenderer.material;
        InitialColor = PathMat.color;
        FlameArea = gameObject.transform.GetChild(0).GetComponent<MeshRenderer>();

        // Disable both mesh renderers by default
        pathMeshRenderer.enabled = false;
        FlameArea.enabled = false;
    }
    private void OnMouseEnter()
    {
        if (SpellAbility.instance != null)
        {
            if (SpellAbility.instance.CanSelectPaths)
            {
                // Enable both mesh renderers and show red highlight
                pathMeshRenderer.enabled = true;
                FlameArea.enabled = true;
                PathMat.color = new Color(1, 0, 0, 0.5f);
            }
        }
    }
    private void OnMouseExit()
    {
        // Reset color and disable both mesh renderers
        PathMat.color = InitialColor;
        pathMeshRenderer.enabled = false;
        FlameArea.enabled = false;
    }
    private void OnMouseDown()
    {
        if (SpellAbility.instance.CanSelectPaths)
        {
            
            Vector3 mousepos = (Input.mousePosition);

            Ray ray = Camera.main.ScreenPointToRay(mousepos);


            RaycastHit hit;
            Vector3 mouseWorldPos = Vector3.zero;
           
            if (Physics.Raycast(ray.origin,ray.direction ,out hit,300f) & hit.collider != null)
            {
                
                mouseWorldPos = hit.point;
                mouseWorldPos.y += 0.1f;
                print($"<color=green> mouseWorldpos </color>" + mouseWorldPos);
                SpellAbility.instance.SelectedPathFromPlayer(this, mouseWorldPos);
                Debug.DrawRay(ray.origin,ray.direction*300f, Color.red,10f);

            }
        }
    }

  

    public MeshRenderer FlameArea {  get; private set; }
}
