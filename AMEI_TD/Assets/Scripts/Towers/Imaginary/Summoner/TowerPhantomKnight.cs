using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Tower that spawns spectral PhantomKnight warriors to attack enemies.
/// Phantoms spawn in a formation ahead of the target enemy and charge toward them.
/// Supports upgrades for more phantoms, closer spawns, slow debuff, and double attacks.
/// </summary>
public class TowerPhantomKnight : TowerBase
{
    // ==================== PHANTOM SETUP ====================
    [Header("Phantom Setup")]
    [SerializeField] private GameObject phantomPrefab;
    [SerializeField] private float spawnDistanceAhead = 5f;  // How far ahead of enemy to spawn
    
    // ==================== FORMATION ====================
    [Header("Formation")]
    [SerializeField] private int phantomCount = 3;           // Base number of phantoms per attack
    [SerializeField] private float horizontalSpacing = 1.5f; // Side-to-side spread
    [SerializeField] private float depthStagger = 0.5f;      // Front-to-back offset per phantom
    
    // ==================== PHANTOM STATS ====================
    [Header("Phantom Stats")]
    [SerializeField] private float phantomSpeed = 5f;
    [SerializeField] private float phantomDamage = 20f;
    [SerializeField] private float attackRadius = 2f;        // How close phantom must be to attack
    [SerializeField] private float stoppingDistance = 0.5f;  // NavMeshAgent stopping distance
    [SerializeField] private float fadeOutTime = 0.5f;       // Time to fade after attacking
    
    [Header("Pooling")]
    [SerializeField] private int phantomPoolAmount = 10;
    
    // ==================== VFX ====================
    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnVFXPrefab;
    [SerializeField] private float vfxDuration = 2f;

    // ==================== AUDIO ====================
    [Header("Audio")]
    [SerializeField] private AudioClip phantomSlashSound;
    [SerializeField] [Range(0f, 1f)] private float phantomSlashSoundVolume = 1f;
    
    // ==================== UPGRADES ====================
    [Header("Phantom Upgrades")]
    [SerializeField] private bool morePhantoms = false;      // Spawn additional phantoms
    [SerializeField] private int bonusPhantomCount = 1;
    [Space]
    [SerializeField] private bool closerSpawn = false;       // Spawn closer to enemy
    [SerializeField] private float closerSpawnDistance = 2f;
    [Space]
    [SerializeField] private bool spectralChains = false;    // Phantoms apply slow on hit
    [SerializeField] private float slowPercent = 0.3f;
    [SerializeField] private float slowDuration = 2f;
    [Space]
    [SerializeField] private bool doubleSlash = false;       // Phantoms attack twice
    
    // ==================== RUNTIME STATE ====================
    private List<PhantomKnight> activePhantoms = new List<PhantomKnight>();  // Track spawned phantoms
    private bool isAttacking = false;      // Waiting for animation
    private Transform savedEnemy;          // Target saved at attack start
    private float basePhantomDamage;       // For damage boost calculations
    
    protected override void FixedUpdate()
    {
        // Clean up null references from destroyed phantoms
        activePhantoms.RemoveAll(p => p == null);
        
        base.FixedUpdate();
    }
    
    protected override void Start()
    {
        basePhantomDamage = phantomDamage;
        base.Start();
        
        // Register phantom prefab with pool
        if (phantomPrefab != null)
        {
            ObjectPooling.instance.Register(phantomPrefab, phantomPoolAmount);
        }
    }
    
