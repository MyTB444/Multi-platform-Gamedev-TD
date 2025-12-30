using System.Collections.Generic;
using UnityEngine;

public class TowerGuardian : MonoBehaviour
{
    public static TowerGuardian instance;

    [Header("Guardian Setup")]
    [SerializeField] private Animator guardianAnimator;
    [SerializeField] private string idleAnimationState = "Idle";
    [SerializeField] private string lightningAnimationTrigger = "LightningStrike";

    [Header("Global Buff Settings")]
    [SerializeField] private float damageBuffPercent = 0.25f;
    [SerializeField] private float attackSpeedBuffPercent = 0.15f;
    [SerializeField] private float rangeBuffPercent = 0.10f;
    [SerializeField] private float buffUpdateInterval = 1f;

    [Header("Lightning Strike")]
    [SerializeField] private float lightningCooldown = 15f;
    [SerializeField] private ElementType lightningElementType = ElementType.Imaginary;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float lightningRange = 50f;

    [Header("Lightning VFX")]
    [SerializeField] private GameObject lightningEffectPrefab;
    [SerializeField] private GameObject lightningImpactPrefab;
    [SerializeField] private float lightningEffectDuration = 1f;

    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float spawnEffectDuration = 3f;

    private float lastLightningTime;
    private float lastBuffUpdateTime;
    private bool isActive = false;
    private List<TowerBase> buffedTowers = new List<TowerBase>();
    private EnemyBase pendingLightningTarget;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        if (guardianAnimator != null)
        {
            guardianAnimator.Play(idleAnimationState);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        HandleLightningStrike();
        HandleBuffUpdate();
    }

    // Called by Animation Event
    public void OnLightningStrikeEvent()
    {
        if (pendingLightningTarget == null) return;

        SpawnLightningVFX(pendingLightningTarget);
        pendingLightningTarget = null;
    }

    public void ActivateGuardian()
    {
        isActive = true;
        lastLightningTime = Time.time;
        lastBuffUpdateTime = Time.time;

        // Spawn VFX
        if (spawnEffectPrefab != null)
        {
            GameObject spawnVFX = Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity);
            Destroy(spawnVFX, spawnEffectDuration);
        }

        // Apply initial buffs to all towers
        ApplyBuffsToAllTowers();

        // Trigger mega wave
        if (WaveManager.instance != null)
        {
            WaveManager.instance.TriggerMegaWave();
        }

        Debug.Log("Guardian Tower Activated! Mega Wave incoming!");
    }

    private void HandleBuffUpdate()
    {
        if (Time.time < lastBuffUpdateTime + buffUpdateInterval) return;

        lastBuffUpdateTime = Time.time;
        ApplyBuffsToAllTowers();
    }

    private void ApplyBuffsToAllTowers()
    {
        TowerBase[] allTowers = FindObjectsByType<TowerBase>(FindObjectsSortMode.None);

        foreach (TowerBase tower in allTowers)
        {
            if (tower == null) continue;
            if (tower.gameObject == gameObject) continue;

            if (!buffedTowers.Contains(tower))
            {
                buffedTowers.Add(tower);
            }

            tower.ApplyGuardianBuff(damageBuffPercent, attackSpeedBuffPercent, rangeBuffPercent);
        }

        // Clean up destroyed towers
        buffedTowers.RemoveAll(t => t == null);
    }

    private void HandleLightningStrike()
    {
        if (Time.time < lastLightningTime + lightningCooldown) return;

        EnemyBase target = FindLightningTarget();
        if (target == null) return;

        lastLightningTime = Time.time;
        ExecuteLightningStrike(target);
    }

    private EnemyBase FindLightningTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, lightningRange, enemyLayerMask);

        if (enemies.Length == 0) return null;

        // Target the enemy with the most HP
        EnemyBase bestTarget = null;
        float highestHp = 0f;

        foreach (Collider col in enemies)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            float hp = enemy.GetEnemyHp();
            if (hp > highestHp)
            {
                highestHp = hp;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    private void ExecuteLightningStrike(EnemyBase target)
    {
        // Store target for animation event
        pendingLightningTarget = target;

        // Play animation - VFX will spawn when animation event fires
        if (guardianAnimator != null)
        {
            guardianAnimator.SetTrigger(lightningAnimationTrigger);
        }
        else
        {
            // No animator, fire immediately
            SpawnLightningVFX(target);
            pendingLightningTarget = null;
        }
    }

    private void SpawnLightningVFX(EnemyBase target)
    {
        if (target == null) return;

        Vector3 targetPos = target.GetCenterPoint();
        Vector3 spawnPos = targetPos + Vector3.up * 15f;

        // Spawn lightning line
        if (lightningEffectPrefab != null)
        {
            GameObject lightningVFX = Instantiate(lightningEffectPrefab, targetPos, Quaternion.identity);

            LineRenderer lr = lightningVFX.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, spawnPos);
                lr.SetPosition(1, targetPos);
            }

            Destroy(lightningVFX, lightningEffectDuration);
        }

        // Spawn impact VFX
        if (lightningImpactPrefab != null)
        {
            GameObject impact = Instantiate(lightningImpactPrefab, targetPos, Quaternion.identity);
            Destroy(impact, lightningEffectDuration);
        }

        // Insta-kill the target
        float overkillDamage = target.enemyMaxHp * 999f;
        DamageInfo lightningDamage = new DamageInfo(overkillDamage, lightningElementType);
        target.TakeDamage(lightningDamage);

        Debug.Log($"Lightning Strike! Insta-killed {target.name}");
    }

    public float GetDamageBuffPercent() => damageBuffPercent;
    public float GetAttackSpeedBuffPercent() => attackSpeedBuffPercent;
    public float GetRangeBuffPercent() => rangeBuffPercent;
    public bool IsActive() => isActive;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightningRange);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        // Remove buffs from all towers
        foreach (TowerBase tower in buffedTowers)
        {
            if (tower != null)
            {
                tower.RemoveGuardianBuff();
            }
        }
    }
}