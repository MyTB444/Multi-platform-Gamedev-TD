using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private TowerButton[] towerButtons;
    private MeshRenderer mr;
    private Material mrMat;
    private Animator anim;
    private Color originalColour;
    private bool buttonControl;
    private bool towerBuilt;
    [SerializeField] private GameObject spawnedUnit;
    [SerializeField] private GameObject destroyButton;
    [SerializeField] private int sellPrice;
    private GameManager gameManager;
    private InputHandler inputHandler;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        mrMat = mr.material;
        originalColour = mrMat.color;
        anim = GetComponent<Animator>();
        towerButtons = GetComponentsInChildren<TowerButton>(true);
        gameManager = GameManager.instance;
        inputHandler = InputHandler.instance;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mrMat.color = Color.magenta;
        StopAllCoroutines();
        if (inputHandler != null)
            inputHandler.SelectedTower(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mrMat.color = originalColour;
        if (buttonControl)
        {
            StartCoroutine(DeActivationDelay());
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (towerBuilt == false)
        {
            if (buttonControl == false)
            {
                buttonControl = true;
                ActivateTileButtons(true);
                ActivateButtons();
            }
            else if (buttonControl)
            {
                buttonControl = false;
                DeactivateButtons();
            }
        }
        else if (towerBuilt == true)
        {
            if (destroyButton.activeSelf == false)
            {
                destroyButton.SetActive(true);
                SellButton sb = destroyButton.GetComponent<SellButton>();
                sb.SetDefaultShake();
                StartCoroutine(DestroyButtonDisabler());
            }
            else if (destroyButton.activeSelf == true)
            {
                destroyButton.SetActive(false);
                StopAllCoroutines();
            }
        }
    }

    private IEnumerator DestroyButtonDisabler()
    {
        yield return new WaitForSeconds(5.0f);
        if (destroyButton.activeSelf == true)
        {
            destroyButton.SetActive(false);
        }
    }
    private IEnumerator DeActivationDelay()
    {
        yield return new WaitForSeconds(1.5f);
        buttonControl = false;
        DeactivateButtons();
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
        // Hide ALL first
        for (int i = 0; i < towerButtons.Length; i++)
        {
            towerButtons[i].gameObject.SetActive(false);
        }
    
        List<ElementType> availableElements = gameManager.GetAvailableElements();
    
        // Only show available ones
        for (int i = 0; i < towerButtons.Length; i++)
        {
            ElementType buttonElement = towerButtons[i].GetElementType();
        
            if (availableElements.Contains(buttonElement))
            {
                towerButtons[i].Activate();
            }
        }
    }

    public void ActivateTileButtons(bool a)
    {
        anim.SetBool("TowerButtonPopup", a);
    }

    public void DestoryTower()
    {
        TowerBase towerBase = spawnedUnit.GetComponent<TowerBase>();
        int sellP = towerBase.GetSellPrice();
        gameManager.UpdateSkillPoints(sellP);
        Destroy(spawnedUnit);
        towerBuilt = false;
        if (destroyButton.activeSelf == true)
        {
            destroyButton.SetActive(false);
        }
    }

    public void SetTowerBuiltMode(bool a)
    {
        towerBuilt = a;
    }

    public void SetUnit(GameObject a)
    {
        spawnedUnit = a;
    }
    
    public GameObject GetUnit()
    {
        return spawnedUnit;
    }
}