using UnityEngine;

public class TowerProjectileBase : MonoBehaviour
{
    protected Vector3 direction;
    protected DamageInfo damageInfo;
    protected float speed;
    protected bool isActive = true;
    protected bool hasHit = false;
    protected IDamageable damageable;
    protected Rigidbody rb;

    [SerializeField] protected float maxLifeTime = 10f;
    protected float spawnTime;

    [Header("VFX")]
    [SerializeField] protected GameObject impactEffectPrefab;

    [Header("Audio")]
    [SerializeField] protected AudioClip impactSound;
    [SerializeField] [Range(0f, 1f)] protected float impactSoundVolume = 1f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetupProjectile(Vector3 targetPosition, IDamageable newDamageable, DamageInfo newDamageInfo, float newSpeed)
    {
        direction = (targetPosition - transform.position).normalized;
        damageInfo = newDamageInfo;
        speed = newSpeed;
        spawnTime = Time.time;
        damageable = newDamageable;
    }

    public void SetupProjectile(Vector3 targetPosition, IDamageable newDamageable, float newDamage, float newSpeed)
    {
        SetupProjectile(targetPosition, newDamageable, new DamageInfo(newDamage, ElementType.Physical), newSpeed);
    }

    protected virtual void Update()
    {
        if (!isActive) return;

        if (Time.time - spawnTime > maxLifeTime)
        {
            DestroyProjectile();
            return;
        }

        MoveProjectile();
    }

    protected virtual void MoveProjectile()
    {
        transform.position += direction * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        OnHit(other);
        LayerMask TowerLayer = LayerMask.NameToLayer("Tower");
        if (other.gameObject.layer != TowerLayer)
            DestroyProjectile();
    }

    protected virtual void OnHit(Collider other)
    {
        if (hasHit) return;
        hasHit = true;

        Vector3 impactPoint = other.ClosestPoint(transform.position);

        if (impactEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(impactEffectPrefab, impactPoint, Quaternion.identity, 2f);
        }

        PlayImpactSound();

        if (other.GetComponent<EnemyBase>())
        {
            damageable?.TakeDamage(damageInfo);
        }
    }

    protected virtual void PlayImpactSound()
    {
        if (impactSound != null && SFXPlayer.instance != null)
        {
            SFXPlayer.instance.Play(impactSound, transform.position, impactSoundVolume);
        }
    }

    protected virtual void DestroyProjectile()
    {
        if (!hasHit && impactEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, 2f);
            PlayImpactSound();
        }

        isActive = false;
        hasHit = false;
        ObjectPooling.instance.Return(gameObject);
    }
    
    protected virtual void OnEnable()
    {
        isActive = true;
        hasHit = false;
        spawnTime = Time.time;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}