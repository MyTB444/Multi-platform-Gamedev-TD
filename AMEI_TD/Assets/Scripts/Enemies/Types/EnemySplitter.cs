using System.Collections;
using UnityEngine;

public class EnemySplitter : EnemyBase
{
    [Header("Split Settings")]
    [SerializeField] private GameObject splitVFXPrefab;
    [SerializeField] private GameObject splitterPrefab; // Reference to own prefab for pooling
    [SerializeField] private int maxSplitLevel = 2;
    [SerializeField] private int currentSplitLevel = 0;
    [SerializeField] private float splitDelayAfterDeath = 0.2f;

    [Header("Split Stats Multipliers")]
    [SerializeField] private float healthMultiplierPerSplit = 0.5f;
    [SerializeField] private float scaleMultiplierPerSplit = 0.7f;
    [SerializeField] private float speedMultiplierPerSplit = 1.2f;
    [SerializeField] private int splitCount = 2;

    private Vector3 originalScale;
    private float originalMaxHp;
    private bool hasStoredOriginals = false;

    protected override void Awake()
    {
        base.Awake(); // Capture layer and base speed
    
        if (!hasStoredOriginals)
        {
            originalScale = transform.localScale;
            originalMaxHp = enemyMaxHp;
            hasStoredOriginals = true;
        }
    }

    public override void TakeDamage(DamageInfo damageInfo)
    {
        float hpBeforeDamage = enemyCurrentHp;
        bool wasAlive = !isDead;
        bool canSplit = currentSplitLevel < maxSplitLevel;

        base.TakeDamage(damageInfo);

        bool justDied = wasAlive && isDead && hpBeforeDamage > 0;

        if (justDied && canSplit)
        {
            SpawnSplits();
        }
    }

    private void SpawnSplits()
    {
        if (splitVFXPrefab != null)
        {
            ObjectPooling.instance.GetVFX(splitVFXPrefab, transform.position, Quaternion.identity, 2f);
        }

        for (int i = 0; i < splitCount; i++)
        {
            SpawnSplitEnemy(i);
        }

        Debug.Log($"[EnemySplitter] Spawned {splitCount} splits at level {currentSplitLevel + 1}");
    }

    private void SpawnSplitEnemy(int index)
    {
        if (splitterPrefab == null)
        {
            Debug.LogError("[EnemySplitter] splitterPrefab is not assigned! Cannot spawn splits.");
            return;
        }

        Vector3 spawnOffset = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0,
            Random.Range(-0.5f, 0.5f)
        );
        Vector3 spawnPosition = transform.position + spawnOffset;

        GameObject newEnemy = ObjectPooling.instance.Get(splitterPrefab);
        if (newEnemy == null) return;

        newEnemy.transform.position = spawnPosition;
        newEnemy.transform.rotation = transform.rotation;
        newEnemy.SetActive(true);
    
        // Get remaining waypoints
        Vector3[] remainingWaypoints = GetRemainingWaypoints();
    
        EnemySplitter splitterScript = newEnemy.GetComponent<EnemySplitter>();
    
        // Call SetupEnemy first (this resets everything)
        if (splitterScript != null && remainingWaypoints.Length > 0)
        {
            splitterScript.SetupEnemy(mySpawner, remainingWaypoints);
        
            // Set split level AFTER SetupEnemy (since ResetEnemy resets it to 0)
            splitterScript.currentSplitLevel = currentSplitLevel + 1;
        
            // Set stats AFTER SetupEnemy
            float newMaxHp = enemyMaxHp * healthMultiplierPerSplit;
            float newScale = originalScale.x * Mathf.Pow(scaleMultiplierPerSplit, currentSplitLevel + 1);
        
            splitterScript.enemyMaxHp = newMaxHp;
            splitterScript.enemyCurrentHp = newMaxHp;
            newEnemy.transform.localScale = Vector3.one * newScale;
        }

        // Add to spawner's active enemies list
        if (mySpawner != null)
        {
            mySpawner.GetActiveEnemies().Add(newEnemy);
        }

        Debug.Log($"[EnemySplitter] Spawned split at level {currentSplitLevel + 1}, maxSplitLevel is {maxSplitLevel}");
    }
    
    private Vector3[] GetRemainingWaypoints()
    {
        if (myWaypoints == null) return new Vector3[0];
    
        int remaining = myWaypoints.Length - currentWaypointIndex;
        if (remaining <= 0) return new Vector3[0];

        Vector3[] remainingWaypoints = new Vector3[remaining];
        for (int i = 0; i < remaining; i++)
        {
            remainingWaypoints[i] = myWaypoints[currentWaypointIndex + i];
        }

        return remainingWaypoints;
    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();

        // Reset split level back to 0
        currentSplitLevel = 0;

        // Reset scale and max HP to original values
        if (hasStoredOriginals && originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
            enemyMaxHp = originalMaxHp;
        }
    }
}
