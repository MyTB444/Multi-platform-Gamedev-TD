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
        towerButtons = GetComponentsInChildren<TowerButton>();
        gameManager = GameManager.instance;
        inputHandler = InputHandler.instance;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
            mrMat.color = Color.magenta;
        if (inputHandler != null)
            inputHandler.SelectedTower(this);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
            mrMat.color = originalColour;
    }

    public void OnPointerDown(PointerEventData eventData)
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
        else if (towerBuilt == true)
        {
            if (destroyButton.activeSelf == false)
            {
                destroyButton.SetActive(true);
            }
            else if (destroyButton.activeSelf == true)
            {
                destroyButton.SetActive(false);
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
    public void DestoryTower()
    {
        gameManager.UpdateSkillPoints(sellPrice);
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
}