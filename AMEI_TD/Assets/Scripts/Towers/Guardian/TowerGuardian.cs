using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Special tower that buffs all other towers and periodically insta-kills the highest HP enemy.
/// Spawning this triggers the Mega Wave.
/// </summary>
public class TowerGuardian : MonoBehaviour
{
    public static TowerGuardian instance;

    // ==================== ANIMATION ====================
    [Header("Guardian Setup")]
    [SerializeField] private Animator guardianAnimator;
    [SerializeField] private string idleAnimationState = "Idle";
    [SerializeField] private string lightningAnimationTrigger = "LightningStrike";

    // ==================== GLOBAL BUFF SETTINGS ====================
    // These buffs are applied to ALL towers when Guardian is active
    [Header("Global Buff Settings")]
    [SerializeField] private float damageBuffPercent = 0.25f;       // +25% damage to all towers
    [SerializeField] private float attackSpeedBuffPercent = 0.15f;  // +15% attack speed to all towers
    [SerializeField] private float rangeBuffPercent = 0.10f;        // +10% range to all towers
    [SerializeField] private float buffUpdateInterval = 1f;         // How often to check for new towers
    [SerializeField] private float buffGlowIntensity = 0.7f;        // Visual glow intensity on buffed towers

    // ==================== LIGHTNING STRIKE ====================
    // Periodically insta-kills the enemy with highest HP
    [Header("Lightning Strike")]
    [SerializeField] private float lightningCooldown = 15f;         // Seconds between strikes
    [SerializeField] private ElementType lightningElementType = ElementType.Imaginary;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float lightningRange = 50f;            // Search radius for targets

    // ==================== LIGHTNING VFX ====================
    [Header("Lightning VFX")]
    [SerializeField] private GameObject lightningEffectPrefab;      // Main lightning bolt VFX
    [SerializeField] private GameObject lightningImpactPrefab;      // Ground impact VFX
    [SerializeField] private float lightningEffectDuration = 1f;
    [SerializeField] private float lightningSpawnHeight = 5f;       // Height above target to spawn bolt
    [SerializeField] private float lightningScale = 3f;
    [SerializeField] private float lightningSpeed = 0.5f;           // Particle simulation speed

    // ==================== SPAWN VFX ====================
    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float spawnEffectDuration = 3f;
    [SerializeField] private float towerAppearDelay = 1f;           // Delay before tower scales in
    [SerializeField] private float towerScaleInDuration = 0.5f;     // How long the scale-in animation takes

    // ==================== AUDIO ====================
    [Header("Audio")]
    [SerializeField] private AudioClip lightningStrikeSound;
    [SerializeField] [Range(0f, 1f)] private float lightningStrikeSoundVolume = 1f;

    // ==================== RUNTIME STATE ====================
    private float lastLightningTime;
    private float lastBuffUpdateTime;
    private bool isActive = false;
    private List<TowerBase> buffedTowers = new List<TowerBase>();   // Tracks all towers we've buffed
    private EnemyBase pendingLightningTarget;                        // Target waiting for animation event
    private Vector3 originalScale;

    private void Awake()
    {
        // Singleton pattern - only one Guardian allowed
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
        // Start in idle animation
        if (guardianAnimator != null)
        {
            guardianAnimator.Play(idleAnimationState);
        }

        // Register VFX prefabs with object pool
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

    /// <summary>
    /// Called by Animation Event when lightning strike animation reaches the "strike" frame.
    /// This ensures VFX syncs with animation.
    /// </summary>
    public void OnLightningStrikeEvent()
    {
        if (pendingLightningTarget == null) return;

        SpawnLightningVFX(pendingLightningTarget);
        pendingLightningTarget = null;
    }

    /// <summary>
    /// Called when Guardian is spawned. Plays spawn VFX, scales in the tower,
    /// applies buffs to all towers, and triggers the Mega Wave.
    /// </summary>
    public void ActivateGuardian()
    {
        // Spawn entrance VFX
        if (spawnEffectPrefab != null)
        {
            GameObject spawnVFX = ObjectPooling.instance.Get(spawnEffectPrefab);
            spawnVFX.transform.position = transform.position;
            spawnVFX.transform.rotation = Quaternion.identity;
            spawnVFX.SetActive(true);

            // Restart all particle systems
            ParticleSystem[] particles = spawnVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Clear();
                ps.Play();
            }

            StartCoroutine(ReturnToPoolAfterDelay(spawnVFX, spawnEffectDuration));
        }

        StartCoroutine(DelayedActivation());
    }

