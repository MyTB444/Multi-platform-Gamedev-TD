using UnityEngine;

/// <summary>
/// Fire magic tower that shoots homing fireballs. Supports burn DoT, AOE splash, and burn spread.
/// Uses HomingProjectile for tracking enemies.
/// </summary>
public class TowerPyromancer : TowerBase
{
    // ==================== UPGRADES ====================
    [Header("Pyromancer Effects")]
    [SerializeField] private bool burnChance = false;                    // Enables burn DoT
    [SerializeField] [Range(0f, 1f)] private float burnChancePercent = 0.3f;  // 30% chance
    [SerializeField] private float burnDamage = 3f;                      // Damage per tick
    [SerializeField] private float burnDuration = 3f;
    [Space]
    [SerializeField] private bool biggerFireball = false;                // Enables AOE splash
    [SerializeField] private float fireballScaleMultiplier = 1.5f;       // Visual scale increase
    [SerializeField] private float fireballAoERadius = 1.5f;
    [SerializeField] private float fireballAoEDamagePercent = 0.5f;      // 50% of main damage
    [Space]
    [SerializeField] private bool burnSpread = false;                    // Burn spreads to nearby enemies
    [SerializeField] private float burnSpreadRadius = 2f;
    
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
            case TowerUpgradeType.BurnChance:
                burnChance = enabled;
                break;
            case TowerUpgradeType.BiggerFireball:
                biggerFireball = enabled;
                break;
            case TowerUpgradeType.BurnSpread:
                burnSpread = enabled;
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
    /// Spawns homing fireball with configured effects.
    /// Called by Animation Event.
    /// </summary>
    protected override void FireProjectile()
    {
        // Spawn muzzle flash VFX
        if (attackSpawnEffectPrefab != null && gunPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, gunPoint.position, Quaternion.identity, 0.1f);
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
        Quaternion spawnRotation = Quaternion.LookRotation(directionToEnemy);

        // ===== SPAWN PROJECTILE =====
        GameObject newProjectile = ObjectPooling.instance.Get(projectilePrefab);
        newProjectile.transform.position = spawnPosition;
        newProjectile.transform.rotation = spawnRotation;
        newProjectile.SetActive(true);

        // ===== CONFIGURE PROJECTILE =====
        HomingProjectile homing = newProjectile.GetComponent<HomingProjectile>();
        if (homing != null)
        {
            // Basic homing setup
            homing.SetupHomingProjectile(lockedTarget.transform, damageable, CreateDamageInfo(), projectileSpeed, whatIsEnemy);

            // Add burn effect if upgraded
            if (burnChance)
            {
                homing.SetBurnEffect(burnChancePercent, burnDamage, burnDuration, elementType, burnSpread, burnSpreadRadius, whatIsEnemy);
            }
    
            // Add AOE and scale if upgraded
            if (biggerFireball)
            {
                newProjectile.transform.localScale *= fireballScaleMultiplier;
                homing.SetAoEEffect(fireballAoERadius, fireballAoEDamagePercent, whatIsEnemy);
            }
        }
        
        // Clear locked target for next attack
        lockedTarget = null;
    }
}