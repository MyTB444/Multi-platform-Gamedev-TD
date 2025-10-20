using UnityEngine;
using UnityEngine.EventSystems;

public class TowerClick : MonoBehaviour
{
    [SerializeField] private GameObject destroyButton;
    [SerializeField] private int sellPrice;
    private GameManager gameManager;
    private InputHandler inputHandler;
    void Awake()
    {
        gameManager = GameManager.instance;
        inputHandler = InputHandler.instance;
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
        if (inputHandler != null)
            inputHandler.SelectedTower(this);
    }
    private void OnMouseExit()
    {
        if (inputHandler != null)
            inputHandler.SelectedTower(null);
    }

    public void DestoryTower()
    {
        gameManager.UpdateSkillPoints(sellPrice);
        Destroy(this.gameObject);
    }
}
