using UnityEngine;

public class EnemyHexer : EnemyBase
{
    [Header("Hexer Settings")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float attackCooldown = 5f;
    [SerializeField] private LayerMask towerLayer;

    [Header("Slow Effect")]
    [SerializeField] private float slowPercent = 0.5f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Disable Effect")]
    [SerializeField] private float disableChance = 0.25f;

    [Header("Hexer VFX")]
    [SerializeField] private GameObject magicCirclePrefab;
    [SerializeField] private float magicCircleYOffset = 0.1f;

    [Header("Tower Debuff VFX")]
    [SerializeField] private GameObject towerSlowVFXPrefab;
    [SerializeField] private float towerVFXYOffset = 1f;
    [SerializeField] private float vfxFadeOutBuffer = 1f;

    private GameObject activeMagicCircle;
    private float lastAttackTime;
    private bool isCasting = false;
    private Animator hexerAnimator;
    private Collider[] detectedTowers = new Collider[20];
    private TowerBase currentTarget;

    protected override void Start()
    {
        base.Start();
        hexerAnimator = GetComponent<Animator>();
        lastAttackTime = Time.time;
    }

    protected override void Update()
    {
        base.Update();

        if (!isCasting && !isDead)
        {
            TryAttackTower();
        }
    }

    private void TryAttackTower()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        TowerBase target = FindTowerInRange();
        if (target != null)
        {
            AttackTower(target);
        }
    }

    private TowerBase FindTowerInRange()
    {
        int towerCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, detectedTowers, towerLayer);
        if (towerCount == 0) return null;

        TowerBase bestTarget = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < towerCount; i++)
        {
            TowerBase tower = detectedTowers[i].GetComponent<TowerBase>();
            if (tower == null)
            {
                tower = detectedTowers[i].GetComponentInParent<TowerBase>();
            }
            if (tower == null) continue;

            // Skip already disabled towers
            if (tower.IsDisabled()) continue;

            float distance = Vector3.Distance(transform.position, tower.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = tower;
            }
        }

        return bestTarget;
    }

    private void AttackTower(TowerBase target)
    {
        isCasting = true;
        canMove = false;
        currentTarget = target;

        // Spawn magic circle VFX (like Summoner)
        if (magicCirclePrefab != null)
        {
            Vector3 circlePos = transform.position + Vector3.up * magicCircleYOffset;
            activeMagicCircle = Instantiate(magicCirclePrefab, circlePos, Quaternion.Euler(360, 180, 0), transform);
        }

        // Trigger animation if available
        if (hexerAnimator != null)
        {
            hexerAnimator.SetTrigger("Summon");
        }
        else
        {
            ApplyEffectToTower(target);
            FinishCasting();
        }
    }

    // Called by animation event
    public void OnCastAnimationEvent()
    {
        if (currentTarget != null)
        {
            ApplyEffectToTower(currentTarget);
        }

        // Clean up magic circle
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
    public void OnCastAnimationEnd()
    {
        FinishCasting();
    }

    private void ApplyEffectToTower(TowerBase target)
    {
        // Always apply slow
        target.ApplySlow(slowPercent, slowDuration);

        // Spawn slow VFX on tower (parented to tower)
        if (towerSlowVFXPrefab != null)
        {
            Vector3 vfxPos = target.transform.position + Vector3.up * towerVFXYOffset;
            GameObject slowVFX = Instantiate(towerSlowVFXPrefab, vfxPos, Quaternion.identity, target.transform);
            Destroy(slowVFX, slowDuration + vfxFadeOutBuffer);
        }

        // Chance to disable (1 second longer than slow)
        if (Random.value < disableChance)
        {
            target.ApplyDisable(slowDuration + 1f);
        }
    }

    private void FinishCasting()
    {
        isCasting = false;
        canMove = true;
        currentTarget = null;
        lastAttackTime = Time.time;

        // Cleanup magic circle if still active
        if (activeMagicCircle != null)
        {
            Destroy(activeMagicCircle);
            activeMagicCircle = null;
        }
    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();

        // Reset to allow immediate casting after cooldown
        lastAttackTime = -attackCooldown;
        isCasting = false;
        currentTarget = null;

        // Cleanup any active magic circle VFX
        if (activeMagicCircle != null)
        {
            Destroy(activeMagicCircle);
            activeMagicCircle = null;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
