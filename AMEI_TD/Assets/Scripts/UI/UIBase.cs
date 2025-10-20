using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIBase : MonoBehaviour
{
    private bool isPaused;
    public static UIBase instance;
    void Awake()
    {
        instance = this;
        isPaused = false;
    }
    public void StartButton()
    {
        SceneManager.LoadScene("Eren");
    }
    public void Pause()
    {
        if (isPaused == false)
        {
            Time.timeScale = 0;
            isPaused = true;
            Debug.Log("Paused");
        }
        else
        {
            Time.timeScale = 1;
            isPaused = false;
            Debug.Log("Unpaused");
        }
    }
}
