using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIBase : MonoBehaviour
{
    private bool isPaused;
    public static UIBase instance;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private TextMeshProUGUI waveCounter;
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject lostUI;

    void Awake()
    {
        instance = this;
        isPaused = false;
    }

    public void StartButton()
    {
        SceneManager.LoadScene("Prototype");
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

    public void UpdateWaveTimerUI(float value)
    {
        waveCounter.text = value.ToString("0");
    }
    
    // Shows win or lose UI based on game outcome
    public void GameWon(bool a)
    {
        if (a) winUI.SetActive(true);
        else lostUI.SetActive(true);
    }
}