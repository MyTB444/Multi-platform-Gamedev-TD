using UnityEngine;
using UnityEngine.EventSystems;

public class TowerClick : MonoBehaviour
{

    public GameObject destroyButton; // Assign in Inspector or dynamically

    void OnMouseEnter()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return; // Ignore UI clicks
                    // Instantiate UI above the tower
        destroyButton.SetActive(true);

    }
    void OnMouseExit()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return; // Ignore UI clicks
                    // Instantiate UI above the tower
        destroyButton.SetActive(false);

    }
    public void DestoryTower()
    {
        Destroy(this.gameObject);
    }
}
