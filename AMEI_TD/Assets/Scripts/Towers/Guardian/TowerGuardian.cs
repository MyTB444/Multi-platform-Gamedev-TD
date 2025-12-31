using System.Collections;
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
    [SerializeField] private float lightningSpawnHeight = 5f;
    [SerializeField] private float lightningScale = 3f;
    [SerializeField] private float lightningSpeed = 0.5f;

    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float spawnEffectDuration = 3f;
    [SerializeField] private float towerAppearDelay = 1f;
    [SerializeField] private float towerScaleInDuration = 0.5f;

    private float lastLightningTime;
    private float lastBuffUpdateTime;
    private bool isActive = false;
    private List<TowerBase> buffedTowers = new List<TowerBase>();
    private EnemyBase pendingLightningTarget;
    private Vector3 originalScale;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        originalScale = transform.localScale;
    }

    private void Start()
    {
        if (guardianAnimator != null)
        {
            guardianAnimator.Play(idleAnimationState);
        }

        // Register VFX prefabs with pool
        if (lightningEffectPrefab != null)
        {
            ObjectPooling.instance.Register(lightningEffectPrefab, 3);
        }
        if (lightningImpactPrefab != null)
        {
            ObjectPooling.instance.Register(lightningImpactPrefab, 3);
        }
        if (spawnEffectPrefab != null)
        {
            ObjectPooling.instance.Register(spawnEffectPrefab, 1);
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
        // Spawn VFX first
        if (spawnEffectPrefab != null)
        {
            GameObject spawnVFX = ObjectPooling.instance.Get(spawnEffectPrefab);
            spawnVFX.transform.position = transform.position;
            spawnVFX.transform.rotation = Quaternion.identity;
            spawnVFX.SetActive(true);

            // Restart particles
            ParticleSystem[] particles = spawnVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Clear();
                ps.Play();
            }

            StartCoroutine(ReturnToPoolAfterDelay(spawnVFX, spawnEffectDuration));
        }

        // Delay the tower activation
        StartCoroutine(DelayedActivation());
    }

    private IEnumerator DelayedActivation()
    {
        // Hide the tower initially
        transform.localScale = Vector3.zero;

        yield return new WaitForSeconds(towerAppearDelay);

        // Scale in the tower
        yield return StartCoroutine(ScaleTowerIn());

        // Now activate everything
        isActive = true;
        lastLightningTime = Time.time;
        lastBuffUpdateTime = Time.time;

        // Start playing idle animation
        if (guardianAnimator != null)
        {
            guardianAnimator.Play(idleAnimationState);
        }

        ApplyBuffsToAllTowers();

        if (WaveManager.instance != null)
        {
            WaveManager.instance.TriggerMegaWave();
        }

        Debug.Log("Guardian Tower Activated! Mega Wave incoming!");
    }

    private IEnumerator ScaleTowerIn()
    {
        float timer = 0f;

        while (timer < towerScaleInDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / towerScaleInDuration);
            
            // Ease out for a nice pop effect
            t = 1f - (1f - t) * (1f - t);
            
            transform.localScale = originalScale * t;
            yield return null;
        }

        transform.localScale = originalScale;
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
        Vector3 bottomPos = target.GetBottomPoint() != null ? target.GetBottomPoint().position : target.transform.position;
        Vector3 spawnPos = bottomPos + Vector3.up * lightningSpawnHeight;

        // Spawn lightning VFX from pool
        if (lightningEffectPrefab != null)
        {
            GameObject lightningVFX = ObjectPooling.instance.Get(lightningEffectPrefab);
            lightningVFX.transform.position = spawnPos;
            lightningVFX.transform.rotation = Quaternion.identity;
            lightningVFX.transform.localScale = Vector3.one * lightningScale;
            lightningVFX.SetActive(true);

            // Restart particle systems
            ParticleSystem[] particles = lightningVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.simulationSpeed = lightningSpeed;
                ps.Clear();
                ps.Play();
            }

            // Return to pool after duration
            StartCoroutine(ReturnToPoolAfterDelay(lightningVFX, lightningEffectDuration * 2f));
        }

        // Spawn impact VFX from pool
        if (lightningImpactPrefab != null)
        {
            GameObject impact = ObjectPooling.instance.Get(lightningImpactPrefab);
            impact.transform.position = bottomPos;
            impact.transform.rotation = Quaternion.identity;
            impact.SetActive(true);

            // Restart particle systems
            ParticleSystem[] particles = impact.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Clear();
                ps.Play();
            }

            StartCoroutine(ReturnToPoolAfterDelay(impact, lightningEffectDuration));
        }

        // Insta-kill the target
        float overkillDamage = target.enemyMaxHp * 999f;
        DamageInfo lightningDamage = new DamageInfo(overkillDamage, lightningElementType);
        target.TakeDamage(lightningDamage);

        Debug.Log($"Lightning Strike! Insta-killed {target.name}");
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPooling.instance.Return(obj);
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