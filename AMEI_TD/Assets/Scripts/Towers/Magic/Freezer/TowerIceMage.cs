using UnityEngine;

public class TowerIceMage : TowerBase
{
    [Header("Projectile Visual")]
    [SerializeField] private Vector3 visualRotationOffset = new Vector3(90f, 0f, 0f);
    
    [Header("Ice Settings")]
    [SerializeField] private float slowPercent = 0.5f;
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float effectRadius = 2f;
    
    [Header("Damage Over Time")]
    [SerializeField] private float dotDamagePerTick = 2f;
    [SerializeField] private float dotDuration = 3f;
    [SerializeField] private float dotTickInterval = 0.5f;
    
    [Header("Ice Upgrades")]
    [SerializeField] private bool strongerSlow = false;
    [SerializeField] private float bonusSlowPercent = 0.15f;
    [Space]
    [SerializeField] private bool longerSlow = false;
    [SerializeField] private float bonusSlowDuration = 1f;
    [Space]
    [SerializeField] private bool frostbite = false;
    [SerializeField] private float frostbiteDamageMultiplier = 2f;
    [Space]
    [SerializeField] private bool freezeSolid = false;
    [SerializeField] [Range(0f, 1f)] private float freezeChance = 0.2f;
    [SerializeField] private float freezeDuration = 1.5f;
    
    // Locked target for attack
    private EnemyBase lockedTarget;
    
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
    
    protected override void Attack()
    {
        // Lock target before animation
        lockedTarget = currentEnemy;
        base.Attack();
    }
    
    protected override void FireProjectile()
    {
        if (attackSpawnEffectPrefab != null && gunPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, gunPoint.position, Quaternion.identity, 0.5f);
        }
    
        if (projectilePrefab == null || gunPoint == null) return;
        
        // Use locked target
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
    
        Vector3 targetPos = lockedTarget.GetCenterPoint();
    
        Vector3 directionToEnemy = (targetPos - gunPoint.position).normalized;
        Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.5f;
    
        Quaternion flightRotation = Quaternion.LookRotation(directionToEnemy);
        Quaternion spawnRotation = flightRotation * Quaternion.Euler(visualRotationOffset);
    
        GameObject newProjectile = ObjectPooling.instance.Get(projectilePrefab);
        newProjectile.transform.position = spawnPosition;
        newProjectile.transform.rotation = spawnRotation;
        newProjectile.SetActive(true);
    
        float finalSlowPercent = slowPercent + (strongerSlow ? bonusSlowPercent : 0f);
        float finalSlowDuration = slowDuration + (longerSlow ? bonusSlowDuration : 0f);
        float finalDotDamage = dotDamagePerTick * (frostbite ? frostbiteDamageMultiplier : 1f);
    
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
        
            if (freezeSolid)
            {
                ice.SetFreezeEffect(freezeChance, freezeDuration);
            }
        }
        
        lockedTarget = null;
    }
}