    /// <summary>
    /// Handles the activation sequence: wait for VFX, scale in, apply buffs, trigger mega wave.
    /// </summary>
    private IEnumerator DelayedActivation()
    {
        // Start invisible
        transform.localScale = Vector3.zero;

        // Wait for spawn VFX to play
        yield return new WaitForSeconds(towerAppearDelay);

        // Animate tower scaling in
        yield return StartCoroutine(ScaleTowerIn());

        // Now fully active
        isActive = true;
        lastLightningTime = Time.time;
        lastBuffUpdateTime = Time.time;

        if (guardianAnimator != null)
        {
            guardianAnimator.Play(idleAnimationState);
        }

        // Buff all existing towers
        ApplyBuffsToAllTowers();

        // Trigger the final mega wave
        if (WaveManager.instance != null)
        {
            WaveManager.instance.TriggerMegaWave();
        }

        Debug.Log("Guardian Tower Activated! Mega Wave incoming!");
    }

    /// <summary>
    /// Smoothly scales the tower from 0 to original size using ease-out curve.
    /// </summary>
    private IEnumerator ScaleTowerIn()
    {
        float timer = 0f;

        while (timer < towerScaleInDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / towerScaleInDuration);
            
            // Ease-out curve: starts fast, slows at end
            t = 1f - (1f - t) * (1f - t);
            
            transform.localScale = originalScale * t;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// Periodically refreshes buffs to catch newly placed towers.
    /// </summary>
    private void HandleBuffUpdate()
    {
        if (Time.time < lastBuffUpdateTime + buffUpdateInterval) return;

        lastBuffUpdateTime = Time.time;
        ApplyBuffsToAllTowers();
    }

    /// <summary>
    /// Finds all towers in the scene and applies Guardian buffs to them.
    /// </summary>
    private void ApplyBuffsToAllTowers()
    {
        TowerBase[] allTowers = FindObjectsByType<TowerBase>(FindObjectsSortMode.None);

        foreach (TowerBase tower in allTowers)
        {
            if (tower == null) continue;
            if (tower.gameObject == gameObject) continue;  // Don't buff self

            // Track buffed towers for cleanup on destroy
            if (!buffedTowers.Contains(tower))
            {
                buffedTowers.Add(tower);
            }

            // Apply buff with glow intensity for visual feedback
            tower.ApplyGuardianBuff(damageBuffPercent, attackSpeedBuffPercent, rangeBuffPercent, buffGlowIntensity);
        }

        // Clean up references to destroyed towers
        buffedTowers.RemoveAll(t => t == null);
    }

    /// <summary>
    /// Checks if cooldown has passed, finds highest HP enemy, and strikes them.
    /// </summary>
    private void HandleLightningStrike()
    {
        if (Time.time < lastLightningTime + lightningCooldown) return;

        EnemyBase target = FindLightningTarget();
        if (target == null) return;

        lastLightningTime = Time.time;
        ExecuteLightningStrike(target);
    }

    /// <summary>
    /// Searches for the enemy with highest current HP within range.
    /// </summary>
    /// <returns>The highest HP enemy, or null if none found</returns>
    private EnemyBase FindLightningTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, lightningRange, enemyLayerMask);

        if (enemies.Length == 0) return null;

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

