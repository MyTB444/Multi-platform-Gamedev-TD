using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private WaveManager myWaveManager;
    [SerializeField] private EnemyPath myPath;
    [SerializeField] private float spawnCooldown;
    private float spawnTimer;
    private bool canCreateEnemies = true;

    [SerializeField] public List<Transform> waypointList;

    public Vector3[] currentWaypoints { get; private set; }
    
    private List<GameObject> enemiesToCreate = new List<GameObject>();
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        CollectWaypoints();
    }

    private void Update()
    {
        if (CanMakeNewEnemy()) CreateEnemy();
    }
    
    private bool CanMakeNewEnemy()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0 && enemiesToCreate.Count > 0)
        {
            spawnTimer = spawnCooldown;
            return true;
        }

        return false;
    }

    private void CreateEnemy()
    {
        if (!canCreateEnemies) return;

        GameObject randomEnemy = GetRandomEnemy();
        if (randomEnemy == null) return;
        
        GameObject newEnemy = Instantiate(randomEnemy, transform.position, Quaternion.identity);

        EnemyBase enemyScript = newEnemy.GetComponent<EnemyBase>();
        enemyScript.SetupEnemy(this);
        
        activeEnemies.Add(newEnemy);
    }

    private GameObject GetRandomEnemy()
    {
        if (enemiesToCreate.Count == 0) return null;

        int randomIndex = Random.Range(0, enemiesToCreate.Count);
        GameObject chosenEnemy = enemiesToCreate[randomIndex];

        enemiesToCreate.RemoveAt(randomIndex);

        return chosenEnemy;
    }

    private void CollectWaypoints()
    {
        waypointList = new List<Transform>();
        foreach (Transform child in myPath.GetWaypoints())
        {
            if (child != null) waypointList.Add(child);
        }
        
        currentWaypoints = new Vector3[waypointList.Count];

        for (int i = 0; i < currentWaypoints.Length; i++)
        {
            currentWaypoints[i] = waypointList[i].transform.position;
        }
    }

    public void AddEnemy(GameObject enemyToAdd) => enemiesToCreate.Add(enemyToAdd);
    public List<GameObject> GetActiveEnemies() => activeEnemies;
    
    public void RemoveActiveEnemy(GameObject enemyToRemove)
    {
        if (activeEnemies != null && activeEnemies.Contains(enemyToRemove))
        {
            activeEnemies.Remove(enemyToRemove);
        }
        
        myWaveManager.CheckIfWaveCompleted();
    }
}
