using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy that splits into smaller copies when killed, up to a max split level.
/// </summary>
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

    public override void TakeDamage(DamageInfo damageInfo,float vfxDamage = 0,bool spellDamageEnabled = false)
    {
        // Check if death will trigger split before applying damage
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

    /// <summary>
    /// Spawns the configured number of split enemies when this enemy dies.
    /// Plays split VFX at the death location.
    /// </summary>
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

    /// <summary>
    /// Spawns a single split enemy with reduced stats and increased split level.
    /// The split is smaller, faster, and weaker than the parent, and continues from the same path.
    /// </summary>
    /// <param name="index">Index of this split (used for offset calculation)</param>
    private void SpawnSplitEnemy(int index)
    {
        // Create smaller, faster copy with reduced HP at next split level
        if (splitterPrefab == null) return;

        Vector3 spawnOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        Vector3 spawnPosition = transform.position + spawnOffset;

        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
        }
        else
        {
            spawnPosition = transform.position;
        }

        GameObject newEnemy = ObjectPooling.instance.Get(splitterPrefab);
        if (newEnemy == null) return;

        newEnemy.transform.position = spawnPosition;
        newEnemy.transform.rotation = transform.rotation;
        newEnemy.SetActive(true);

        Vector3[] remainingWaypoints = GetRemainingWaypoints();

        EnemySplitter splitterScript = newEnemy.GetComponent<EnemySplitter>();

        if (splitterScript != null && remainingWaypoints.Length > 0)
        {
            splitterScript.SetupEnemyNoGrace(remainingWaypoints);  // Use NoGrace version
            splitterScript.mySpawner = mySpawner;

            splitterScript.currentSplitLevel = currentSplitLevel + 1;

            float newMaxHp = enemyMaxHp * healthMultiplierPerSplit;
            float newScale = originalScale.x * Mathf.Pow(scaleMultiplierPerSplit, currentSplitLevel + 1);

            splitterScript.enemyMaxHp = newMaxHp;
            splitterScript.enemyCurrentHp = newMaxHp;
            newEnemy.transform.localScale = Vector3.one * newScale;

            UnityEngine.AI.NavMeshAgent agent = newEnemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.radius = 2.5f * Mathf.Pow(scaleMultiplierPerSplit, currentSplitLevel + 1);
            }
        }

        if (mySpawner != null)
        {
            mySpawner.GetActiveEnemies().Add(newEnemy);
        }
    }
    
    /// <summary>
    /// Returns the remaining waypoints from the current position to the end of the path.
    /// Splits continue from where the parent died, following the same route.
    /// </summary>
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
