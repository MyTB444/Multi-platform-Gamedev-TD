using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileButton : MonoBehaviour
{
    [SerializeField] private TowerButton[] towerButtons;
    private MeshRenderer mr;
    private Material mrMat;
    private Animator anim;
    private Color originalColour;
    private bool buttonControl;
    private bool towerBuilt;
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        mrMat = mr.material;
        originalColour = mrMat.color;
        anim = GetComponent<Animator>();
        towerButtons = GetComponentsInChildren<TowerButton>();
    }
    void Update()
    {
        DetectTowerAbove();
    }
    private void OnMouseEnter()
    {
        if (towerBuilt == false)
            mrMat.color = Color.magenta;
    }

    private void OnMouseExit()
    {
        if (towerBuilt == false)
            mrMat.color = originalColour;
    }
    private void OnMouseDown()
    {
        if (towerBuilt == false)
        {
            if (buttonControl == false)
            {
                buttonControl = true;
                ActivateButtons();
                ActivateTileButtons(true);
            }
            else if (buttonControl)
            {
                buttonControl = false;
                DeactivateButtons();
            }
        }
    }
    public void DeactivateButtons()
    {
        for (int i = 0; i < towerButtons.Length; i++)
        {
            if (towerButtons[i].GetActive() == true)
                towerButtons[i].DeActivate();
        }
        ActivateTileButtons(false);
    }
    private void ActivateButtons()
    {
        for (int i = 0; i < towerButtons.Length; i++)
        {
            if (towerButtons[i].GetActive() == false)
                towerButtons[i].Activate();
        }
    }
    public void ActivateTileButtons(bool a)
    {
        anim.SetBool("TowerButtonPopup", a);
    }
    private void DetectTowerAbove()
    {
        int layerMask = 1 << 7;

        Vector3 origin = transform.position;
        Vector3 direction = Vector3.up;

        Debug.DrawRay(origin, direction * 10.0f, Color.green);
        RaycastHit hit;

        bool isHit = Physics.Raycast(origin, direction, out hit, 10.0f, layerMask);

        if (isHit)
        {
            towerBuilt = true;
        }
        else if (isHit == false)
        {
            towerBuilt = false;
        }

    }
}
