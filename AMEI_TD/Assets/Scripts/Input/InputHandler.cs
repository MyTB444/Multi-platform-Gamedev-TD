using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private TowerClick tc;
    public static InputHandler instance;
    [SerializeField] private UIBase uiBase;
    private bool gameEnd;
    
    void Awake()
    {
        instance = this;
        gameEnd = false;
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        
        // Subscribe to input events
        inputActions.Player.Pause.performed += PauseGame;
        inputActions.Player.SellTower.performed += SellTower;
        inputActions.Player.Restart.performed += RestartGame;
        
        uiBase = FindAnyObjectByType<UIBase>();
    }
    
    // Called when player clicks on a tower to track the selected tower for selling
    public void SelectedTower(TowerClick selectedTower)
    {
        tc = selectedTower;
    }
    
    // Enables restart functionality when game ends (win or lose)
    public void EnableRestart()
    {
        gameEnd = true;
    }
    
    private void SellTower(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            if (tc != null)
                tc.DestoryTower();
        }
    }
    
    private void PauseGame(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            uiBase.Pause();
        }
    }
    
    private void RestartGame(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            // Only allow restart after game has ended
            if (gameEnd)
                UIBase.instance.StartButton();
        }
    }
}