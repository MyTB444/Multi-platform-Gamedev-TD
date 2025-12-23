using UnityEngine;
using System.Collections.Generic;

public class BladeTower : TowerBase
{
    [Header("Blade Setup")]
    [SerializeField] private Transform bladeHolder;
    
    [Header("Rotation Settings")]
    [SerializeField] private float returnSpeed = 90f;
    
    [Header("Damage Settings")]
    [SerializeField] private float damageCooldown = 0.5f;
    
    private Dictionary<EnemyBase, float> recentlyHitEnemies = new Dictionary<EnemyBase, float>();
    private Quaternion startRotation;
    private bool isActive = false;
    
    protected override void Start()
    {
        base.Start();
        
        if (bladeHolder != null)
        {
            startRotation = bladeHolder.rotation;
        }
    }
    
    protected override void FixedUpdate()
    {
        CheckForEnemies();
        
        if (isActive)
        {
            RotateBlades();
        }
        else
        {
            ReturnToStart();
        }
        
        CleanupHitList();
    }
    
    private void CheckForEnemies()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, whatIsEnemy);
        isActive = enemies.Length > 0;
    }
    
    private void RotateBlades()
    {
        if (bladeHolder == null) return;
        
        bladeHolder.Rotate(Vector3.up, rotationSpeed * Time.fixedDeltaTime);
    }
    
    private void ReturnToStart()
    {
        if (bladeHolder == null) return;
        
        bladeHolder.rotation = Quaternion.RotateTowards(
            bladeHolder.rotation, 
            startRotation, 
            returnSpeed * Time.fixedDeltaTime
        );
    }
    
    private void CleanupHitList()
    {
        List<EnemyBase> toRemove = new List<EnemyBase>();
        
        foreach (var kvp in recentlyHitEnemies)
        {
            if (kvp.Key == null || !kvp.Key.gameObject.activeSelf || Time.time >= kvp.Value)
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var enemy in toRemove)
        {
            recentlyHitEnemies.Remove(enemy);
        }
    }
    
    public void OnBladeHit(EnemyBase enemy)
    {
        if (enemy == null) return;
        
        if (recentlyHitEnemies.ContainsKey(enemy))
        {
            return;
        }
        
        IDamageable damageable = enemy.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        recentlyHitEnemies[enemy] = Time.time + damageCooldown;
    }
    
    protected override void HandleRotation() { }
    protected override void Attack() { }
    protected override bool CanAttack() { return false; }
    
    protected override void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}