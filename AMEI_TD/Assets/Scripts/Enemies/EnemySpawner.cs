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
    
    [Header("Spawn Area Check")]
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(2f, 2f, 3f);
    [SerializeField] private List<EnemyType> bigEnemyTypes = new List<EnemyType>();
    [SerializeField] private LayerMask enemyLayer;

    private bool waitingForAreaClear = false;

    private void Start()
    {
        // Initialize waypoint paths and wave manager reference
        CollectAllWaypoints();
        myWaveManager = FindFirstObjectByType<WaveManager>();
    }

    private void Update()
    {
        if (CanMakeNewEnemy()) CreateEnemy();
    }
    
    private bool IsSpawnAreaBlocked()
    {
        // Check if spawn area is blocked by big enemies to prevent overlap
        if (!waitingForAreaClear) return false;

        Vector3 checkCenter = spawnLocation.position;
        Quaternion checkRotation = spawnLocation.rotation;

        if (allPathWaypoints.Count > 0 && allPathWaypoints[0].Length > 0)
        {
            Vector3 roadDirection = (allPathWaypoints[0][0] - spawnLocation.position).normalized;
            checkCenter += roadDirection * (spawnAreaSize.z * 0.5f);
            checkRotation = Quaternion.LookRotation(roadDirection);
        }
    
        Collider[] enemies = Physics.OverlapBox(checkCenter, spawnAreaSize * 0.5f, checkRotation, enemyLayer);
    
        Debug.Log($"[SpawnCheck] Found {enemies.Length} colliders in box");
    
        foreach (Collider col in enemies)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                Debug.Log($"[SpawnCheck] Enemy: {enemy.name} | Type: {enemy.GetEnemyType()} | IsBigType: {bigEnemyTypes.Contains(enemy.GetEnemyType())}");
            
                if (bigEnemyTypes.Contains(enemy.GetEnemyType()))
                {
                    return true;
                }
            }
            else
            {
                Debug.Log($"[SpawnCheck] Collider {col.name} has no EnemyBase");
            }
        }
    
        Debug.Log("[SpawnCheck] Area clear, resuming spawns");
        waitingForAreaClear = false;
        return false;
    }

    private bool CanMakeNewEnemy()
    {
        // Check if spawn timer is ready and apply wave difficulty multipliers
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0 && enemiesToCreate.Count > 0)
        {
            float cooldown = spawnCooldown;

            if (myWaveManager != null)
            {
                if (myWaveManager.IsMegaWaveActive())
                {
                    cooldown /= myWaveManager.GetMegaWaveSpawnRateMultiplier();
                }
                else
                {
                    float tierMultiplier = myWaveManager.GetCurrentSpawnRateMultiplier();
                    if (tierMultiplier > 0)
                    {
                        cooldown /= tierMultiplier;
                    }
                }
            }

            spawnTimer = cooldown;
            return true;
        }

        return false;
    }

    private void CreateEnemy()
    {
        // Spawn enemy from pool, orient toward path, and setup with random waypoint path
        if (!canCreateEnemies) return;
        if (enemiesToCreate.Count == 0) return;

        // Wait if spawn area is blocked by big enemy
        if (IsSpawnAreaBlocked()) return;

        GameObject randomEnemyPrefab = GetRandomEnemy();
        if (randomEnemyPrefab == null) return;

        GameObject newEnemy = ObjectPooling.instance.Get(randomEnemyPrefab);
        if (newEnemy != null)
        {
            newEnemy.SetActive(true);
            newEnemy.transform.position = spawnLocation.position;

            if (allPathWaypoints.Count > 0 && allPathWaypoints[0].Length > 0)
            {
                Vector3 directionToWaypoint = (allPathWaypoints[0][0] - spawnLocation.position).normalized;
                directionToWaypoint.y = 0;
                if (directionToWaypoint != Vector3.zero)
                {
                    newEnemy.transform.rotation = Quaternion.LookRotation(directionToWaypoint);
                }
            }

            EnemyBase enemyScript = newEnemy.GetComponent<EnemyBase>();
            Vector3[] randomPath = GetRandomPathWaypoints();
            enemyScript.SetupEnemy(this, randomPath);

            activeEnemies.Add(newEnemy);

            // If big enemy just spawned, enable area check
            if (enemyScript != null && bigEnemyTypes.Contains(enemyScript.GetEnemyType()))
            {
                waitingForAreaClear = true;
            }
        }
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
        // Convert all EnemyPath transforms to Vector3 arrays for pathfinding
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
        // Remove from active list and notify wave manager to check completion
        if (activeEnemies != null && activeEnemies.Contains(enemyToRemove))
        {
            activeEnemies.Remove(enemyToRemove);
        }

        // Check for mega wave completion if active, otherwise check normal wave
        if (myWaveManager.IsMegaWaveActive())
        {
            myWaveManager.CheckIfMegaWaveCompleted();
        }
        else
        {
            myWaveManager.CheckIfWaveCompleted();
        }
    }

    private void OnDrawGizmos()
    {
        // Draw spawn point
        if (spawnLocation != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnLocation.position, 0.5f);

            // Draw forward direction arrow
            Gizmos.color = Color.blue;
            Vector3 forward = spawnLocation.forward * 2f;
            Gizmos.DrawLine(spawnLocation.position, spawnLocation.position + forward);

            Vector3 right = Quaternion.Euler(0, 30, 0) * -forward.normalized * 0.5f;
            Vector3 left = Quaternion.Euler(0, -30, 0) * -forward.normalized * 0.5f;
            Gizmos.DrawLine(spawnLocation.position + forward, spawnLocation.position + forward + right);
            Gizmos.DrawLine(spawnLocation.position + forward, spawnLocation.position + forward + left);
            
            // Draw spawn area box
            Gizmos.color = waitingForAreaClear ? Color.red : Color.cyan;
            
            Vector3 checkCenter = spawnLocation.position;
            Quaternion checkRotation = spawnLocation.rotation;
            
            if (myPaths != null && myPaths.Count > 0 && myPaths[0] != null)
            {
                Transform[] waypoints = myPaths[0].GetWaypoints();
                if (waypoints != null && waypoints.Length > 0)
                {
                    Vector3 roadDirection = (waypoints[0].position - spawnLocation.position).normalized;
                    checkCenter += roadDirection * (spawnAreaSize.z * 0.5f);
                    checkRotation = Quaternion.LookRotation(roadDirection);
                }
            }
            
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(checkCenter, checkRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, spawnAreaSize);
            Gizmos.matrix = oldMatrix;
        }

        // Draw line to first waypoint
        if (myPaths != null && myPaths.Count > 0 && myPaths[0] != null)
        {
            Transform[] waypoints = myPaths[0].GetWaypoints();
            if (waypoints != null && waypoints.Length > 0 && spawnLocation != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(spawnLocation.position, waypoints[0].position);
            }
        }
    }
}
