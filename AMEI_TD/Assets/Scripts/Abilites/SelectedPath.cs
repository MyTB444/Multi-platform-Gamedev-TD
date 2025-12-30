using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectedPath : MonoBehaviour
{
    private Material PathMat;
    private Color InitialColor;

    

   

    private void OnEnable()
    {
        PathMat = gameObject.GetComponent<MeshRenderer>().material;
        InitialColor = PathMat.color;
        FlameArea = gameObject.transform.GetChild(0).GetComponent<MeshRenderer>();

    }
    private void OnMouseEnter()
    {
        if (SpellAbility.instance != null)
        {
            if (SpellAbility.instance.CanSelectPaths)
            {
                PathMat.color = new Color(1, 0, 0, 0.5f);
            }
        }
    }
    private void OnMouseExit()
    {
        PathMat.color = InitialColor;
    }
    private void OnMouseDown()
    {
        if (SpellAbility.instance.CanSelectPaths)
        {
            Vector3 mousepos = Input.mousePosition - Camera.main.transform.position;
            Ray ray = Camera.main.ScreenPointToRay(mousepos);

            RaycastHit hit;
            Vector3 mouseWorldPos = Vector3.zero;
            if (Physics.Raycast(ray, out hit) & hit.collider != null)
            {
              
                mouseWorldPos = hit.point;
                mouseWorldPos.y += 0.1f;
                print($"<color=green> mouseWorldpos </color>" + mouseWorldPos);
                SpellAbility.instance.SelectedPathFromPlayer(this, mouseWorldPos);
               
            }
            //Vector3 mousepos = Input.mousePosition - Camera.main.transform.position;


         
        }
    }

  

    public MeshRenderer FlameArea {  get; private set; }
}
