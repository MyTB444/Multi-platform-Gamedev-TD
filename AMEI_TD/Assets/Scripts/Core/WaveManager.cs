using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveDetails
{
    public int enemyBasic;
    public int enemyFast;
    public int enemyTank;
}
public class WaveManager : MonoBehaviour
{
    private UIBase uiBase;
    [Header("Wave Details")] 
    [SerializeField] private float timeBetweenWaves;
    [SerializeField] private float waveTimer;
    [SerializeField] private WaveDetails[] levelWaves;

    [Header("Enemy Prefabs")] 
    [SerializeField] private GameObject enemyBasic;
    [SerializeField] private GameObject enemyFast;
    [SerializeField] private GameObject enemyTank;

    private List<EnemySpawner> enemySpawners;
    private int waveIndex;
    private bool waveTimerEnabled;
    private bool makingNextWave;
    public bool gameBegan;

    private void Awake()
    {
        enemySpawners = new List<EnemySpawner>(FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None));
        uiBase = FindFirstObjectByType<UIBase>();
    }

    private void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.T)) ActivateWaveManager(); // This is for testing purposes specifically and is not how the game starts a wave

        if (gameBegan == false) return;

        HandleWaveTimer();
    }
    

    private void ActivateWaveManager()
    {
        gameBegan = true;
        EnableWaveTimer(true);
    }
    
    public void CheckIfWaveCompleted()
    {
        if (gameBegan == false) return;

        if (AllEnemiesDefeated() == false || makingNextWave) return;

        makingNextWave = true;
        waveIndex++;

        if (HasNoMoreWaves()) return;
        
        EnableWaveTimer(true);
    }

    private void EnableWaveTimer(bool enable)
    {
        if (waveTimerEnabled == enable) return;

        waveTimer = timeBetweenWaves;
        waveTimerEnabled = enable;
    }
    
    private void HandleWaveTimer()
    {
        if (waveTimerEnabled == false) return;

        waveTimer -= Time.deltaTime;
        uiBase.UpdateWaveTimerUI(waveTimer);

        if (waveTimer <= 0) StartNewWave();
    }

    private void StartNewWave()
    {
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
    
    private bool HasNoMoreWaves() => waveIndex >= levelWaves.Length;
}
