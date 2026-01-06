using UnityEngine;

public class ArrowProjectile : TowerProjectileBase
{
    [Header("Arrow Settings")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float curveDelay = 0.05f;
    [SerializeField] private float curveStrength = 8f;
    [SerializeField] private float gravityMultiplier = 3f;
    [SerializeField] private float maxCurveDuration = 0.5f;
    [SerializeField] private float closeRangeGravityBoost = 3f;
    [SerializeField] private float closeRangeThreshold = 6f;
    
    [Header("VFX")]
    [SerializeField] private Transform vfxPoint;
    
    [Header("DoT Effects")]
    private bool applyPoison = false;
    private float poisonDamage;
    private float poisonDuration;
    private DamageInfo poisonDamageInfo;

    private bool applyFire = false;
    private float fireDamage;
    private float fireDuration;
    private DamageInfo fireDamageInfo;
    
    private Rigidbody rb;
    private bool launched = false;
    private bool curving = true;
    private float launchTime;
    private Vector3 targetPosition;
    private Vector3 initialForward;
    private float arrowSpeed;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        launched = false;
        curving = true;
        applyPoison = false;
        applyFire = false;
    
        if (vfxPoint != null)
        {
            foreach (Transform child in vfxPoint)
            {
                ObjectPooling.instance.Return(child.gameObject);
            }
        }
    
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    
        if (trail != null)
        {
            trail.Clear();
        }
    }
    
    public void SetupArcProjectile(Vector3 targetPos, IDamageable newDamageable, DamageInfo newDamageInfo, float newSpeed, float distance)
    {
        damageInfo = newDamageInfo;
        damageable = newDamageable;
        spawnTime = Time.time;
        launchTime = Time.time;
        targetPosition = targetPos;
        initialForward = transform.forward;
    
        arrowSpeed = newSpeed + (distance * 0.5f);
        maxCurveDuration = 0.3f + (distance * 0.05f);
    
        if (distance < closeRangeThreshold)
        {
            gravityMultiplier = 3f + closeRangeGravityBoost;
        }
        else
        {
            gravityMultiplier = 3f;
        }
    
        rb.useGravity = false;
        rb.velocity = initialForward * arrowSpeed;
        launched = true;
        curving = true;
    }
    
    protected override void Update()
    {
        if (!launched || !isActive) return;
        
        if (Time.time - spawnTime > maxLifeTime)
        {
            DestroyProjectile();
            return;
        }
        
        float timeSinceLaunch = Time.time - launchTime;
        
        if (timeSinceLaunch > curveDelay && curving)
        {
            if (timeSinceLaunch > curveDelay + maxCurveDuration)
            {
                curving = false;
            }
            else
            {
                Vector3 toTarget = (targetPosition - transform.position).normalized;
                Vector3 desiredVelocity = toTarget * arrowSpeed;
                rb.velocity = Vector3.Lerp(rb.velocity, desiredVelocity, curveStrength * Time.deltaTime);
            }
        }
        
        if (timeSinceLaunch > curveDelay)
        {
            rb.velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        }
        
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }
    
    public void SetPoisonEffect(float damage, float duration, ElementType elementType, GameObject arrowVFX = null)
    {
        applyPoison = true;
        poisonDamage = damage;
        poisonDuration = duration;
        poisonDamageInfo = new DamageInfo(damage, elementType, true);
    
        if (arrowVFX != null)
        {
            Transform spawnPoint = vfxPoint != null ? vfxPoint : transform;
            GameObject vfx = ObjectPooling.instance.GetVFXWithParent(arrowVFX, spawnPoint, -1f);
            vfx.transform.localPosition = Vector3.zero;
        }
    }

    public void SetFireEffect(float damage, float duration, ElementType elementType, GameObject arrowVFX = null)
    {
        applyFire = true;
        fireDamage = damage;
        fireDuration = duration;
        fireDamageInfo = new DamageInfo(damage, elementType, true);

        if (arrowVFX != null)
        {
            Transform spawnPoint = vfxPoint != null ? vfxPoint : transform;
            GameObject vfx = ObjectPooling.instance.GetVFXWithParent(arrowVFX, spawnPoint, -1f);
            vfx.transform.localPosition = Vector3.zero;
        }
    }
    
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;
        hasHit = true;

        Vector3 impactPoint = other.ClosestPoint(transform.position);

        if (impactEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(impactEffectPrefab, impactPoint, Quaternion.identity, 2f);
        }

        PlayImpactSound(impactPoint);

        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            damageable?.TakeDamage(damageInfo);
        
            if (applyFire)
            {
                enemy.ApplyDoT(fireDamageInfo, fireDuration, 0.5f, false, 0f, default, DebuffType.Burn);
            }
            else if (applyPoison)
            {
                enemy.ApplyDoT(poisonDamageInfo, poisonDuration, 0.5f, false, 0f, default, DebuffType.Poison);
            }
        }
    }
    
    protected override void MoveProjectile()
    {
    }
    
    protected override void DestroyProjectile()
    {
        if (trail != null)
        {
            trail.Clear();
        }
    
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    
        base.DestroyProjectile();
    }
}