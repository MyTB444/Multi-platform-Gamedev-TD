using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int points;
    [SerializeField] private float healthPoints = 10f;
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
        UIBase.instance.UpdateHpUi(healthPoints);
    }

    // Use negative values to deduct points, positive values to add points
    public void UpdateSkillPoints(int newPoints)
    {
        points += newPoints;
        UIBase.instance.UpdatePointsUI(points);
        SkillTreeManager.instance.PointsGained();
        //LevelFailed();
    }

    public void LevelFailed()
    {
        InputHandler.instance.DisableInput();
        GameOverOutcomeUI(false);
        gameLost = true;
        StopWaveProgression();
    }

    public void LevelCompleted()
    {
        InputHandler.instance.DisableInput();
        GameOverOutcomeUI(true);
    }
    
    private void GameOverOutcomeUI(bool playerWon)
    {
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
    public float GetHealthPoints() => healthPoints;
    public void TakeDamageHealth(float damage)
    {
        healthPoints -= damage;
        UIBase.instance.UpdateHpUi(healthPoints);

        if (healthPoints <= 0 && !gameLost)
        {
            healthPoints = 0;
            LevelFailed();
        }
    }
}