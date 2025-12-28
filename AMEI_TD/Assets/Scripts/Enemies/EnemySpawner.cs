using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private WaveManager myWaveManager;
    [SerializeField] private List<EnemyPath> myPaths;
    [SerializeField] private float spawnCooldown;
    private float spawnTimer;
    private bool canCreateEnemies = true;
    [SerializeField] private Transform spawnLocation;

    private List<Vector3[]> allPathWaypoints = new List<Vector3[]>();

    private List<GameObject> enemiesToCreate = new List<GameObject>();
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        CollectAllWaypoints();
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

        PoolGameObjectType poolType = GetEnemyTypeForPooling(randomEnemy);
        Debug.Log($"Requesting from pool: {poolType}");
    
        GameObject newEnemy = ObjectPooling.instance.GetPoolObject(poolType);
        Debug.Log($"Got from pool: {(newEnemy != null ? newEnemy.name : "NULL")}");
    
        if (newEnemy != null)
        {
            newEnemy.SetActive(true);
            newEnemy.transform.position = spawnLocation.position;
            newEnemy.transform.rotation = Quaternion.identity;

            EnemyBase enemyScript = newEnemy.GetComponent<EnemyBase>();
            Debug.Log($"EnemyBase on spawned object: {(enemyScript != null ? "Found" : "NULL")}");
        
            Vector3[] randomPath = GetRandomPathWaypoints();
            Debug.Log($"Path waypoints: {(randomPath != null ? randomPath.Length.ToString() : "NULL")}");
        
            enemyScript.SetupEnemy(this, randomPath);

            activeEnemies.Add(newEnemy);
        }
    }

    private PoolGameObjectType GetEnemyTypeForPooling(GameObject randomEnemy)
    {
        if (randomEnemy.GetComponent<EnemyBase>() != null)
        {
            EnemyBase enemy = randomEnemy.GetComponent<EnemyBase>();
            EnemyType enemyType = enemy.GetEnemyType();

            switch (enemyType)
            {
                case EnemyType.Basic:
                    return PoolGameObjectType.EnemyBasic;
                case EnemyType.Fast:
                    return PoolGameObjectType.EnemyFast;
                case EnemyType.Tank:
                    return PoolGameObjectType.EnemyTank;
                case EnemyType.Invisible:
                    return PoolGameObjectType.EnemyInvisible;
                case EnemyType.Reinforced:
                    return PoolGameObjectType.EnemyReinforced;
                case EnemyType.Summoner:
                    return PoolGameObjectType.EnemySummoner;
                case EnemyType.Minion:
                    return PoolGameObjectType.EnemyMinion;
            }
        }
        return PoolGameObjectType.EnemyBasic;
    }

    private GameObject GetRandomEnemy()
    {
        if (enemiesToCreate.Count == 0) return null;

        int randomIndex = Random.Range(0, enemiesToCreate.Count);
        GameObject chosenEnemy = enemiesToCreate[randomIndex];

        enemiesToCreate.RemoveAt(randomIndex);

        return chosenEnemy;
    }

    private void CollectAllWaypoints()
    {
        allPathWaypoints.Clear();

        foreach (EnemyPath path in myPaths)
        {
            List<Transform> waypointTransforms = new List<Transform>();

            foreach (Transform child in path.GetWaypoints())
            {
                if (child != null) waypointTransforms.Add(child);
            }

            Vector3[] waypoints = new Vector3[waypointTransforms.Count];
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypoints[i] = waypointTransforms[i].position;
            }

            allPathWaypoints.Add(waypoints);
        }
    }

    private Vector3[] GetRandomPathWaypoints()
    {
        if (allPathWaypoints.Count == 0) return null;

        int randomIndex = Random.Range(0, allPathWaypoints.Count);
        return allPathWaypoints[randomIndex];
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