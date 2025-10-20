using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private TowerClick tc;
    public static InputHandler instance;
    [SerializeField] private UIBase uiBase;
    void Awake()
    {
        instance = this;
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        inputActions.Player.Pause.performed += PauseGame;
        inputActions.Player.SellTower.performed += SellTower;
        uiBase = UIBase.instance;
    }
    public void SelectedTower(TowerClick selectedTower)
    {
        tc = selectedTower;
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
            //Debug.Log("P");
        }
    }
}
