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

        // Calculate direction to enemy
        Vector3 directionToEnemy = (currentEnemy.transform.position - gunPoint.position).normalized;
    
        // Spawn position slightly forward
        Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.5f;
    
        // Create rotation facing the enemy
        Quaternion spawnRotation = Quaternion.LookRotation(directionToEnemy);
    
        // Spawn with correct rotation
        GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
    
        HomingProjectile homing = newProjectile.GetComponent<HomingProjectile>();
        if (homing != null)
        {
            homing.SetupHomingProjectile(currentEnemy.transform, damageable, damage, projectileSpeed);
        }
    }
}