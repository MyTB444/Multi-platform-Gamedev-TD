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
    private Rigidbody rb;

    protected override void Start()
    {
        base.Start();
        heraldAnimator = GetComponent<Animator>();
        lastCastTime = Time.time;
        rb = GetComponent<Rigidbody>();
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
        // Check if any nearby allies need healing (don't have HoT active)
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

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (magicCirclePrefab != null)
        {
            Vector3 circlePos = transform.position + Vector3.up * magicCircleYOffset;
            activeMagicCircle = ObjectPooling.instance.GetVFX(magicCirclePrefab, circlePos, Quaternion.Euler(360, 180, 0), -1f);
            activeMagicCircle.transform.SetParent(transform);
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
                StartCoroutine(ReturnToPoolAfterDelay(activeMagicCircle, ps.main.startLifetime.constantMax));
            }
            else
            {
                ObjectPooling.instance.Return(activeMagicCircle);
            }
            activeMagicCircle = null;
        }
    }

    private System.Collections.IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPooling.instance.Return(obj);
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
                float enemyScale = enemy.transform.localScale.x;
                Vector3 scale = Vector3.one * enemyScale;
                GameObject healVFX = ObjectPooling.instance.GetVFX(enemyHealVFXPrefab, vfxPos, Quaternion.identity, scale, healDuration + vfxFadeOutBuffer);
                healVFX.transform.SetParent(enemy.transform);
            }
        }
    }

    private void FinishCasting()
    {
        isCasting = false;
        canMove = true;
        lastCastTime = Time.time;

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (activeMagicCircle != null)
        {
            ObjectPooling.instance.Return(activeMagicCircle);
            activeMagicCircle = null;
        }
    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();

        lastCastTime = Time.time;
        isCasting = false;

        if (activeMagicCircle != null)
        {
            ObjectPooling.instance.Return(activeMagicCircle);
            activeMagicCircle = null;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, castRadius);
    }
    
    private void OnDisable()
    {
        if (activeMagicCircle != null)
        {
            ObjectPooling.instance.Return(activeMagicCircle);
            activeMagicCircle = null;
        }
    
        isCasting = false;
        canMove = true;
    }
}
