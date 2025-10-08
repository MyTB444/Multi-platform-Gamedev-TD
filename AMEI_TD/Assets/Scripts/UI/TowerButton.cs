using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TowerButton : MonoBehaviour
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Transform  buildPosition;
    private void OnMouseDown()
    {
        BuildTower(towerPrefab, buildPosition);
    }
    private void BuildTower(GameObject tower, Transform pos)
    {
        Instantiate(tower.gameObject, pos.position, Quaternion.identity);
        this.gameObject.SetActive(false);
    }
}
