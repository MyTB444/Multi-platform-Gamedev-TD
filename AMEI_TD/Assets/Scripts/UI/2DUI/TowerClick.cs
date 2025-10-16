using UnityEngine;
using UnityEngine.EventSystems;

public class TowerClick : MonoBehaviour
{
    private bool onTower = false;
    [SerializeField] private GameObject destroyButton;

    private PlayerCastle playerCastle;
    void Start()
    {
        playerCastle = PlayerCastle.instance;
    }
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
        playerCastle.AddPoints(15);
        Destroy(this.gameObject);
    }
}
