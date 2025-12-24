using UnityEngine;
using UnityEngine.EventSystems;

public class TowerButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Transform buildPosition;
    [SerializeField] private int buyPrice;
    [SerializeField] private GameObject icon;
    private TileButton tb;
    private MeshRenderer mr;
    [SerializeField] private Material defaultMat;
    [SerializeField] private Material howeredMat;
    private Material[] matList;
    private Vector3 defaultSize;

    private void Awake()
    {
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
        icon.SetActive(false);
        this.gameObject.transform.localScale = defaultSize;
        matList[0] = defaultMat;
        mr.materials = matList;

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.instance.GetPoints() > buyPrice)
        {
            BuildTower(towerPrefab, buildPosition);
        }
        else
        {
            Debug.Log("U dont have money");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        icon.SetActive(true);
        this.gameObject.transform.localScale = new Vector3(6, 5.4f, 6);
        matList[0] = howeredMat;
        mr.materials = matList;

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        icon.SetActive(false);
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

    public bool GetActive()
    {
        return this.gameObject.activeSelf;
    }
}