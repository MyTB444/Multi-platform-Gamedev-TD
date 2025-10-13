using UnityEngine;
using UnityEngine.EventSystems;

public class TowerClick : MonoBehaviour
{

    [SerializeField] private GameObject destroyButton;

    private void OnMouseDown()
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
    public void DestoryTower()
    {
        Destroy(this.gameObject);
    }
}