    /// <summary>
    /// Triggers lightning strike animation. Actual damage is dealt in OnLightningStrikeEvent
    /// when animation event fires, or immediately if no animator.
    /// </summary>
    private void ExecuteLightningStrike(EnemyBase target)
    {
        pendingLightningTarget = target;

        if (guardianAnimator != null)
        {
            // Animation event will call OnLightningStrikeEvent
            guardianAnimator.SetTrigger(lightningAnimationTrigger);
        }
        else
        {
            // No animator - strike immediately
            SpawnLightningVFX(target);
            pendingLightningTarget = null;
        }
    }

    /// <summary>
    /// Spawns lightning VFX at target and deals massive overkill damage (insta-kill).
    /// </summary>
    private void SpawnLightningVFX(EnemyBase target)
    {
        if (target == null) return;

        // Calculate spawn positions
        Vector3 targetPos = target.GetCenterPoint();
        Vector3 bottomPos = target.GetBottomPoint() != null ? target.GetBottomPoint().position : target.transform.position;
        Vector3 spawnPos = bottomPos + Vector3.up * lightningSpawnHeight;

        // Spawn lightning bolt VFX
        if (lightningEffectPrefab != null)
        {
            GameObject lightningVFX = ObjectPooling.instance.Get(lightningEffectPrefab);
            lightningVFX.transform.position = spawnPos;
            lightningVFX.transform.rotation = Quaternion.identity;
            lightningVFX.transform.localScale = Vector3.one * lightningScale;
            lightningVFX.SetActive(true);

            // Configure and play particles
            ParticleSystem[] particles = lightningVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.simulationSpeed = lightningSpeed;
                ps.Clear();
                ps.Play();
            }

            StartCoroutine(ReturnToPoolAfterDelay(lightningVFX, lightningEffectDuration * 2f));
        }

        // Spawn ground impact VFX
        if (lightningImpactPrefab != null)
        {
            GameObject impact = ObjectPooling.instance.Get(lightningImpactPrefab);
            impact.transform.position = bottomPos;
            impact.transform.rotation = Quaternion.identity;
            impact.SetActive(true);

            ParticleSystem[] particles = impact.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Clear();
                ps.Play();
            }

            StartCoroutine(ReturnToPoolAfterDelay(impact, lightningEffectDuration));
        }

        PlayLightningStrikeSound(bottomPos);

        // Deal massive overkill damage to guarantee kill regardless of HP
        float overkillDamage = target.enemyMaxHp * 999f;
        DamageInfo lightningDamage = new DamageInfo(overkillDamage, lightningElementType);
        target.TakeDamage(lightningDamage);

        Debug.Log($"Lightning Strike! Insta-killed {target.name}");
    }

    /// <summary>
    /// Plays lightning sound effect at world position.
    /// </summary>
    private void PlayLightningStrikeSound(Vector3 position)
    {
        if (lightningStrikeSound != null)
        {
            AudioSource.PlayClipAtPoint(lightningStrikeSound, position, lightningStrikeSoundVolume);
        }
    }

    /// <summary>
    /// Helper coroutine to return pooled VFX after delay.
    /// </summary>
    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPooling.instance.Return(obj);
    }

    // ==================== PUBLIC GETTERS ====================
    public float GetDamageBuffPercent() => damageBuffPercent;
    public float GetAttackSpeedBuffPercent() => attackSpeedBuffPercent;
    public float GetRangeBuffPercent() => rangeBuffPercent;
    public bool IsActive() => isActive;

    /// <summary>
    /// Editor visualization - shows lightning strike range.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightningRange);
    }

    /// <summary>
    /// Cleanup: remove buffs from all towers when Guardian is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        // Remove buffs from all towers we buffed
        foreach (TowerBase tower in buffedTowers)
        {
            if (tower != null)
            {
                tower.RemoveGuardianBuff();
            }
        }
    }
}