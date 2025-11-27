using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIBase : MonoBehaviour
{
    private bool isPaused;
    public static UIBase instance;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private TextMeshProUGUI waveCounter;

    [SerializeField] private GameObject combatIcon;
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject lostUI;
    [SerializeField] private GameObject treeUI;
    [SerializeField] private TextMeshProUGUI pointsUI;

    void Awake()
    {
        instance = this;
        isPaused = false;
    }

    public void StartButton()
    {
        SceneManager.LoadScene("Prototype");
    }
    public void Quit()
    {
        SceneManager.LoadScene("MainMenu");

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
    public void ViewSkillTree()
    {
        if(treeUI.activeSelf == false)
            treeUI.SetActive(true);
        else
        {
            treeUI.SetActive(false);
        }
    }
    public void UpdatePointsUI(int points)
    {
        points = Mathf.Clamp(points, 0, int.MaxValue);
        pointsUI.text = points.ToString();
        if (points < 50)
        {
            pointsUI.color = Color.red;
            if (points <= 0)
            {
                Debug.Log("YouDied");
            }
        }
    }

    public void UpdateWaveTimerUI(float value)
    {
        if (value > 0)
        {
            waveCounter.enabled = true;
            waveCounter.text = value.ToString("0");
            combatIcon.SetActive(false);
        }
        else
        {
            waveCounter.enabled = false;
            combatIcon.SetActive(true);
        }
    }

    // Shows win or lose UI based on game outcome
    public void GameWon(bool a)
    {
        if (a) winUI.SetActive(true);
        else lostUI.SetActive(true);
    }
}