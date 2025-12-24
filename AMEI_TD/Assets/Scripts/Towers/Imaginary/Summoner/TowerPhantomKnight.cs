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
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float fadeOutTime = 0.5f;
    
    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnVFXPrefab;
    [SerializeField] private float vfxDuration = 2f;
    
    private List<PhantomKnight> activePhantoms = new List<PhantomKnight>();
    private bool isAttacking = false;
    private Transform savedEnemy;
    
    protected override void FixedUpdate()
    {
        activePhantoms.RemoveAll(p => p == null);
        
        base.FixedUpdate();
    }
    
    protected override bool CanAttack()
    {
        return Time.time > lastTimeAttacked + attackCooldown 
            && currentEnemy != null 
            && activePhantoms.Count == 0
            && !isAttacking;
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        // Save enemy reference for when animation triggers spawn
        savedEnemy = currentEnemy?.transform;
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
        else
        {
            OnSpawnPhantoms();
        }
    }
    
    // Called by animation event
    public void OnSpawnPhantoms()
    {
        SpawnPhantoms();
        isAttacking = false;
    }
    
    private void SpawnPhantoms()
    {
        if (savedEnemy == null) return;
        
        Vector3 enemyPosition = savedEnemy.position;
        Vector3 enemyForward = savedEnemy.forward;
        
        Vector3 baseSpawnPos = enemyPosition + enemyForward * spawnDistanceAhead;
        
        Vector3 rightVector = Vector3.Cross(Vector3.up, -enemyForward).normalized;
        
        for (int i = 0; i < phantomCount; i++)
        {
            float horizontalOffset = 0f;
            if (phantomCount > 1)
            {
                float t = (float)i / (phantomCount - 1);
                horizontalOffset = Mathf.Lerp(-horizontalSpacing, horizontalSpacing, t);
            }
            
            float depthOffset = i * depthStagger;
            
            Vector3 spawnPos = baseSpawnPos + rightVector * horizontalOffset + enemyForward * depthOffset;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 15f, NavMesh.AllAreas))  // Increased from 10f to 15f
            {
                spawnPos = hit.position;
            }
            else
            {
                Debug.LogWarning($"PhantomKnight: No NavMesh found near {spawnPos}");
                continue;
            }
            
            // Spawn VFX
            if (spawnVFXPrefab != null)
            {
                GameObject vfx = Instantiate(spawnVFXPrefab, spawnPos, Quaternion.identity);
                Destroy(vfx, vfxDuration);
            }
            
            // Spawn phantom
            Quaternion spawnRot = Quaternion.LookRotation(-enemyForward);
            GameObject phantomObj = Instantiate(phantomPrefab, spawnPos, spawnRot);
            
            PhantomKnight phantom = phantomObj.GetComponent<PhantomKnight>();
            if (phantom != null)
            {
                phantom.Setup(phantomSpeed, phantomDamage, attackRadius, stoppingDistance, fadeOutTime, whatIsEnemy, savedEnemy);
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