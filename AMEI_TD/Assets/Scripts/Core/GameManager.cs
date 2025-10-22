using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int points;
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
        points += 200;
        waveManager.ActivateWaveManager();
    }
    
    void Update()
    {
        points = Mathf.Clamp(points, 0, int.MaxValue);
        pointsUI.text = "Points: " + points.ToString();
        if(points > 50)
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

    public void UpdateSkillPoints(int newPoints)
    {
        points += newPoints;// merged two functions into one //add negative points to remove points and positive to gain points

        if (points <= 0 && gameLost == false) LevelFailed();
    }

    private void LevelFailed()
    {
        gameLost = true;
        StopWaveProgression();
    }

    public void LevelCompleted()
    {
        Debug.Log("You won yipeee");
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
