using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
        inputActions.Player.Pause.performed += PauseGame;
        inputActions.Player.SellTower.performed += SellTower;
        inputActions.Player.Restart.performed += RestartGame;
        uiBase = FindAnyObjectByType<UIBase>();
    }
    public void SelectedTower(TowerClick selectedTower)
    {
        tc = selectedTower;
    }
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

            if (gameEnd)
                UIBase.instance.StartButton();
        }
    }
}
