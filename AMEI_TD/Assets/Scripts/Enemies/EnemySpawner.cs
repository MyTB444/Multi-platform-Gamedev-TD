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
    [SerializeField] private Transform spawnLocation;

    [SerializeField] public List<Transform> waypointList;

    public Vector3[] currentWaypoints { get; private set; }

    private List<GameObject> enemiesToCreate = new List<GameObject>();
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        CollectWaypoints();
        myWaveManager = FindFirstObjectByType<WaveManager>();
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

        GameObject newEnemy = Instantiate(randomEnemy, spawnLocation.position, Quaternion.identity);

        EnemyBase enemyScript = newEnemy.GetComponent<EnemyBase>();
        enemyScript.SetupEnemy(this);

        activeEnemies.Add(newEnemy);
    }

    // Picks and removes a random enemy prefab from the queue
    private GameObject GetRandomEnemy()
    {
        if (enemiesToCreate.Count == 0) return null;

        int randomIndex = Random.Range(0, enemiesToCreate.Count);
        GameObject chosenEnemy = enemiesToCreate[randomIndex];

        enemiesToCreate.RemoveAt(randomIndex);

        return chosenEnemy;
    }

    // Converts waypoint transforms to Vector3 positions for enemy pathfinding
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
    public bool HasEnemiesToSpawn() => enemiesToCreate.Count > 0;
    public void CanCreateNewEnemies(bool canCreate) => canCreateEnemies = canCreate;

    public void RemoveActiveEnemy(GameObject enemyToRemove)
    {
        if (activeEnemies != null && activeEnemies.Contains(enemyToRemove))
        {
            activeEnemies.Remove(enemyToRemove);
        }

        myWaveManager.CheckIfWaveCompleted();
    }
}