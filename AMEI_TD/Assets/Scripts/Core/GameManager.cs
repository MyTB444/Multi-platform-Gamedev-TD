using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int points;
    public static GameManager instance;
    public WaveManager waveManager;
   
    private bool gameLost;
    public bool IsGameLost() => gameLost;
    
    [Header("Damage System")]
    [SerializeField] private TypeMatchupDatabase typeMatchupDatabase;

    private void Awake()
    {
        instance = this;
        DamageCalculator.Initialize(typeMatchupDatabase);
    }

    void Start()
    {
        UIBase.instance.UpdatePointsUI(points);
        waveManager.ActivateWaveManager();
    }

    // Use negative values to deduct points, positive values to add points
    public void UpdateSkillPoints(int newPoints)
    {
        points += newPoints;
        UIBase.instance.UpdatePointsUI(points);
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
        // InputHandler.instance.EnableRestart();
        // UIBase.instance.GameWon(playerWon);
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