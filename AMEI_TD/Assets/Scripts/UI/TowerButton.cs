using UnityEngine;
using UnityEngine.EventSystems;

public class TowerButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Transform buildPosition;
    [SerializeField] private int buyPrice;
    private TileButton tb;
    
    private void Awake()
    {
        tb = GetComponentInParent<TileButton>();
    }
    
    public void Activate()
    {
        this.gameObject.SetActive(true);
    }
    
    public void DeActivate()
    {
        this.gameObject.SetActive(false);
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.instance.GetPoints() >= buyPrice)
        {
            BuildTower(towerPrefab, buildPosition);
        }
        else
        {
            Debug.Log("U dont have money");
        }
    }
    
    private void BuildTower(GameObject tower, Transform pos)
    {
        Instantiate(tower.gameObject, new Vector3(pos.position.x, pos.position.y, pos.position.z), Quaternion.identity);
        GameManager.instance.UpdateSkillPoints(-buyPrice);
        tb.DeactivateButtons();
        tb.ActivateTileButtons(false);
    }
    
    public bool GetActive()
    {
        return this.gameObject.activeSelf;
    }
}