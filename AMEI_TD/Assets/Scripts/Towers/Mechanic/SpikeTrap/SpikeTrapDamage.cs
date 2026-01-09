using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spike trap mechanism spawned by TowerSpikeTrap on the road.
/// Raises spikes when enemies are in range, deals damage, then lowers and waits for cooldown.
/// 
/// Supports upgrades: poison DoT, bleed DoT with crit, and crippling slow.
/// Note: Bleed replaces poison (mutually exclusive).
/// </summary>
public class SpikeTrapDamage : MonoBehaviour
{
    // ==================== ANIMATION ====================
    [Header("Animation")]
    [SerializeField] private Transform spikesTransform;  // Spikes that move up/down
    [SerializeField] private float raisedHeight = 0.5f;  // How high spikes raise
    [SerializeField] private float raiseSpeed = 5f;      // Speed to raise spikes
    [SerializeField] private float lowerSpeed = 2f;      // Speed to lower spikes
    
    // ==================== TIMING ====================
    [Header("Timing")]
    [SerializeField] private float trapActiveDuration = 1f;  // How long spikes stay raised
    [SerializeField] private float spikeDelay = 0.3f;        // Delay before spikes raise (for hammer anim)
    [SerializeField] private float damageDelay = 0.1f;       // Delay after raising before damage
    
    // ==================== AUDIO ====================
    [Header("Audio")]
    [SerializeField] private AudioClip spikeImpactSound;
    [SerializeField] [Range(0f, 1f)] private float spikeImpactVolume = 1f;
    
    // ==================== VFX ====================
    [Header("VFX Points")]
    [SerializeField] private Transform[] spikeVFXPoints;  // Points for DoT VFX attachment

    private List<GameObject> activeVFXInstances = new List<GameObject>();
    
    // ==================== RUNTIME STATE ====================
    private BoxCollider boxCollider;
    private LayerMask whatIsEnemy;
    private DamageInfo damageInfo;
    private float cooldown;
    private bool isOnCooldown = false;
    private bool spikesAreRaised = false;
    private Vector3 loweredPosition;
    private Vector3 raisedPosition;
    private Collider[] detectedEnemies = new Collider[20];  // Preallocated for OverlapBox
    private TowerSpikeTrap tower;
    
    // ==================== POISON UPGRADE ====================
    private bool applyPoison = false;
    private DamageInfo poisonDamageInfo;
    private float poisonDuration;
    private GameObject poisonVFX;

    // ==================== BLEED UPGRADE ====================
    private bool applyBleed = false;
    private DamageInfo bleedDamageInfo;
    private float bleedDuration;
    private GameObject bleedVFX;
    private bool hasCrit = false;
    private float critChance;
    private float critMultiplier;

    // ==================== CRIPPLE UPGRADE ====================
    private bool applyCripple = false;
    private float slowPercent;
    private float slowDuration;
    private GameObject crippleVFX;

    // Legacy VFX references (not actively used but kept for compatibility)
    private GameObject activePoisonVFX;
    private GameObject activeBleedVFX;
    private GameObject activeCrippleVFX;
    
    /// <summary>
    /// Configures the spike trap with damage and timing parameters.
    /// Called by TowerSpikeTrap after instantiation.
    /// </summary>
    public void Setup(DamageInfo newDamageInfo, LayerMask enemyLayer, float trapCooldown, TowerSpikeTrap ownerTower)
    {
        damageInfo = newDamageInfo;
        whatIsEnemy = enemyLayer;
        cooldown = trapCooldown;
        tower = ownerTower;
        boxCollider = GetComponent<BoxCollider>();
        
        // Cache lowered and raised positions
        if (spikesTransform != null)
        {
            loweredPosition = spikesTransform.localPosition;
            raisedPosition = loweredPosition + Vector3.up * raisedHeight;
        }
    }
    
    /// <summary>
    /// Updates damage info when tower damage upgrades change.
    /// </summary>
    public void UpdateDamageInfo(DamageInfo newDamageInfo)
    {
        damageInfo = newDamageInfo;
    }
    
