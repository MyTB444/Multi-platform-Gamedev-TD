using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileButton : MonoBehaviour
{
    private MeshRenderer mr;
    private Animator anim;
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnMouseEnter()
    {
        mr.enabled = true;
    }

    private void OnMouseExit()
    {
        mr.enabled = false;
    }
    private void OnMouseDown()
    {
        anim.SetTrigger("TilePopup");
    }
}
