using UnityEngine;
using System.Collections.Generic;

public class BladeTower : TowerBase
{
    [Header("Blade Setup")]
    [SerializeField] private Transform bladeHolder;
    
    [Header("Spin Settings")]
    [SerializeField] private float spinSpeed = 360f;
    [SerializeField] private float returnSpeed = 180f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] [Range(0f, 0.9f)] private float momentumStrength = 0.5f;
    [SerializeField] private float momentumOffset = 90f;
    
    [Header("Damage Settings")]
    [SerializeField] private float damageCooldown = 0.5f;
    
    private Dictionary<EnemyBase, float> recentlyHitEnemies = new Dictionary<EnemyBase, float>();
    private Quaternion startRotation;
    private bool isActive = false;
    private bool isReturning = false;
    private float currentAngle = 0f;
    
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
            isReturning = false;
            RotateBlades();
        }
        else if (isReturning || !IsAtStartRotation())
        {
            isReturning = true;
            ReturnToStart();
        }
        
        CleanupHitList();
    }
    
    private void CheckForEnemies()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, whatIsEnemy);
        isActive = enemies.Length > 0;
    }
    
    private bool IsAtStartRotation()
    {
        return Quaternion.Angle(bladeHolder.rotation, startRotation) < 1f;
    }
    
    private void RotateBlades()
    {
        if (bladeHolder == null) return;
        
        float adjustedAngle = currentAngle + momentumOffset;
        float momentumMultiplier = 1f + (Mathf.Sin(adjustedAngle * Mathf.Deg2Rad) * momentumStrength);
        
        float currentSpeed = spinSpeed * momentumMultiplier;
        float rotationThisFrame = currentSpeed * Time.fixedDeltaTime;
        
        currentAngle += rotationThisFrame;
        if (currentAngle >= 360f) currentAngle -= 360f;
        
        Vector3 axis = clockwise ? Vector3.right : Vector3.left;
        bladeHolder.Rotate(axis, rotationThisFrame);
    }
    
    private void ReturnToStart()
    {
        if (bladeHolder == null) return;
        
        // Continue rotating in same direction until back at start
        Vector3 axis = clockwise ? Vector3.right : Vector3.left;
        bladeHolder.Rotate(axis, returnSpeed * Time.fixedDeltaTime);
        
        // Check if we've reached start position
        if (IsAtStartRotation())
        {
            bladeHolder.rotation = startRotation;
            isReturning = false;
            currentAngle = 0f;
        }
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