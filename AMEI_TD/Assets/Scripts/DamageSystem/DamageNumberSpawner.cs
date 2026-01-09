using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton that manages spawning damage number UI elements.
/// Supports both immediate spawning and delayed/accumulated damage display
/// (useful for DoT effects to avoid spam).
/// </summary>
public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner instance;

    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);  // Offset above hit position
    [SerializeField] private float randomOffsetRange = 0.3f;                    // Random spread to prevent stacking
    [SerializeField] private int poolAmount = 50;

    // ===== DAMAGE ACCUMULATION SYSTEM =====
    // When shouldWaitForDamage is true, damage is accumulated over time
    // and displayed as a single number. Useful for DoT/rapid hits.
    private float accumulatedDamage = 0f;
    private Vector3 lastPosition = Vector3.zero;
    
    // Effectiveness flags for accumulated damage - preserves the "best" outcome
    private bool isSuperEffectiveStored = false;
    private bool isNotVeryEffectiveStored = false;
    private bool isImmuneStored = false;
    
    private Coroutine activeCoroutine = null;
    
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Pre-populate object pool with damage number instances
        if (damageNumberPrefab != null)
        {
            ObjectPooling.instance.Register(damageNumberPrefab, poolAmount);
        }
    }

    /// <summary>
    /// Spawns a damage number at the specified position.
    /// </summary>
    /// <param name="position">World position to spawn at (offset is added automatically)</param>
    /// <param name="damage">Damage value to display</param>
    /// <param name="isSuperEffective">True if 2x damage from type advantage</param>
    /// <param name="isNotVeryEffective">True if 0.5x damage from type disadvantage</param>
    /// <param name="isImmune">True if target is immune (0 damage)</param>
    /// <param name="shouldWaitForDamage">If true, accumulates damage before displaying</param>
    public void Spawn(Vector3 position, float damage, bool isSuperEffective, bool isNotVeryEffective, bool isImmune, bool shouldWaitForDamage = false)
    {
        if (damageNumberPrefab == null) return;

        if (!shouldWaitForDamage)
        {
            // Immediate spawn - used for single hits, projectile impacts, etc.
            SpawnDamageNumber(position, damage, isSuperEffective, isNotVeryEffective, isImmune);
        }
        else
        {
            // ===== ACCUMULATION MODE =====
            // Used for DoT effects, rapid multi-hits, etc.
            // Collects damage over 1.5 seconds then displays total
            
            // Track position (additive - will be averaged or used as last known)
            // Note: This adds positions together which may not be intended
            // Consider: lastPosition = position; for just using the most recent
            lastPosition += position;

            // Sum up all damage
            accumulatedDamage += damage;

            // ===== EFFECTIVENESS FLAG PRIORITY =====
            // Super Effective takes priority over Not Very Effective
            // Once we've seen a super effective hit, we show that
            if (isSuperEffective) isSuperEffectiveStored = true;
            if (isImmune) isImmuneStored = true;
            
            // Only mark as not very effective if we haven't seen super effective
            if (isNotVeryEffective && !isSuperEffectiveStored) isNotVeryEffectiveStored = true;

            // Start accumulation timer if not already running
            // Note: Missing null check - will start new coroutine every call
            // Consider: if (activeCoroutine == null) activeCoroutine = StartCoroutine(SpawnDelayed());
            activeCoroutine = StartCoroutine(SpawnDelayed());
        }
    }

    /// <summary>
    /// Waits for accumulation period then spawns combined damage number.
    /// </summary>
    private IEnumerator SpawnDelayed()
    {
        // Wait for damage to accumulate (e.g., multiple DoT ticks)
        yield return new WaitForSeconds(1.5f);

        // Spawn single number with total accumulated damage
        SpawnDamageNumber(lastPosition, accumulatedDamage, isSuperEffectiveStored, isNotVeryEffectiveStored, isImmuneStored);

        // Reset accumulation state for next batch
        accumulatedDamage = 0f;
        lastPosition = Vector3.zero;
        isSuperEffectiveStored = false;
        isNotVeryEffectiveStored = false;
        isImmuneStored = false;
        activeCoroutine = null;
    }

    /// <summary>
    /// Actually creates the damage number from the object pool.
    /// Applies random offset to prevent visual stacking.
    /// </summary>
    private void SpawnDamageNumber(Vector3 position, float damage, bool isSuperEffective, bool isNotVeryEffective, bool isImmune)
    {
        // Add randomization so multiple numbers don't overlap exactly
        // X and Z spread horizontally, Y only goes up (looks better)
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomOffsetRange, randomOffsetRange),
            Random.Range(0f, randomOffsetRange),  // Only positive Y offset
            Random.Range(-randomOffsetRange, randomOffsetRange)
        );
        
        Vector3 spawnPos = position + spawnOffset + randomOffset;

        // Get pooled instance and position it
        GameObject numberObj = ObjectPooling.instance.Get(damageNumberPrefab);
        numberObj.transform.position = spawnPos;
        numberObj.transform.rotation = Quaternion.identity;
        numberObj.SetActive(true);

        // Initialize with damage data
        DamageNumber damageNumber = numberObj.GetComponent<DamageNumber>();
        damageNumber.Setup(damage, isSuperEffective, isNotVeryEffective, isImmune);
    }
}