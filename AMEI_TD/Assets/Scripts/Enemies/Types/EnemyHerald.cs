using UnityEngine;

public class EnemyHerald : EnemyBase
{
    [Header("Herald Settings")]
    [SerializeField] private float castRadius = 5f;
    [SerializeField] private float castCooldown = 5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Heal Settings")]
    [SerializeField] private float healPercent = 0.4f;
    [SerializeField] private float healDuration = 3f;

    [Header("Herald VFX")]
    [SerializeField] private GameObject magicCirclePrefab;
    [SerializeField] private float magicCircleYOffset = 0.1f;

    [Header("Enemy Heal VFX")]
    [SerializeField] private GameObject enemyHealVFXPrefab;
    [SerializeField] private float enemyVFXYOffset = 1f;
    [SerializeField] private float vfxFadeOutBuffer = 1f;

    private GameObject activeMagicCircle;
    private float lastCastTime;
    private bool isCasting = false;
    private Animator heraldAnimator;
    private Collider[] detectedEnemies = new Collider[20];

    protected override void Start()
    {
        base.Start();
        heraldAnimator = GetComponent<Animator>();
        lastCastTime = Time.time;
    }

    protected override void Update()
    {
        base.Update();

        if (!isCasting && !isDead)
        {
            TryCast();
        }
    }

    private void TryCast()
    {
        if (Time.time < lastCastTime + castCooldown) return;

        if (HasEnemiesInRange())
        {
            StartCasting();
        }
    }

    private bool HasEnemiesInRange()
    {
        int enemyCount = Physics.OverlapSphereNonAlloc(transform.position, castRadius, detectedEnemies, enemyLayer);

        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i].gameObject == gameObject) continue;

            EnemyBase enemy = detectedEnemies[i].GetComponent<EnemyBase>();
            if (enemy == null)
            {
                enemy = detectedEnemies[i].GetComponentInParent<EnemyBase>();
            }
            if (enemy != null && !enemy.HasHoT())
            {
                return true;
            }
        }
        return false;
    }

    private void StartCasting()
    {
        isCasting = true;
        canMove = false;

        if (magicCirclePrefab != null)
        {
            Vector3 circlePos = transform.position + Vector3.up * magicCircleYOffset;
            activeMagicCircle = Instantiate(magicCirclePrefab, circlePos, Quaternion.Euler(360, 180, 0), transform);
        }

        if (heraldAnimator != null)
        {
            heraldAnimator.SetTrigger("Summon");
        }
        else
        {
            ApplyHealToEnemies();
            FinishCasting();
        }
    }

    // Called by animation event
    public void OnSummonAnimationEvent()
    {
        ApplyHealToEnemies();

        if (activeMagicCircle != null)
        {
            ParticleSystem ps = activeMagicCircle.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
                Destroy(activeMagicCircle, ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(activeMagicCircle);
            }
            activeMagicCircle = null;
        }
    }

    // Called by animation event
    public void OnSummonAnimationEnd()
    {
        FinishCasting();
    }

    private void ApplyHealToEnemies()
    {
        int enemyCount = Physics.OverlapSphereNonAlloc(transform.position, castRadius, detectedEnemies, enemyLayer);

        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i].gameObject == gameObject) continue;

            EnemyBase enemy = detectedEnemies[i].GetComponent<EnemyBase>();
            if (enemy == null)
            {
                enemy = detectedEnemies[i].GetComponentInParent<EnemyBase>();
            }
            if (enemy == null) continue;

            if (enemy.HasHoT()) continue;

            enemy.ApplyHoT(healPercent, healDuration);

            if (enemyHealVFXPrefab != null)
            {
                Vector3 vfxPos = enemy.transform.position + Vector3.up * enemyVFXYOffset;
                GameObject healVFX = Instantiate(enemyHealVFXPrefab, vfxPos, Quaternion.identity, enemy.transform);

                // Scale VFX based on enemy size
                float enemyScale = enemy.transform.localScale.x;
                healVFX.transform.localScale = Vector3.one * enemyScale;

                Destroy(healVFX, healDuration + vfxFadeOutBuffer);
            }
        }
    }

    private void FinishCasting()
    {
        isCasting = false;
        canMove = true;
        lastCastTime = Time.time;

        if (activeMagicCircle != null)
        {
            Destroy(activeMagicCircle);
            activeMagicCircle = null;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, castRadius);
    }
}
