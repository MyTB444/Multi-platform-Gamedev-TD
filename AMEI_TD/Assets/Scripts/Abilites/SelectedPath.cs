using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SelectedPath : MonoBehaviour,IPointerEnterHandler,IPointerDownHandler,IPointerExitHandler
{
    private Material PathMat;
    private Color InitialColor;
    private Color selectedColor = new Color(1, 0, 0, 0.5f);
    public bool isHoveringOnPotentialPaths {  get; private set; }
    [SerializeField] private bool isOnXAxis;

    public bool isOnAxisProperty => isOnXAxis;
    

    private void OnEnable()
    {
        PathMat = gameObject.GetComponent<MeshRenderer>().material;
        InitialColor = PathMat.color;
        FlameArea = gameObject.transform.GetChild(0).GetComponent<MeshRenderer>();

    }
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (SpellAbility.instance != null)
        {
            if (SpellAbility.instance.CanSelectPaths)
            {
                PathMat.color = selectedColor;
                SpellAbility.instance.IsHoveringOnPotentialPaths(true);
            }
       
        }

    }
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        PathMat.color = InitialColor;
        if (SpellAbility.instance != null)
        {
            SpellAbility.instance.IsHoveringOnPotentialPaths(false);
        }
    }

    public IEnumerator ChangePathMatToOriginalColor()
    {
        float elapsed = 0;
        float duration = 1f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t =elapsed/ duration;
            Color newColor = Vector4.Lerp(new Vector4(selectedColor.r,selectedColor.g,selectedColor.b,selectedColor.a),
                                          new Vector4(InitialColor.r, InitialColor.g, InitialColor.b, InitialColor.a), t);
            PathMat.color = newColor;
            yield return null;

        }
        
      
       
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (SpellAbility.instance.CanSelectPaths)
        {
            
            Vector2 mousepos = Mouse.current.position.ReadValue();

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
            //Vector3 mousepos = Input.mousePosition - Camera.main.transform.position;


         
        }
    }

  

    public MeshRenderer FlameArea {  get; private set; }
}
