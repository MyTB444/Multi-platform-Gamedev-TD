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
    public int enemyAdaptive;
    public int enemySplitter;
    public int enemyGhostwalk;
    public int enemyDecoy;
}

[Serializable]
public class EnemyPoolConfig
{
    public EnemyType enemyType;
    public GameObject prefab;
    public int poolAmount = 10;
}

public class WaveManager : MonoBehaviour
{
    [Header("Wave Details")] 
    [SerializeField] private float timeBetweenWaves;
    [SerializeField] private float waveTimer;
    [SerializeField] private WaveDetails[] levelWaves;

    [Header("Enemy Prefabs")]
    [SerializeField] private List<EnemyPoolConfig> enemyPrefabs;

    private Dictionary<EnemyType, GameObject> enemyPrefabLookup;
    private List<EnemySpawner> enemySpawners;
    private int waveIndex;
    private bool waveTimerEnabled;
    private bool makingNextWave;
    public bool gameBegan;

    private void Awake()
    {
        enemySpawners = new List<EnemySpawner>(FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None));
        
        // Build lookup and register pools
        enemyPrefabLookup = new Dictionary<EnemyType, GameObject>();
        foreach (var config in enemyPrefabs)
        {
            if (config.prefab != null)
            {
                enemyPrefabLookup[config.enemyType] = config.prefab;
                ObjectPooling.instance.Register(config.prefab, config.poolAmount);
            }
        }
    }

    private void Update()
    {
        if (gameBegan == false) return;

        HandleWaveTimer();
    }

    public void ActivateWaveManager()
    {
        gameBegan = true;
        EnableWaveTimer(true);
    }

    public void DeactivateWaveManager()
    {
        gameBegan = false;
        waveTimerEnabled = false;
    }
    
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
        
        GiveEnemiesToSpawners();
        EnableWaveTimer(false);
        makingNextWave = false;
    }

    private void GiveEnemiesToSpawners()
    {
        List<GameObject> newEnemies = GetNewEnemies();
        int spawnerIndex = 0;

        if (newEnemies == null) return;

        for (int i = 0; i < newEnemies.Count; i++)
        {
            GameObject enemyToAdd = newEnemies[i];
            EnemySpawner spawnerToReceiveEnemy = enemySpawners[spawnerIndex];
            
            spawnerToReceiveEnemy.AddEnemy(enemyToAdd);
            
            spawnerIndex++;
            
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
        WaveDetails wave = levelWaves[waveIndex];

        AddEnemiesToList(newEnemyList, EnemyType.Basic, wave.enemyBasic);
        AddEnemiesToList(newEnemyList, EnemyType.Fast, wave.enemyFast);
        AddEnemiesToList(newEnemyList, EnemyType.Tank, wave.enemyTank);
        AddEnemiesToList(newEnemyList, EnemyType.Invisible, wave.enemyInvisible);
        AddEnemiesToList(newEnemyList, EnemyType.Reinforced, wave.enemyReinforced);
        AddEnemiesToList(newEnemyList, EnemyType.Summoner, wave.enemySummoner);
        AddEnemiesToList(newEnemyList, EnemyType.Adaptive, wave.enemyAdaptive);
        AddEnemiesToList(newEnemyList, EnemyType.Splitter, wave.enemySplitter);
        AddEnemiesToList(newEnemyList, EnemyType.Ghostwalk, wave.enemyGhostwalk);
        AddEnemiesToList(newEnemyList, EnemyType.Decoy, wave.enemyDecoy);

        return newEnemyList;
    }

    private void AddEnemiesToList(List<GameObject> list, EnemyType type, int count)
    {
        if (count <= 0) return;
        if (!enemyPrefabLookup.TryGetValue(type, out GameObject prefab)) return;

        for (int i = 0; i < count; i++)
        {
            list.Add(prefab);
        }
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