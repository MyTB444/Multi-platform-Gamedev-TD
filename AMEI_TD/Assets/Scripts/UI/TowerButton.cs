using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TowerButton : MonoBehaviour
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Transform buildPosition;
    [SerializeField] private int towerPrice;
    private PlayerCastle playerCastle;
    private TileButton tb;
    private void Start()
    {
        //playerCastle = PlayerCastle.instance;
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
        if (playerCastle.GetPoints() >= towerPrice)
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
        playerCastle.RemovePoints(towerPrice);
        tb.DeactivateButtons();
        tb.ActivateTileButtons(false);
    }
    public bool GetActive()
    {
        return this.gameObject.activeSelf;
    }
    public int GetPrice() => towerPrice;
}


