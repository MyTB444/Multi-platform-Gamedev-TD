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
    
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.PyromancerAttackSpeed:
                attackSpeedBoost = enabled;
                ApplyStatUpgrades();
                break;
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
    
    protected override void FireProjectile()
    {
        if (attackSpawnEffectPrefab != null && gunPoint != null)
        {
            GameObject spawnVFX = Instantiate(attackSpawnEffectPrefab, gunPoint.position, Quaternion.identity);
            Destroy(spawnVFX, .1f);
        }

        if (projectilePrefab == null || gunPoint == null) return;

        if (currentEnemy == null) return;

        IDamageable damageable = currentEnemy.GetComponent<IDamageable>();
        if (damageable == null) return;

        Vector3 targetPos = currentEnemy.GetCenterPoint();

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
            homing.SetupHomingProjectile(currentEnemy.transform, damageable, CreateDamageInfo(), projectileSpeed, whatIsEnemy);

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
    }
}