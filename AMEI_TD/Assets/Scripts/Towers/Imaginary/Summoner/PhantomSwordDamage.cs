using UnityEngine;
using System.Collections.Generic;

public class PhantomSwordDamage : MonoBehaviour
{
    private float damage;
    private LayerMask enemyLayer;
    private HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();
    
    public void Setup(float newDamage, LayerMask newEnemyLayer)
    {
        damage = newDamage;
        enemyLayer = newEnemyLayer;
        hitEnemies.Clear();
    }
    
    private void OnEnable()
    {
        // Clear hit list when enabled
        hitEnemies.Clear();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if enemy layer
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
        
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
        
        // Only hit each enemy once per swing
        if (hitEnemies.Contains(enemy)) return;
        
        hitEnemies.Add(enemy);
        
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }
}