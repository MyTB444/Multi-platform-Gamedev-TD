using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TowerButton : MonoBehaviour
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Transform buildPosition;
    private GameManager gameManager;
    [SerializeField] private int buyPrice;
    private TileButton tb;
    private void Start()
    {
        gameManager = GameManager.instance;
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
    private void OnMouseDown()
    {
        if (gameManager.GetPoints() >= buyPrice)
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
        gameManager.UpdateSkillPoints(-buyPrice);
        tb.DeactivateButtons();
        tb.ActivateTileButtons(false);
    }
    public bool GetActive()
    {
        return this.gameObject.activeSelf;
    }

}


