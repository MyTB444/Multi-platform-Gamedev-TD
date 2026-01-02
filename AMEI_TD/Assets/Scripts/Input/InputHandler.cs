using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private TileButton tb;
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
        inputActions.Player.SkillTree.performed += SkillTree;
        inputActions.Player.PhysicalSpell.performed += Physical;
        inputActions.Player.MagicalSpell.performed += Magical;
        inputActions.Player.MechanicalSpell.performed += Mechanical;
        inputActions.Player.ImaginarySpell.performed += Imaginary;

        uiBase = FindAnyObjectByType<UIBase>();
    }

    // Called when player clicks on a tower to track the selected tower for selling
    public void SelectedTower(TileButton selectedTower)
    {
        tb = selectedTower;
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
            if (tb != null)
                tb.DestoryTower();
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
                Menu.instance.RestartGame();
        }
    }
    private void SkillTree(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            UIBase.instance.ViewSkillTree();
        }
    }
    private void Physical(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            //Adnan
        }
    }
    private void Magical(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            //Adnan
        }
    }
    private void Mechanical(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            //Adnan
        }
    }
    private void Imaginary(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            //Adnan
        }
    }
}