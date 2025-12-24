using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class TowerPhantomKnight : TowerBase
{
    [Header("Phantom Setup")]
    [SerializeField] private GameObject phantomPrefab;
    [SerializeField] private float spawnDistanceAhead = 5f;
    
    [Header("Formation")]
    [SerializeField] private int phantomCount = 3;
    [SerializeField] private float horizontalSpacing = 1.5f;
    [SerializeField] private float depthStagger = 0.5f;
    
    [Header("Phantom Stats")]
    [SerializeField] private float phantomSpeed = 5f;
    [SerializeField] private float phantomDamage = 20f;
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private float fadeOutTime = 0.5f;
    
    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnVFXPrefab;
    
    private List<PhantomKnight> activePhantoms = new List<PhantomKnight>();
    
    protected override void FixedUpdate()
    {
        // Clean up dead phantoms
        activePhantoms.RemoveAll(p => p == null);
        
        base.FixedUpdate();
    }
    
    protected override bool CanAttack()
    {
        // Only attack if cooldown passed AND no active phantoms
        return Time.time > lastTimeAttacked + attackCooldown 
            && currentEnemy != null 
            && activePhantoms.Count == 0;
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
        
        if (projectileSpawnDelay > 0)
        {
            Invoke("SpawnPhantoms", projectileSpawnDelay);
        }
        else
        {
            SpawnPhantoms();
        }
    }
    
    private void SpawnPhantoms()
    {
        if (currentEnemy == null) return;
        
        // Get enemy's forward direction
        Vector3 enemyPosition = currentEnemy.transform.position;
        Vector3 enemyForward = currentEnemy.transform.forward;
        
        // Base spawn position ahead of enemy
        Vector3 baseSpawnPos = enemyPosition + enemyForward * spawnDistanceAhead;
        
        // Get right vector for horizontal spread
        Vector3 rightVector = Vector3.Cross(Vector3.up, -enemyForward).normalized;
        
        for (int i = 0; i < phantomCount; i++)
        {
            // Calculate horizontal offset
            float horizontalOffset = 0f;
            if (phantomCount > 1)
            {
                float t = (float)i / (phantomCount - 1);
                horizontalOffset = Mathf.Lerp(-horizontalSpacing, horizontalSpacing, t);
            }
            
            // Calculate depth offset
            float depthOffset = i * depthStagger;
            
            // Calculate spawn position
            Vector3 spawnPos = baseSpawnPos + rightVector * horizontalOffset + enemyForward * depthOffset;
            
            // Find nearest point on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 5f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
            else
            {
                Debug.LogWarning("PhantomKnight: No NavMesh found at spawn position");
                continue;
            }
            
            // Spawn VFX
            if (spawnVFXPrefab != null)
            {
                GameObject vfx = Instantiate(spawnVFXPrefab, spawnPos, Quaternion.identity);
                Destroy(vfx, 2f);
            }
            
            // Spawn phantom facing the enemy
            Quaternion spawnRot = Quaternion.LookRotation(-enemyForward);
            GameObject phantomObj = Instantiate(phantomPrefab, spawnPos, spawnRot);
            
            PhantomKnight phantom = phantomObj.GetComponent<PhantomKnight>();
            if (phantom != null)
            {
                phantom.Setup(phantomSpeed, phantomDamage, attackRadius, fadeOutTime, whatIsEnemy, currentEnemy.transform);
                activePhantoms.Add(phantom);
            }
        }
    }
    
    protected override void HandleRotation() { }
    
    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}