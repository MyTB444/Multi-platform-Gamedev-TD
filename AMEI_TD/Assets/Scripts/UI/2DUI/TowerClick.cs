using UnityEngine;
using UnityEngine.EventSystems;

public class TowerClick : MonoBehaviour
{
    private bool onTower = false;
    [SerializeField] private GameObject destroyButton;

    void FixedUpdate()
    {
        if (onTower == true)
            KeyboardDestroy();
    }

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
    private void OnMouseEnter()
    {
        onTower = true;
    }
    private void OnMouseExit()
    {
        onTower = false;
    }
    private void KeyboardDestroy()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            Destroy(this.gameObject);
        }
    }
    public void DestoryTower()
    {
        Destroy(this.gameObject);
    }
}
