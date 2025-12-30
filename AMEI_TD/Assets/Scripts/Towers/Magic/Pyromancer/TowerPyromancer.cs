using UnityEngine;

public class TowerPyromancer : TowerBase
{
    [Header("Pyromancer Effects")]
    [SerializeField] private bool burnChance = false;
    [SerializeField] [Range(0f, 1f)] private float burnChancePercent = 0.3f;
    [SerializeField] private float burnDamage = 3f;
    [SerializeField] private float burnDuration = 3f;
    [Space]
    [SerializeField] private bool biggerFireball = false;
    [SerializeField] private float fireballScaleMultiplier = 1.5f;
    [SerializeField] private float fireballAoERadius = 1.5f;
    [SerializeField] private float fireballAoEDamagePercent = 0.5f;
    [Space]
    [SerializeField] private bool burnSpread = false;
    [SerializeField] private float burnSpreadRadius = 2f;
    
    // Locked target for attack
    private EnemyBase lockedTarget;
    
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
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, gunPoint.position, Quaternion.identity, 0.1f);
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
        Quaternion spawnRotation = Quaternion.LookRotation(directionToEnemy);

        GameObject newProjectile = ObjectPooling.instance.Get(projectilePrefab);
        newProjectile.transform.position = spawnPosition;
        newProjectile.transform.rotation = spawnRotation;
        newProjectile.SetActive(true);

        HomingProjectile homing = newProjectile.GetComponent<HomingProjectile>();
        if (homing != null)
        {
            homing.SetupHomingProjectile(lockedTarget.transform, damageable, CreateDamageInfo(), projectileSpeed, whatIsEnemy);

            if (burnChance)
            {
                homing.SetBurnEffect(burnChancePercent, burnDamage, burnDuration, elementType, burnSpread, burnSpreadRadius, whatIsEnemy);
            }
    
            if (biggerFireball)
            {
                newProjectile.transform.localScale *= fireballScaleMultiplier;
                homing.SetAoEEffect(fireballAoERadius, fireballAoEDamagePercent, whatIsEnemy);
            }
        }
        
        lockedTarget = null;
    }
}