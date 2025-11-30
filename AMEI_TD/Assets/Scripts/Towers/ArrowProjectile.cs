using UnityEngine;

public class ArrowProjectile : TowerProjectileBase
{
    [Header("Arrow Settings")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float arrowSpeed = 18f;
    [SerializeField] private float curveDelay = 0.05f;
    [SerializeField] private float curveStrength = 8f;
    [SerializeField] private float gravityMultiplier = 3f;
    [SerializeField] private float maxCurveDuration = 0.5f;
    
    private Rigidbody rb;
    private bool launched = false;
    private bool curving = true;
    private float launchTime;
    private Vector3 targetPosition;
    private Vector3 initialForward;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    public void SetupArcProjectile(Vector3 targetPos, IDamageable newDamageable, float newDamage, float flightTime)
    {
        damage = newDamage;
        damageable = newDamageable;
        spawnTime = Time.time;
        launchTime = Time.time;
        targetPosition = targetPos; // Fixed target position, no homing
        initialForward = transform.forward;
        
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
        
        // After delay, start curving toward target position (not enemy)
        if (timeSinceLaunch > curveDelay && curving)
        {
            // Stop curving after max duration - just fly straight with gravity
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
        
        // Always apply gravity after delay
        if (timeSinceLaunch > curveDelay)
        {
            rb.velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        }
        
        // Face direction of travel
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }
    
    protected override void MoveProjectile()
    {
    }
    
    protected override void OnHit(Collider other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
    }
    
    protected override void DestroyProjectile()
    {
        if (trail != null)
        {
            trail.transform.SetParent(null);
            Destroy(trail.gameObject, trail.time);
        }
        
        base.DestroyProjectile();
    }
}