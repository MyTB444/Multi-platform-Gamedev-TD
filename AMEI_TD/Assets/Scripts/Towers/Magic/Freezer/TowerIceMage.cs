using UnityEngine;

/// <summary>
/// Magic tower that fires homing ice projectiles. Applies slow and frost DoT in an area.
/// Upgrades can increase slow strength/duration, DoT damage, or add freeze chance.
/// </summary>
public class TowerIceMage : TowerBase
{
    // ==================== PROJECTILE VISUAL ====================
    [Header("Projectile Visual")]
    [SerializeField] private Vector3 visualRotationOffset = new Vector3(90f, 0f, 0f);  // Model rotation fix
    
    // ==================== ICE EFFECTS ====================
    [Header("Ice Settings")]
    [SerializeField] private float slowPercent = 0.5f;    // 50% movement speed reduction
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float effectRadius = 2f;      // AOE radius for slow/DoT
    
    // ==================== DAMAGE OVER TIME ====================
    [Header("Damage Over Time")]
    [SerializeField] private float dotDamagePerTick = 2f;
    [SerializeField] private float dotDuration = 3f;
    [SerializeField] private float dotTickInterval = 0.5f;
    
    // ==================== UPGRADES ====================
    [Header("Ice Upgrades")]
    [SerializeField] private bool strongerSlow = false;          // Increases slow percentage
    [SerializeField] private float bonusSlowPercent = 0.15f;
    [Space]
    [SerializeField] private bool longerSlow = false;            // Increases slow duration
    [SerializeField] private float bonusSlowDuration = 1f;
    [Space]
    [SerializeField] private bool frostbite = false;             // Increases DoT damage
    [SerializeField] private float frostbiteDamageMultiplier = 2f;
    [Space]
    [SerializeField] private bool freezeSolid = false;           // Chance to stun on direct hit
    [SerializeField] [Range(0f, 1f)] private float freezeChance = 0.2f;
    [SerializeField] private float freezeDuration = 1.5f;
    
    // Target locked at attack start (prevents target switching mid-animation)
    private EnemyBase lockedTarget;
    
    /// <summary>
    /// Handles upgrade state changes from skill tree.
    /// </summary>
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.StrongerSlow:
                strongerSlow = enabled;
                break;
            case TowerUpgradeType.LongerSlow:
                longerSlow = enabled;
                break;
            case TowerUpgradeType.Frostbite:
                frostbite = enabled;
                break;
            case TowerUpgradeType.FreezeSolid:
                freezeSolid = enabled;
                break;
        }
    }
    
    /// <summary>
    /// Locks target before starting attack animation.
    /// Prevents target switching if a closer enemy appears mid-cast.
    /// </summary>
    protected override void Attack()
    {
        lockedTarget = currentEnemy;
        base.Attack();
    }
    
    /// <summary>
    /// Spawns ice projectile with all calculated effects.
    /// Called by Animation Event.
    /// </summary>
    protected override void FireProjectile()
    {
        // Spawn muzzle VFX
        if (attackSpawnEffectPrefab != null && gunPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, gunPoint.position, Quaternion.identity, 0.5f);
        }
    
        if (projectilePrefab == null || gunPoint == null) return;
        
        // ===== VALIDATE LOCKED TARGET =====
        if (lockedTarget == null || !lockedTarget.gameObject.activeSelf)
        {
            lockedTarget = null;
            return;
        }
    
        IDamageable damageable = lockedTarget.GetComponent<IDamageable>();
        if (damageable == null)
        {
            lockedTarget = null;
            return;
        }
    
        // ===== CALCULATE SPAWN POSITION/ROTATION =====
        Vector3 targetPos = lockedTarget.GetCenterPoint();
    
        Vector3 directionToEnemy = (targetPos - gunPoint.position).normalized;
        Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.5f;  // Spawn slightly forward
    
        // Apply visual rotation offset for projectile model orientation
        Quaternion flightRotation = Quaternion.LookRotation(directionToEnemy);
        Quaternion spawnRotation = flightRotation * Quaternion.Euler(visualRotationOffset);
    
        // ===== SPAWN PROJECTILE =====
        GameObject newProjectile = ObjectPooling.instance.Get(projectilePrefab);
        newProjectile.transform.position = spawnPosition;
        newProjectile.transform.rotation = spawnRotation;
        newProjectile.SetActive(true);
    
        // ===== CALCULATE FINAL VALUES WITH UPGRADES =====
        float finalSlowPercent = slowPercent + (strongerSlow ? bonusSlowPercent : 0f);
        float finalSlowDuration = slowDuration + (longerSlow ? bonusSlowDuration : 0f);
        float finalDotDamage = dotDamagePerTick * (frostbite ? frostbiteDamageMultiplier : 1f);
    
        // ===== CONFIGURE PROJECTILE =====
        IceProjectile ice = newProjectile.GetComponent<IceProjectile>();
        if (ice != null)
        {
            ice.SetupIceProjectile(
                lockedTarget.transform, 
                damageable, 
                CreateDamageInfo(), 
                projectileSpeed, 
                whatIsEnemy,
                finalSlowPercent,
                finalSlowDuration,
                effectRadius,
                finalDotDamage,
                dotDuration,
                dotTickInterval,
                visualRotationOffset
            );
        
            // Add freeze effect if upgraded
            if (freezeSolid)
            {
                ice.SetFreezeEffect(freezeChance, freezeDuration);
            }
        }
        
        // Clear locked target for next attack
        lockedTarget = null;
    }
}