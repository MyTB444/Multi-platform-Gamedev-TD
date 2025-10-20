using UnityEngine;
using UnityEngine.EventSystems;

public class TowerClick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
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
    public void OnPointerDown(PointerEventData eventData)
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
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inputHandler != null)
            inputHandler.SelectedTower(this);
    }
    public void OnPointerExit(PointerEventData eventData)
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
