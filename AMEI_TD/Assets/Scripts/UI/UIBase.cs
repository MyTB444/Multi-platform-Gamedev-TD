using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIBase : MonoBehaviour
{
    private bool isPaused;
    public static UIBase instance;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private TextMeshProUGUI waveCounter;
    private WaveManager waveMan;
    void Awake()
    {
        instance = this;
        isPaused = false;
        waveMan = FindAnyObjectByType<WaveManager>();
    }
    void FixedUpdate()
    {
        waveCounter.text = waveMan.GetTimer().ToString("0");
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
            pauseUI.SetActive(true);
            Debug.Log("Paused");
        }
        else
        {
            Time.timeScale = 1;
            isPaused = false;
            pauseUI.SetActive(false);
            Debug.Log("Unpaused");
        }
    }
}
