using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TowerButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private GameObject towerPrefab2;
    [SerializeField] private Transform buildPosition;
    [SerializeField] private int buyPrice;
    [SerializeField] private GameObject icon;
    [SerializeField] private GameObject icon2;

    private bool towerSwapped;
    private TileButton tb;
    private MeshRenderer mr;
    [SerializeField] private Material defaultMat;
    [SerializeField] private Material howeredMat;
    private Material[] matList;
    private Vector3 defaultSize;
    public SwapType buttonType;

    public SkillNode difficultyEvent;

    private void Awake()
    {
        towerSwapped = false;
        difficultyEvent.EventRaised += SwapTower;
        defaultSize = this.gameObject.transform.localScale;
        tb = GetComponentInParent<TileButton>();
        mr = GetComponent<MeshRenderer>();
        matList = mr.materials;
    }

    public void Activate()
    {
        this.gameObject.SetActive(true);
    }

    public void DeActivate()
    {
        this.gameObject.SetActive(false);
        if (!towerSwapped)
        {
            icon.SetActive(false);
        }
        else
        {
            icon2.SetActive(false);
        }
        this.gameObject.transform.localScale = defaultSize;
        matList[0] = defaultMat;
        mr.materials = matList;

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.instance.GetPoints() > buyPrice)
        {
            if (!towerSwapped)
            {
                BuildTower(towerPrefab, buildPosition);
            }
            else
            {
                BuildTower(towerPrefab2, buildPosition);

            }
        }
        else
        {
            Debug.Log("U dont have money");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!towerSwapped)
        {
            icon.SetActive(true);
        }
        else
        {
            icon2.SetActive(true);
        }
        this.gameObject.transform.localScale = new Vector3(6, 5.4f, 6);
        matList[0] = howeredMat;
        mr.materials = matList;

    }

    public void OnPointerExit(PointerEventData eventData)
    {

        if (!towerSwapped)
        {
            icon.SetActive(false);
        }
        else
        {
            icon2.SetActive(false);
        }
        this.gameObject.transform.localScale = defaultSize;
        matList[0] = defaultMat;
        mr.materials = matList;
    }
    private void BuildTower(GameObject tower, Transform pos)
    {
        tb.SetUnit(Instantiate(tower.gameObject, new Vector3(pos.position.x, pos.position.y, pos.position.z), Quaternion.identity));
        GameManager.instance.UpdateSkillPoints(-buyPrice);
        tb.SetTowerBuiltMode(true);
        tb.DeactivateButtons();
        tb.ActivateTileButtons(false);
    }
    private void SwapTower(SwapType givenType)
    {
        if (givenType == buttonType)
        {
            towerSwapped = true;
        }

    }

    public bool GetActive()
    {
        return this.gameObject.activeSelf;
    }
}