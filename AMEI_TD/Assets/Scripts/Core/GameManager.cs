using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int points;
    public static GameManager instance;
    public WaveManager waveManager;
    [SerializeField] private TextMeshProUGUI pointsUI;

    private bool gameLost;
    public bool IsGameLost() => gameLost;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UpdatePointsUI();
        waveManager.ActivateWaveManager();
    }

    private void UpdatePointsUI()
    {
        points = Mathf.Clamp(points, 0, int.MaxValue);
        pointsUI.text = "Points: " + points.ToString();
        if (points > 50)
        {
            pointsUI.color = Color.green;
        }
        if (points <= 50)
        {
            pointsUI.color = Color.red;
            if (points <= 0)
            {
                Debug.Log("YouDied");
            }
        }
    }

    // Use negative values to deduct points, positive values to add points
    public void UpdateSkillPoints(int newPoints)
    {
        points += newPoints;
        UpdatePointsUI();
        if (points <= 0 && gameLost == false) LevelFailed();
    }

    private void LevelFailed()
    {
        GameOverOutcomeUI(false);
        gameLost = true;
        StopWaveProgression();
    }

    public void LevelCompleted()
    {
        GameOverOutcomeUI(true);
    }
    
    private void GameOverOutcomeUI(bool playerWon)
    {
        InputHandler.instance.EnableRestart();
        UIBase.instance.GameWon(playerWon);
    }

    private void StopWaveProgression()
    {
        StopMakingEnemies();

        if (waveManager != null) waveManager.DeactivateWaveManager();
    }

    public void StopMakingEnemies()
    {
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);

        foreach (var spawner in spawners)
        {
            spawner.CanCreateNewEnemies(false);
        }
    }

    public int GetPoints() => points;
}