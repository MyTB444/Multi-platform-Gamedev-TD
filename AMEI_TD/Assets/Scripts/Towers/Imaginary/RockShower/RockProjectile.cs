using UnityEngine;

public class RockProjectile : TowerProjectileBase
{
    [SerializeField] private float damageRadius = 1.5f;
    [SerializeField] private LayerMask groundLayer;
    
    private float rockSize = 1f;
    private LayerMask whatIsEnemy;
    
    private Vector3 originalScale;
    private float baseDamageRadius;
    
    [Header("Audio")]
    [SerializeField] [Range(0f, 1f)] private float impactSoundChance = 0.3f;
    
    private bool isMeteor = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        baseDamageRadius = damageRadius;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.localScale = originalScale;
        damageRadius = baseDamageRadius;
        rockSize = 1f;
        isMeteor = false;
        speed = 0f;  // Reset speed on enable
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    public void Setup(DamageInfo newDamageInfo, LayerMask enemyLayer, float fallSpeed, float size = 1f)
    {
        damageInfo = newDamageInfo;
        whatIsEnemy = enemyLayer;
        speed = fallSpeed;
        direction = Vector3.down;
        spawnTime = Time.time;
        rockSize = size;
        damageRadius = baseDamageRadius * size;
        hasHit = false;
        isActive = true;
        isMeteor = false;
    }
    
    public void SetAsMeteor()
    {
        isMeteor = true;
    }
    
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;

        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            hasHit = true;
            DealAOEDamage();
    
            if (impactEffectPrefab != null)
            {
                Vector3 scaledSize = impactEffectPrefab.transform.localScale * rockSize;
                ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, scaledSize, 2f);
            }

            if (isMeteor || Random.value <= impactSoundChance)
            {
                PlayImpactSound();
            }

            ObjectPooling.instance.Return(gameObject);
        }
    }
    
    private void DealAOEDamage()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, damageRadius, whatIsEnemy);
    
        foreach (Collider enemy in enemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damageInfo);
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        bool hitEnemy = ((1 << other.gameObject.layer) & whatIsEnemy) != 0;
        bool hitGround = ((1 << other.gameObject.layer) & groundLayer) != 0;

        if (hitEnemy || hitGround)
        {
            hasHit = true;
            DealAOEDamage();

            if (impactEffectPrefab != null)
            {
                Vector3 scaledSize = impactEffectPrefab.transform.localScale * rockSize;
                ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, scaledSize, 2f);
            }

            if (isMeteor || Random.value <= impactSoundChance)
            {
                PlayImpactSound();
            }

            ObjectPooling.instance.Return(gameObject);
        }
    }
    
    protected override void MoveProjectile()
    {
        float moveDistance = speed * Time.deltaTime;
    
        Debug.DrawRay(transform.position, Vector3.down * (moveDistance + 0.5f), Color.red, 0.1f);
    
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, moveDistance + 0.5f, groundLayer))
        {
            Debug.Log($"[ROCK] Hit ground: {hit.collider.name}");
            transform.position = hit.point;
        
            if (!hasHit)
            {
                hasHit = true;
                DealAOEDamage();

                if (impactEffectPrefab != null)
                {
                    Vector3 scaledSize = impactEffectPrefab.transform.localScale * rockSize;
                    ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, scaledSize, 2f);
                }

                if (isMeteor || Random.value <= impactSoundChance)
                {
                    PlayImpactSound();
                }

                ObjectPooling.instance.Return(gameObject);
            }
            return;
        }
    
        transform.position += direction * moveDistance;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}