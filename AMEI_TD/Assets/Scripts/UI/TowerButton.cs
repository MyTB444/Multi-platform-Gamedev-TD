using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TowerButton : MonoBehaviour
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Transform buildPosition;
    private TileButton tb;
    private void Start()
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
    private void OnMouseDown()
    {
        BuildTower(towerPrefab, buildPosition);
    }
    private void BuildTower(GameObject tower, Transform pos)
    {
        Instantiate(tower.gameObject, pos.position, Quaternion.identity);
        tb.DeactivateButtons();
        tb.ActivateTileButtons(false);
    }
    public bool GetActive()
    {
        return this.gameObject.activeSelf;
    }
}


