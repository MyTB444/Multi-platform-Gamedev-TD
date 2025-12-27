using UnityEngine;

public class ArrowProjectile : TowerProjectileBase
{
    [Header("Arrow Settings")]
    [SerializeField] private TrailRenderer trail;
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
    private float arrowSpeed;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    public void SetupArcProjectile(Vector3 targetPos, IDamageable newDamageable, float newDamage, float newSpeed, float distance)
    {
        damageInfo.amount = newDamage;
        damageable = newDamageable;
        spawnTime = Time.time;
        launchTime = Time.time;
        targetPosition = targetPos;
        initialForward = transform.forward;
        
        arrowSpeed = newSpeed + (distance * 0.5f);
        maxCurveDuration = 0.3f + (distance * 0.05f);
        
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
    
    protected override void MoveProjectile()
    {
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