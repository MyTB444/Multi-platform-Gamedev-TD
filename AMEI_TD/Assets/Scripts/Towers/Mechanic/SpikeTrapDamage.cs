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
    
    private BoxCollider boxCollider;
    private LayerMask whatIsEnemy;
    private float damage;
    private float cooldown;
    private bool isOnCooldown = false;
    private Vector3 loweredPosition;
    private Vector3 raisedPosition;
    private Collider[] detectedEnemies = new Collider[20];
    
    public void Setup(float damageAmount, LayerMask enemyLayer, float trapCooldown)
    {
        damage = damageAmount;
        whatIsEnemy = enemyLayer;
        cooldown = trapCooldown;
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
        
        // Raise spikes
        yield return StartCoroutine(MoveSpikes(raisedPosition, raiseSpeed));
        
        // Damage enemies once
        DamageEnemiesInRange();
        
        // Stay raised for duration
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
        
        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i] != null && detectedEnemies[i].gameObject.activeSelf)
            {
                IDamageable damageable = detectedEnemies[i].GetComponent<IDamageable>();
                if (damageable != null)
                {
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