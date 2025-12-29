using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            SpellAbility.instance.SelectedPathFromPlayer(this);
        }
    }

    public MeshRenderer FlameArea {  get; private set; }
}
