using UnityEngine;

public class TowerPyromancer : TowerBase
{
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
    
        GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
    
        HomingProjectile homing = newProjectile.GetComponent<HomingProjectile>();
        if (homing != null)
        {
            homing.SetupHomingProjectile(currentEnemy.transform, damageable, damage, projectileSpeed, whatIsEnemy);
        }
    }
}