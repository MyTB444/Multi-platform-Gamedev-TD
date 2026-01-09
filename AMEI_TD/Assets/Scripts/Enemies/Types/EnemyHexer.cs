using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy that debuffs nearby towers with slow effects and has a chance to disable them.
/// </summary>
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
    private Rigidbody rb;

    protected override void Start()
    {
        base.Start();
        hexerAnimator = GetComponent<Animator>();
        lastAttackTime = Time.time;
        rb = GetComponent<Rigidbody>();
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

    /// <summary>
    /// Finds the closest tower within detection radius that is not already disabled.
    /// Returns null if no valid targets are found.
    /// </summary>
    private TowerBase FindTowerInRange()
    {
        // Find closest non-disabled tower within detection radius
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

    /// <summary>
    /// Initiates a debuff attack on the target tower.
    /// Stops movement, spawns magic circle VFX, and plays the casting animation.
    /// </summary>
    /// <param name="target">The tower to attack with debuffs</param>
    private void AttackTower(TowerBase target)
    {
        isCasting = true;
        canMove = false;
        currentTarget = target;

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Spawn magic circle VFX (like Summoner)
        if (magicCirclePrefab != null)
        {
            Vector3 circlePos = transform.position + Vector3.up * magicCircleYOffset;
            activeMagicCircle = ObjectPooling.instance.GetVFX(magicCirclePrefab, circlePos, Quaternion.Euler(360, 180, 0), -1f);
            activeMagicCircle.transform.SetParent(transform);
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

    /// <summary>
    /// Called by animation event at the peak of the cast animation.
    /// Applies debuff effects to the target tower and stops the magic circle VFX.
    /// </summary>
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
                StartCoroutine(ReturnToPoolAfterDelay(activeMagicCircle, ps.main.startLifetime.constantMax));
            }
            else
            {
                ObjectPooling.instance.Return(activeMagicCircle);
            }
            activeMagicCircle = null;
        }
    }

    /// <summary>
    /// Called by animation event when the cast animation completes.
    /// Re-enables movement and resets casting state.
    /// </summary>
    public void OnCastAnimationEnd()
    {
        FinishCasting();
    }

    /// <summary>
    /// Applies slow and potentially disable effects to the target tower.
    /// Slow is always applied, disable has a percentage chance.
    /// </summary>
    /// <param name="target">The tower to apply effects to</param>
    private void ApplyEffectToTower(TowerBase target)
    {
        // Always apply slow
        target.ApplySlow(slowPercent, slowDuration);

        // Spawn slow VFX on tower (parented to tower)
        if (towerSlowVFXPrefab != null)
        {
            Vector3 vfxPos = target.transform.position + Vector3.up * towerVFXYOffset;
            GameObject slowVFX = ObjectPooling.instance.GetVFX(towerSlowVFXPrefab, vfxPos, Quaternion.identity, slowDuration + vfxFadeOutBuffer);
            slowVFX.transform.SetParent(target.transform);
        }

        // Chance to disable (1 second longer than slow)
        if (Random.value < disableChance)
        {
            target.ApplyDisable(slowDuration + 1f);
        }
    }

    /// <summary>
    /// Completes the casting sequence, re-enables movement, and cleans up VFX.
    /// Resets the attack cooldown timer.
    /// </summary>
    private void FinishCasting()
    {
        isCasting = false;
        canMove = true;
        currentTarget = null;
        lastAttackTime = Time.time;

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Cleanup magic circle if still active
        if (activeMagicCircle != null)
        {
            ObjectPooling.instance.Return(activeMagicCircle);
            activeMagicCircle = null;
        }
    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();

        lastAttackTime = Time.time;
        isCasting = false;
        currentTarget = null;

        if (activeMagicCircle != null)
        {
            ObjectPooling.instance.Return(activeMagicCircle);
            activeMagicCircle = null;
        }
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPooling.instance.Return(obj);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
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
        currentTarget = null;
    }
}
