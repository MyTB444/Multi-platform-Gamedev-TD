using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

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
    [SerializeField] private GameObject infoUI;
    [SerializeField] private TextMeshProUGUI pointsUI;
    [SerializeField] private TextMeshProUGUI hpUI;

    [SerializeField] private GameObject spellNotActive;


    void Awake()
    {
        instance = this;
        isPaused = false;
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
            infoUI.SetActive(false);
            Debug.Log("Unpaused");
        }
    }
    public void ViewSkillTree()
    {
        if (treeUI.activeSelf == false)
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
    public void UpdateHpUi(float value)
    {
        if (value > 0)
        {
            hpUI.text = value.ToString("0");
        }
        else
        {
            hpUI.text = "0";
            GameManager.instance.LevelFailed();
        }
    }


    // Shows win or lose UI based on game outcome
    public void GameWon(bool a)
    {
        if (a) winUI.SetActive(true);
        else lostUI.SetActive(true);
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void EnableInfo()
    {
        infoUI.SetActive(true);
    }
    public void DisableInfo()
    {
        infoUI.SetActive(false);
    }
    public void ActivateNotActiveText()
    {
        StopAllCoroutines();
        spellNotActive.SetActive(true);
        StartCoroutine(TextDelay());
    }
    private IEnumerator TextDelay()
    {
        yield return new WaitForSeconds(3.0f);
        spellNotActive.SetActive(false);
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}