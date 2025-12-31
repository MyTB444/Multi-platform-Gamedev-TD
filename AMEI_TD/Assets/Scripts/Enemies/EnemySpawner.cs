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
            // Faster spawns during mega wave
            float cooldown = spawnCooldown;
            if (myWaveManager != null && myWaveManager.IsMegaWaveActive())
            {
                cooldown /= myWaveManager.GetMegaWaveSpawnRateMultiplier();
            }
        
            spawnTimer = cooldown;
            return true;
        }

        return false;
    }

    private void CreateEnemy()
    {
        if (!canCreateEnemies) return;

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

            // Draw arrow head
            Vector3 right = Quaternion.Euler(0, 30, 0) * -forward.normalized * 0.5f;
            Vector3 left = Quaternion.Euler(0, -30, 0) * -forward.normalized * 0.5f;
            Gizmos.DrawLine(spawnLocation.position + forward, spawnLocation.position + forward + right);
            Gizmos.DrawLine(spawnLocation.position + forward, spawnLocation.position + forward + left);
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
