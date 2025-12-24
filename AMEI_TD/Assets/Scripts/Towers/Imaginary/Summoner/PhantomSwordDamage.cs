using UnityEngine;
using System.Collections.Generic;

public class PhantomSwordDamage : MonoBehaviour
{
    private float damage;
    private LayerMask enemyLayer;
    private HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();
    private bool isAttacking = false;
    
    public void Setup(float newDamage, LayerMask newEnemyLayer)
    {
        damage = newDamage;
        enemyLayer = newEnemyLayer;
        hitEnemies.Clear();
        isAttacking = false;
        
        Debug.Log($"SwordDamage Setup - Damage: {damage}, EnemyLayer: {enemyLayer.value}");
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
        // Only deal damage during attack
        if (!isAttacking) return;
        
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
        
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
        
        if (hitEnemies.Contains(enemy)) return;
        
        hitEnemies.Add(enemy);
        
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Debug.Log($"Dealt {damage} damage to {other.name}!");
        }
    }
}