    /// <summary>
    /// Enables poison DoT effect on spike hits.
    /// </summary>
    public void SetPoisonEffect(float damage, float duration, ElementType elementType, GameObject vfx = null)
    {
        applyPoison = true;
        poisonDamageInfo = new DamageInfo(damage, elementType, true);
        poisonDuration = duration;
        poisonVFX = vfx;
    
        SpawnVFXOnSpikes(poisonVFX);
    }

    /// <summary>
    /// Enables bleed DoT effect with optional crit chance.
    /// </summary>
    public void SetBleedEffect(float damage, float duration, ElementType elementType, GameObject vfx = null, float crit = 0f, float critMult = 2f)
    {
        applyBleed = true;
        bleedDamageInfo = new DamageInfo(damage, elementType, true);
        bleedDuration = duration;
        bleedVFX = vfx;
        hasCrit = crit > 0f;
        critChance = crit;
        critMultiplier = critMult;
    
        SpawnVFXOnSpikes(bleedVFX);
    }

    /// <summary>
    /// Spawns VFX on all spike points (for visual DoT indication).
    /// </summary>
    private void SpawnVFXOnSpikes(GameObject vfxPrefab)
    {
        if (vfxPrefab == null || spikeVFXPoints == null) return;

        foreach (Transform point in spikeVFXPoints)
        {
            // Spawn VFX parented to spike point (-1 duration = permanent until returned)
            GameObject vfx = ObjectPooling.instance.GetVFXWithParent(vfxPrefab, point, -1f);
            vfx.transform.localPosition = Vector3.zero;
            activeVFXInstances.Add(vfx);
        }
    }

    /// <summary>
    /// Enables crippling slow effect on spike hits.
    /// </summary>
    public void SetCrippleEffect(float percent, float duration)
    {
        applyCripple = true;
        slowPercent = percent;
        slowDuration = duration;
    }
    
    private void Update()
    {
        if (isOnCooldown) return;
        
        // Don't activate if tower is disabled
        if (tower != null && tower.IsDisabled()) return;
    
        // Reset spikes if stuck up with no enemies
        if (spikesAreRaised && !HasEnemiesInRange())
        {
            StartCoroutine(ResetSpikes());
            return;
        }
    
        // Trigger trap if enemies in range
        if (HasEnemiesInRange())
        {
            StartCoroutine(TrapCycle());
        }
    }
    