    /// <summary>
    /// Handles upgrade state changes from skill tree.
    /// </summary>
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.MorePhantoms:
                morePhantoms = enabled;
                break;
            case TowerUpgradeType.CloserSpawn:
                closerSpawn = enabled;
                break;
            case TowerUpgradeType.SpectralChains:
                spectralChains = enabled;
                break;
            case TowerUpgradeType.DoubleSlash:
                doubleSlash = enabled;
                break;
        }
    }
    
    /// <summary>
    /// Applies damage boost to phantom damage.
    /// </summary>
    protected override void ApplyStatUpgrades()
    {
        base.ApplyStatUpgrades();
    
        if (damageBoost)
        {
            phantomDamage = basePhantomDamage * (1f + damageBoostPercent);
        }
        else
        {
            phantomDamage = basePhantomDamage;
        }
    }
    
    /// <summary>
    /// Additional attack conditions - must have target in range and not mid-attack.
    /// </summary>
    protected override bool CanAttack()
    {
        if (currentEnemy == null) return false;
        
        float distanceToEnemy = Vector3.Distance(transform.position, currentEnemy.transform.position);
        if (distanceToEnemy > attackRange) return false;
        
        return Time.time > lastTimeAttacked + attackCooldown && !isAttacking;
    }
    
    /// <summary>
    /// Initiates attack - saves target and triggers animation.
    /// </summary>
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        // Save enemy reference for spawn (enemy may die/move during animation)
        savedEnemy = currentEnemy?.transform;
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
        else
        {
            // No animator - spawn immediately
            OnSpawnPhantoms();
        }
    }
    
    /// <summary>
    /// Called by Animation Event to spawn phantoms.
    /// </summary>
    public void OnSpawnPhantoms()
    {
        PlayAttackSound();
        
        // Re-acquire target if original died
        if (savedEnemy == null || !savedEnemy.gameObject.activeSelf)
        {
            FindNewTarget();
        }
        
        SpawnPhantoms();
        isAttacking = false;
    }
    
    /// <summary>
    /// Finds closest enemy in range as backup target.
    /// </summary>
    private void FindNewTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, whatIsEnemy);
        
        if (enemies.Length > 0)
        {
            float closestDist = float.MaxValue;
            Transform closest = null;
            
            foreach (Collider col in enemies)
            {
                if (!col.gameObject.activeSelf) continue;
                
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.transform;
                }
            }
            
            savedEnemy = closest;
        }
    }
    
    /// <summary>
    /// Spawns phantoms in formation ahead of the target enemy.
    /// Formation is perpendicular to enemy's movement direction.
    /// </summary>
    private void SpawnPhantoms()
    {
        if (savedEnemy == null || !savedEnemy.gameObject.activeSelf) return;

        Vector3 enemyPosition = savedEnemy.position;
        Vector3 enemyForward = savedEnemy.forward;  // Direction enemy is moving

        // Calculate base spawn position ahead of enemy
        float finalSpawnDistance = closerSpawn ? closerSpawnDistance : spawnDistanceAhead;
        Vector3 baseSpawnPos = enemyPosition + enemyForward * finalSpawnDistance;

        // Calculate perpendicular vector for horizontal spread
        Vector3 rightVector = Vector3.Cross(Vector3.up, -enemyForward).normalized;

        int finalPhantomCount = morePhantoms ? phantomCount + bonusPhantomCount : phantomCount;

        for (int i = 0; i < finalPhantomCount; i++)
        {
            // ===== CALCULATE HORIZONTAL OFFSET =====
            // Spread phantoms evenly across the formation width
            float horizontalOffset = 0f;
            if (finalPhantomCount > 1)
            {
                float t = (float)i / (finalPhantomCount - 1);
                horizontalOffset = Mathf.Lerp(-horizontalSpacing, horizontalSpacing, t);
            }

            // ===== CALCULATE DEPTH OFFSET =====
            // Stagger phantoms front-to-back
            float depthOffset = i * depthStagger;

            Vector3 spawnPos = baseSpawnPos + rightVector * horizontalOffset + enemyForward * depthOffset;

            // ===== FIND VALID NAVMESH POSITION =====
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 15f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
            else
            {
                Debug.LogWarning($"PhantomKnight: No NavMesh found near {spawnPos}");
                continue;  // Skip this phantom
            }

            // ===== SPAWN VFX =====
            if (spawnVFXPrefab != null)
            {
                ObjectPooling.instance.GetVFX(spawnVFXPrefab, spawnPos, Quaternion.identity, vfxDuration);
            }

            // ===== SPAWN PHANTOM =====
            // Face toward enemy (opposite of enemy's forward)
            Quaternion spawnRot = Quaternion.LookRotation(-enemyForward);
            GameObject phantomObj = ObjectPooling.instance.Get(phantomPrefab);
            phantomObj.transform.position = spawnPos;
            phantomObj.transform.rotation = spawnRot;
            phantomObj.SetActive(true);

            // Configure phantom with all parameters
            PhantomKnight phantom = phantomObj.GetComponent<PhantomKnight>();
            if (phantom != null)
            {
                DamageInfo phantomDamageInfo = new DamageInfo(phantomDamage, elementType);
                phantom.Setup(phantomSpeed, phantomDamageInfo, attackRadius, stoppingDistance, fadeOutTime, whatIsEnemy, savedEnemy, doubleSlash, spectralChains, slowPercent, slowDuration, phantomSlashSound, phantomSlashSoundVolume);
                activePhantoms.Add(phantom);
            }
        }
    }
    
    /// <summary>
    /// Editor visualization - shows attack range.
    /// </summary>
    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}