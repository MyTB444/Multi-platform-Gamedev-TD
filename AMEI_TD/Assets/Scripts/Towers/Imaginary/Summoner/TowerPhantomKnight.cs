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
    
    [Header("Pooling")]
    [SerializeField] private int phantomPoolAmount = 10;
    
    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnVFXPrefab;
    [SerializeField] private float vfxDuration = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip phantomSlashSound;
    [SerializeField] [Range(0f, 1f)] private float phantomSlashSoundVolume = 1f;
    
    [Header("Phantom Upgrades")]
    [SerializeField] private bool morePhantoms = false;
    [SerializeField] private int bonusPhantomCount = 1;
    [Space]
    [SerializeField] private bool closerSpawn = false;
    [SerializeField] private float closerSpawnDistance = 2f;
    [Space]
    [SerializeField] private bool spectralChains = false;
    [SerializeField] private float slowPercent = 0.3f;
    [SerializeField] private float slowDuration = 2f;
    [Space]
    [SerializeField] private bool doubleSlash = false;
    
    private List<PhantomKnight> activePhantoms = new List<PhantomKnight>();
    private bool isAttacking = false;
    private Transform savedEnemy;
    private float basePhantomDamage;
    
    protected override void FixedUpdate()
    {
        activePhantoms.RemoveAll(p => p == null);
        
        base.FixedUpdate();
    }
    
    protected override void Start()
    {
        basePhantomDamage = phantomDamage;
        base.Start();
        
        if (phantomPrefab != null)
        {
            ObjectPooling.instance.Register(phantomPrefab, phantomPoolAmount);
        }
    }
    
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
    
    protected override bool CanAttack()
    {
        if (currentEnemy == null) return false;
        
        float distanceToEnemy = Vector3.Distance(transform.position, currentEnemy.transform.position);
        if (distanceToEnemy > attackRange) return false;
        
        return Time.time > lastTimeAttacked + attackCooldown && !isAttacking;
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
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
    
    public void OnSpawnPhantoms()
    {
        PlayAttackSound();
        if (savedEnemy == null || !savedEnemy.gameObject.activeSelf)
        {
            FindNewTarget();
        }
        
        SpawnPhantoms();
        isAttacking = false;
    }
    
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
    
    private void SpawnPhantoms()
    {
        if (savedEnemy == null || !savedEnemy.gameObject.activeSelf) return;

        Vector3 enemyPosition = savedEnemy.position;
        Vector3 enemyForward = savedEnemy.forward;

        float finalSpawnDistance = closerSpawn ? closerSpawnDistance : spawnDistanceAhead;
        Vector3 baseSpawnPos = enemyPosition + enemyForward * finalSpawnDistance;

        Vector3 rightVector = Vector3.Cross(Vector3.up, -enemyForward).normalized;

        int finalPhantomCount = morePhantoms ? phantomCount + bonusPhantomCount : phantomCount;

        for (int i = 0; i < finalPhantomCount; i++)
        {
            float horizontalOffset = 0f;
            if (finalPhantomCount > 1)
            {
                float t = (float)i / (finalPhantomCount - 1);
                horizontalOffset = Mathf.Lerp(-horizontalSpacing, horizontalSpacing, t);
            }

            float depthOffset = i * depthStagger;

            Vector3 spawnPos = baseSpawnPos + rightVector * horizontalOffset + enemyForward * depthOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 15f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
            else
            {
                Debug.LogWarning($"PhantomKnight: No NavMesh found near {spawnPos}");
                continue;
            }

            if (spawnVFXPrefab != null)
            {
                ObjectPooling.instance.GetVFX(spawnVFXPrefab, spawnPos, Quaternion.identity, vfxDuration);
            }

            Quaternion spawnRot = Quaternion.LookRotation(-enemyForward);
            GameObject phantomObj = ObjectPooling.instance.Get(phantomPrefab);
            phantomObj.transform.position = spawnPos;
            phantomObj.transform.rotation = spawnRot;
            phantomObj.SetActive(true);

            PhantomKnight phantom = phantomObj.GetComponent<PhantomKnight>();
            if (phantom != null)
            {
                DamageInfo phantomDamageInfo = new DamageInfo(phantomDamage, elementType);
                phantom.Setup(phantomSpeed, phantomDamageInfo, attackRadius, stoppingDistance, fadeOutTime, whatIsEnemy, savedEnemy, doubleSlash, spectralChains, slowPercent, slowDuration, phantomSlashSound, phantomSlashSoundVolume);
                activePhantoms.Add(phantom);
            }
        }
    }
    
    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}