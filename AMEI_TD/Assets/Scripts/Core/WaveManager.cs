using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveDetails
{
    public int enemyBasic;
    public int enemyFast;
    public int enemyTank;
    public int enemyInvisible;
    public int enemyReinforced;
    public int enemySummoner;
}
public class WaveManager : MonoBehaviour
{
    [Header("Wave Details")] 
    [SerializeField] private float timeBetweenWaves;
    [SerializeField] private float waveTimer;
    [SerializeField] private WaveDetails[] levelWaves;

    [Header("Enemy Prefabs")] 
    [SerializeField] private GameObject enemyBasic;
    [SerializeField] private GameObject enemyFast;
    [SerializeField] private GameObject enemyTank;
    [SerializeField] private GameObject enemyInvisible;
    [SerializeField] private GameObject enemyReinforced;
    [SerializeField] private GameObject enemySummoner;
    

    private List<EnemySpawner> enemySpawners;
    private int waveIndex;
    private bool waveTimerEnabled;
    private bool makingNextWave;
    public bool gameBegan;

    private void Awake()
    {
        enemySpawners = new List<EnemySpawner>(FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        if (gameBegan == false) return;

        HandleWaveTimer();
    }
    

    public void ActivateWaveManager()
    {
        Debug.Log($"WaveManager activated. Spawners found: {enemySpawners.Count}, Waves configured: {levelWaves.Length}");
        gameBegan = true;
        EnableWaveTimer(true);
    }

    public void DeactivateWaveManager()
    {
        gameBegan = false;
        waveTimerEnabled = false;
    }
    
    // Called when an enemy is defeated to check if wave is complete and trigger next wave
    public void CheckIfWaveCompleted()
    {
        if (gameBegan == false || GameManager.instance.IsGameLost()) return;

        if (AllEnemiesDefeated() == false || AllSpawnersFinishedSpawning() == false || makingNextWave) return;

        makingNextWave = true;
        waveIndex++;

        if (HasNoMoreWaves())
        {
            GameManager.instance.LevelCompleted();
            return;
        }
        
        EnableWaveTimer(true);
    }

    private void EnableWaveTimer(bool enable)
    {
        if (enable && GameManager.instance.IsGameLost()) return;
        
        if (waveTimerEnabled == enable) return;

        waveTimer = timeBetweenWaves;
        waveTimerEnabled = enable;
    }
    
    private void HandleWaveTimer()
    {
        if (waveTimerEnabled == false) return;

        if (GameManager.instance.IsGameLost())
        {
            waveTimerEnabled = false;
            return;
        }

        waveTimer -= Time.deltaTime;
        //UIBase.instance.UpdateWaveTimerUI(waveTimer);

        if (waveTimer <= 0) StartNewWave();
    }

    private void StartNewWave()
    {
        if (GameManager.instance.IsGameLost()) return;
    
        Debug.Log($"Starting wave {waveIndex}");
        GiveEnemiesToSpawners();
        EnableWaveTimer(false);
        makingNextWave = false;
    }

    // Distributes enemies from the current wave across all spawners in round-robin fashion
    private void GiveEnemiesToSpawners()
    {
        List<GameObject> newEnemies = GetNewEnemies();
        Debug.Log($"Enemies to distribute: {(newEnemies != null ? newEnemies.Count : 0)}");
        int spawnerIndex = 0;

        if (newEnemies == null) return;

        for (int i = 0; i < newEnemies.Count; i++)
        {
            GameObject enemyToAdd = newEnemies[i];
            EnemySpawner spawnerToReceiveEnemy = enemySpawners[spawnerIndex];
            
            spawnerToReceiveEnemy.AddEnemy(enemyToAdd);
            
            spawnerIndex++;
            
            // Cycle back to first spawner if we've used all spawners
            if (spawnerIndex >= enemySpawners.Count) spawnerIndex = 0;
        }
    }

    private List<GameObject> GetNewEnemies()
    {
        if (HasNoMoreWaves())
        {
            Debug.Log("No more waves");
            return null;
        }

        List<GameObject> newEnemyList = new List<GameObject>();

        for (int i = 0; i < levelWaves[waveIndex].enemyBasic; i++)
        {
            newEnemyList.Add(enemyBasic);
        }
        
        for (int i = 0; i < levelWaves[waveIndex].enemyFast; i++)
        {
            newEnemyList.Add(enemyFast);
        }
        
        for (int i = 0; i < levelWaves[waveIndex].enemyTank; i++)
        {
            newEnemyList.Add(enemyTank);
        }
        
        for (int i = 0; i < levelWaves[waveIndex].enemyInvisible; i++)
        {
            newEnemyList.Add(enemyInvisible);
        }
        
        for (int i = 0; i < levelWaves[waveIndex].enemyReinforced; i++)
        {
            newEnemyList.Add(enemyReinforced);
        }
        
        for (int i = 0; i < levelWaves[waveIndex].enemySummoner; i++)
        {
            newEnemyList.Add(enemySummoner);
        }


        return newEnemyList;
    }

    private bool AllEnemiesDefeated()
    {
        foreach (EnemySpawner spawner in enemySpawners)
        {
            if (spawner.GetActiveEnemies().Count > 0)
            {
                return false;
            }
        }

        return true;
    }
    
    private bool AllSpawnersFinishedSpawning()
    {
        foreach (EnemySpawner spawner in enemySpawners)
        {
            if (spawner.HasEnemiesToSpawn())
            {
                return false;
            }
        }
        return true;
    }
    
    private bool HasNoMoreWaves() => waveIndex >= levelWaves.Length;
}