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
    
    protected override void FireProjectile()
    {
        if (attackSpawnEffectPrefab != null && gunPoint != null)
        {
            GameObject spawnVFX = Instantiate(attackSpawnEffectPrefab, gunPoint.position, Quaternion.identity);
            Destroy(spawnVFX, 0.5f);
        }
        
        if (projectilePrefab == null || gunPoint == null) return;
        if (currentEnemy == null) return;
        
        IDamageable damageable = currentEnemy.GetComponent<IDamageable>();
        if (damageable == null) return;
        
        Vector3 targetPos = currentEnemy.GetCenterPoint();
        
        Vector3 directionToEnemy = (targetPos - gunPoint.position).normalized;
        Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.5f;
        
        Quaternion flightRotation = Quaternion.LookRotation(directionToEnemy);
        Quaternion spawnRotation = flightRotation * Quaternion.Euler(visualRotationOffset);
        
        GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
        
        IceProjectile ice = newProjectile.GetComponent<IceProjectile>();
        if (ice != null)
        {
            ice.SetupIceProjectile(
                currentEnemy.transform, 
                damageable, 
                CreateDamageInfo(), 
                projectileSpeed, 
                whatIsEnemy,
                slowPercent,
                slowDuration,
                effectRadius,
                dotDamagePerTick,
                dotDuration,
                dotTickInterval,
                visualRotationOffset
            );
        }
    }
}