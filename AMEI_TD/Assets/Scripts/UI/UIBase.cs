using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIBase : MonoBehaviour
{
    private bool isPaused;
    public void StartButton()
    {
        SceneManager.LoadScene("Eren");
    }
    public void Pause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }
    void Update()
    {
        // TODO Use new input system
        if (Input.GetKeyDown(KeyCode.Backspace))
            Pause();
    }
}