    /// <summary>
    /// Checks if any targetable enemies are within the trap's box collider.
    /// Uses preallocated array to avoid GC.
    /// </summary>
    private bool HasEnemiesInRange()
    {
        int enemyCount = Physics.OverlapBoxNonAlloc(
            transform.position + boxCollider.center,
            boxCollider.size / 2,
            detectedEnemies,
            transform.rotation,
            whatIsEnemy
        );
        
        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i] != null && detectedEnemies[i].gameObject.activeSelf)
            {
                EnemyBase enemy = detectedEnemies[i].GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsTargetable())
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Full trap activation cycle:
    /// 1. Trigger hammer animation
    /// 2. Wait for spike delay
    /// 3. Raise spikes
    /// 4. Deal damage after short delay
    /// 5. Wait active duration
    /// 6. Lower spikes
    /// 7. Wait cooldown
    /// </summary>
    private IEnumerator TrapCycle()
    {
        isOnCooldown = true;
        
        // Trigger hammer animation on tower character
        if (tower != null)
        {
            tower.PlayHammerAnimation();
        }
        
        yield return new WaitForSeconds(spikeDelay);
        
        // Spawn hammer impact VFX
        if (tower != null)
        {
            tower.SpawnHammerImpactVFX();
        }
        
        // Raise spikes
        StartCoroutine(MoveSpikes(raisedPosition, raiseSpeed));
        spikesAreRaised = true;
        
        // Short delay then damage
        yield return new WaitForSeconds(damageDelay);
        DamageEnemiesInRange();
        
        // Keep spikes raised for duration
        yield return new WaitForSeconds(trapActiveDuration);
        
        // Lower spikes
        yield return StartCoroutine(MoveSpikes(loweredPosition, lowerSpeed));
        spikesAreRaised = false;
        
        // Wait cooldown (adjusted for slow effects on tower)
        float effectiveCooldown = cooldown;
        if (tower != null)
        {
            effectiveCooldown = cooldown / tower.GetSlowMultiplier();
        }
        yield return new WaitForSeconds(effectiveCooldown);
        
        isOnCooldown = false;
    }
    
    /// <summary>
    /// Emergency reset when spikes are up but no enemies present.
    /// </summary>
    private IEnumerator ResetSpikes()
    {
        isOnCooldown = true;
        yield return StartCoroutine(MoveSpikes(loweredPosition, lowerSpeed));
        spikesAreRaised = false;
        
        // Wait cooldown
        float effectiveCooldown = cooldown;
        if (tower != null)
        {
            effectiveCooldown = cooldown / tower.GetSlowMultiplier();
        }
        yield return new WaitForSeconds(effectiveCooldown);
        isOnCooldown = false;
    }
    
    /// <summary>
    /// Deals damage to all enemies in range and applies DoT/slow effects.
    /// </summary>
    private void DamageEnemiesInRange()
    {
        int enemyCount = Physics.OverlapBoxNonAlloc(
            transform.position + boxCollider.center,
            boxCollider.size / 2,
            detectedEnemies,
            transform.rotation,
            whatIsEnemy
        );
        
        bool hitAnyEnemy = false;
    
        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i] != null && detectedEnemies[i].gameObject.activeSelf)
            {
                hitAnyEnemy = true;
                
                IDamageable damageable = detectedEnemies[i].GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // ===== CRIT CHECK =====
                    DamageInfo finalDamage = damageInfo;
                    if (hasCrit && Random.value <= critChance)
                    {
                        finalDamage = new DamageInfo(damageInfo.amount * critMultiplier, damageInfo.elementType);
                    }
                
                    damageable.TakeDamage(finalDamage);
                }
            
                EnemyBase enemy = detectedEnemies[i].GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    // ===== DOT EFFECTS (bleed replaces poison) =====
                    if (applyBleed)
                    {
                        enemy.ApplyDoT(bleedDamageInfo, bleedDuration, 0.5f, false, 0f, default, DebuffType.Bleed);
                    }
                    else if (applyPoison)
                    {
                        enemy.ApplyDoT(poisonDamageInfo, poisonDuration, 0.5f, false, 0f, default, DebuffType.Poison);
                    }
                
                    // ===== SLOW EFFECT =====
                    if (applyCripple)
                    {
                        enemy.ApplySlow(slowPercent, slowDuration, false);
                    }
                }
            }
        }
        
        // Play impact sound if any enemy was hit
        if (hitAnyEnemy && spikeImpactSound != null && SFXPlayer.instance != null)
        {
            SFXPlayer.instance.Play(spikeImpactSound, transform.position, spikeImpactVolume);
        }
    }
    
    /// <summary>
    /// Smoothly moves spikes to target position at given speed.
    /// </summary>
    private IEnumerator MoveSpikes(Vector3 targetPosition, float speed)
    {
        while (Vector3.Distance(spikesTransform.localPosition, targetPosition) > 0.01f)
        {
            spikesTransform.localPosition = Vector3.MoveTowards(
                spikesTransform.localPosition,
                targetPosition,
                speed * Time.deltaTime
            );
            yield return null;
        }
        spikesTransform.localPosition = targetPosition;
    }
    
    public void SetCooldown(float newCooldown)
    {
        cooldown = newCooldown;
    }

    /// <summary>
    /// Clears all DoT effects and their VFX.
    /// </summary>
    public void ClearDoTEffects()
    {
        applyPoison = false;
        applyBleed = false;
        hasCrit = false;
    
        // Return all VFX to pool
        foreach (GameObject vfx in activeVFXInstances)
        {
            if (vfx != null)
            {
                ObjectPooling.instance.Return(vfx);
            }
        }
        activeVFXInstances.Clear();
    }

    public void ClearCrippleEffect()
    {
        applyCripple = false;
    }
}