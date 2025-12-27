using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrapDamage : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Transform spikesTransform;
    [SerializeField] private float raisedHeight = 0.5f;
    [SerializeField] private float raiseSpeed = 5f;
    [SerializeField] private float lowerSpeed = 2f;
    
    [Header("Timing")]
    [SerializeField] private float trapActiveDuration = 1f;
    [SerializeField] private float spikeDelay = 0.3f;
    [SerializeField] private float damageDelay = 0.1f;
    
    [Header("VFX Points")]
    [SerializeField] private Transform[] spikeVFXPoints;

    private List<GameObject> activeVFXInstances = new List<GameObject>();
    
    private BoxCollider boxCollider;
    private LayerMask whatIsEnemy;
    private DamageInfo damageInfo;
    private float cooldown;
    private bool isOnCooldown = false;
    private bool spikesAreRaised = false;
    private Vector3 loweredPosition;
    private Vector3 raisedPosition;
    private Collider[] detectedEnemies = new Collider[20];
    private TowerSpikeTrap tower;
    
    // Upgrade effects
    private bool applyPoison = false;
    private DamageInfo poisonDamageInfo;
    private float poisonDuration;
    private GameObject poisonVFX;

    private bool applyBleed = false;
    private DamageInfo bleedDamageInfo;
    private float bleedDuration;
    private GameObject bleedVFX;
    private bool hasCrit = false;
    private float critChance;
    private float critMultiplier;

    private bool applyCripple = false;
    private float slowPercent;
    private float slowDuration;
    private GameObject crippleVFX;

    // Active VFX instances
    private GameObject activePoisonVFX;
    private GameObject activeBleedVFX;
    private GameObject activeCrippleVFX;
    
    public void Setup(DamageInfo newDamageInfo, LayerMask enemyLayer, float trapCooldown, TowerSpikeTrap ownerTower)
    {
        damageInfo = newDamageInfo;
        whatIsEnemy = enemyLayer;
        cooldown = trapCooldown;
        tower = ownerTower;
        boxCollider = GetComponent<BoxCollider>();
        
        if (spikesTransform != null)
        {
            loweredPosition = spikesTransform.localPosition;
            raisedPosition = loweredPosition + Vector3.up * raisedHeight;
        }
    }
    
    public void SetPoisonEffect(float damage, float duration, ElementType elementType, GameObject vfx = null)
    {
        applyPoison = true;
        poisonDamageInfo = new DamageInfo(damage, elementType, true);
        poisonDuration = duration;
        poisonVFX = vfx;
    
        SpawnVFXOnSpikes(poisonVFX);
    }

    public void SetBleedEffect(float damage, float duration, ElementType elementType, GameObject vfx = null, float crit = 0f, float critMult = 2f)
    {
        applyBleed = true;
        bleedDamageInfo = new DamageInfo(damage, elementType, true);
        bleedDuration = duration;
        bleedVFX = vfx;
        hasCrit = crit > 0f;
        critChance = crit;
        critMultiplier = critMult;
    
        SpawnVFXOnSpikes(bleedVFX);
    }

    private void SpawnVFXOnSpikes(GameObject vfxPrefab)
    {
        if (vfxPrefab == null || spikeVFXPoints == null) return;
    
        foreach (Transform point in spikeVFXPoints)
        {
            GameObject vfx = Instantiate(vfxPrefab, point);
            vfx.transform.localPosition = Vector3.zero;
            activeVFXInstances.Add(vfx);
        }
    }

    public void SetCrippleEffect(float percent, float duration)
    {
        applyCripple = true;
        slowPercent = percent;
        slowDuration = duration;
    }
    
    private void Update()
    {
        if (isOnCooldown) return;
        
        // Reset spikes if they're stuck up with no enemies
        if (spikesAreRaised && !HasEnemiesInRange())
        {
            StartCoroutine(ResetSpikes());
            return;
        }
        
        if (HasEnemiesInRange())
        {
            StartCoroutine(TrapCycle());
        }
    }
    
    private bool HasEnemiesInRange()
    {
        int enemyCount = Physics.OverlapBoxNonAlloc(
            transform.position + boxCollider.center,
            boxCollider.size / 2,
            detectedEnemies,
            transform.rotation,
            whatIsEnemy
        );
        
        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i] != null && detectedEnemies[i].gameObject.activeSelf)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private IEnumerator TrapCycle()
    {
        isOnCooldown = true;
        
        if (tower != null)
        {
            tower.PlayHammerAnimation();
        }
        
        yield return new WaitForSeconds(spikeDelay);
        
        if (tower != null)
        {
            tower.SpawnHammerImpactVFX();
        }
        
        StartCoroutine(MoveSpikes(raisedPosition, raiseSpeed));
        spikesAreRaised = true;
        
        yield return new WaitForSeconds(damageDelay);
        DamageEnemiesInRange();
        
        yield return new WaitForSeconds(trapActiveDuration);
        
        yield return StartCoroutine(MoveSpikes(loweredPosition, lowerSpeed));
        spikesAreRaised = false;
        
        yield return new WaitForSeconds(cooldown);
        
        isOnCooldown = false;
    }
    
    private IEnumerator ResetSpikes()
    {
        isOnCooldown = true;
        yield return StartCoroutine(MoveSpikes(loweredPosition, lowerSpeed));
        spikesAreRaised = false;
        yield return new WaitForSeconds(cooldown);
        isOnCooldown = false;
    }
    
    private void DamageEnemiesInRange()
    {
        int enemyCount = Physics.OverlapBoxNonAlloc(
            transform.position + boxCollider.center,
            boxCollider.size / 2,
            detectedEnemies,
            transform.rotation,
            whatIsEnemy
        );
    
        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i] != null && detectedEnemies[i].gameObject.activeSelf)
            {
                IDamageable damageable = detectedEnemies[i].GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Check for crit
                    DamageInfo finalDamage = damageInfo;
                    if (hasCrit && Random.value <= critChance)
                    {
                        finalDamage = new DamageInfo(damageInfo.amount * critMultiplier, damageInfo.elementType);
                    }
                
                    damageable.TakeDamage(finalDamage);
                }
            
                EnemyBase enemy = detectedEnemies[i].GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    // Bleed replaces poison
                    if (applyBleed)
                    {
                        enemy.ApplyDoT(bleedDamageInfo, bleedDuration);
                    }
                    else if (applyPoison)
                    {
                        enemy.ApplyDoT(poisonDamageInfo, poisonDuration);
                    }
                
                    if (applyCripple)
                    {
                        enemy.ApplySlow(slowPercent, slowDuration, false);
                    }
                }
            }
        }
    }
    
    private IEnumerator MoveSpikes(Vector3 targetPosition, float speed)
    {
        while (Vector3.Distance(spikesTransform.localPosition, targetPosition) > 0.01f)
        {
            spikesTransform.localPosition = Vector3.MoveTowards(
                spikesTransform.localPosition,
                targetPosition,
                speed * Time.deltaTime
            );
            yield return null;
        }
        spikesTransform.localPosition = targetPosition;
    }
}