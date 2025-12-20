using System.Collections;
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
    
    private BoxCollider boxCollider;
    private LayerMask whatIsEnemy;
    private float damage;
    private float cooldown;
    private bool isOnCooldown = false;
    private Vector3 loweredPosition;
    private Vector3 raisedPosition;
    private Collider[] detectedEnemies = new Collider[20];
    private TowerSpikeTrap tower;
    
    public void Setup(float damageAmount, LayerMask enemyLayer, float trapCooldown, TowerSpikeTrap ownerTower)
    {
        damage = damageAmount;
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
    
    private void Update()
    {
        if (isOnCooldown) return;
        
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
    
        // Play hammer animation on tower
        if (tower != null)
        {
            tower.PlayHammerAnimation();
        }
    
        // Wait for hammer to hit anvil
        yield return new WaitForSeconds(spikeDelay);
    
        // Spawn impact VFX
        if (tower != null)
        {
            tower.SpawnHammerImpactVFX();
        }
    
        // Start raising spikes
        StartCoroutine(MoveSpikes(raisedPosition, raiseSpeed));
    
        // Small delay then damage (spikes partially up)
        yield return new WaitForSeconds(damageDelay);
        DamageEnemiesInRange();
    
        // Wait for rest of active duration
        yield return new WaitForSeconds(trapActiveDuration);
    
        // Lower spikes
        yield return StartCoroutine(MoveSpikes(loweredPosition, lowerSpeed));
    
        // Cooldown starts after spikes are lowered
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
    
        Debug.Log($"Spike Trap Damage Triggered! Enemies in range: {enemyCount}");
    
        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i] != null && detectedEnemies[i].gameObject.activeSelf)
            {
                IDamageable damageable = detectedEnemies[i].GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Debug.Log($"Dealing {damage} damage to {detectedEnemies[i].name}");
                    damageable.TakeDamage(damage);
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