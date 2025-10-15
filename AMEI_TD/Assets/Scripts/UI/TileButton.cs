using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileButton : MonoBehaviour
{
    [SerializeField] private TowerButton[] towerButtons;
    private MeshRenderer mr;
    private Animator anim;
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        anim = GetComponent<Animator>();
        towerButtons = GetComponentsInChildren<TowerButton>();
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
        ActivateButtons();
        ActivateTileButtons(true);
    }
    public void DeactivateButtons()
    {
        for (int i = 0; i < towerButtons.Length; i++)
        {
            if (towerButtons[i].GetActive() == true)
                towerButtons[i].DeActivate();
        }
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
}
