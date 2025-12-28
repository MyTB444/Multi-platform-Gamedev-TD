using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhantomSwordDamage : MonoBehaviour
{
    private DamageInfo damageInfo;
    private LayerMask enemyLayer;
    private HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();
    private bool isAttacking = false;
    
    private GameObject slashVFXPrefab;
    private Vector3 vfxRotationOffset;
    private float vfxStartDelay;
    private float vfxDuration;
    
    private bool applySlow = false;
    private float slowPercent;
    private float slowDuration;
    
    public void Setup(DamageInfo newDamageInfo, LayerMask newEnemyLayer, GameObject vfxPrefab, Vector3 rotationOffset, float startDelay = 0f, float duration = 0.5f, bool applySlow = false, float slowPercent = 0f, float slowDuration = 0f)
    {
        damageInfo = newDamageInfo;
        enemyLayer = newEnemyLayer;
        slashVFXPrefab = vfxPrefab;
        vfxRotationOffset = rotationOffset;
        vfxStartDelay = startDelay;
        vfxDuration = duration;
        this.applySlow = applySlow;
        this.slowPercent = slowPercent;
        this.slowDuration = slowDuration;
        hitEnemies.Clear();
        isAttacking = false;
    }
    
    public void EnableDamage()
    {
        isAttacking = true;
        hitEnemies.Clear();
        Debug.Log("Sword damage ENABLED");
    }
    
    public void DisableDamage()
    {
        isAttacking = false;
        Debug.Log("Sword damage DISABLED");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other);
    }
    
    private void OnTriggerStay(Collider other)
    {
        TryDealDamage(other);
    }
    
    private void TryDealDamage(Collider other)
    {
        if (!isAttacking) return;
    
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
    
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
    
        if (hitEnemies.Contains(enemy)) return;
    
        hitEnemies.Add(enemy);
    
        // Spawn VFX at impact point
        if (slashVFXPrefab != null)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
        
            Vector3 directionToEnemy = (other.transform.position - transform.position).normalized;
            directionToEnemy.y = 0;
        
            Quaternion rotation = Quaternion.LookRotation(directionToEnemy) * Quaternion.Euler(vfxRotationOffset);
        
            StartCoroutine(SpawnVFX(hitPoint, rotation));
        }
    
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageInfo);
        }
    
        // Apply slow
        if (applySlow)
        {
            enemy.ApplySlow(slowPercent, slowDuration, false);
        }
    }
    
    private IEnumerator SpawnVFX(Vector3 position, Quaternion rotation)
    {
        if (vfxStartDelay > 0)
        {
            yield return new WaitForSeconds(vfxStartDelay);
        }
        
        GameObject vfx = Instantiate(slashVFXPrefab, position, rotation);
        Destroy(vfx, vfxDuration);
    }
}