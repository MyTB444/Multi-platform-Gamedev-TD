using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Rotating blade mechanism spawned by BladeTower on the road.
/// Spins continuously when enemies are in range, dealing damage on contact.
/// Supports upgrades: more blades, extended reach, bleed DoT.
/// 
/// Uses a momentum system - speed varies during rotation via sine wave
/// to sync visual impact with the hammer animation.
/// </summary>
public class BladeApparatus : MonoBehaviour
{
    // ==================== BLADE SETUP ====================
    [Header("Blade Setup")]
    [SerializeField] private Transform bladeHolder;  // Parent transform that rotates
    
    // ==================== SPIN SETTINGS ====================
    [Header("Spin Settings")]
    [SerializeField] private float spinSpeed = 360f;          // Degrees per second
    [SerializeField] private float returnSpeed = 180f;        // Speed when returning to start
    [SerializeField] private bool clockwise = true;
    [SerializeField] [Range(0f, 0.9f)] private float momentumStrength = 0.5f;  // How much speed varies
    [SerializeField] private float momentumOffset = 90f;       // Phase offset for momentum wave
    
    [Header("Damage Settings")]
    [SerializeField] private float damageCooldown = 0.5f;     // Min time between hits on same enemy
    
    [Header("Animation")]
    [SerializeField] private float animationAnticipation = 0.5f;  // How early to trigger hammer anim
    
    // ==================== VFX ====================
    [Header("VFX")]
    [SerializeField] private Transform hammerImpactPoint;
    [SerializeField] private GameObject attackSpawnEffectPrefab;
    [SerializeField] private float vfxDelay = 0.3f;

    // ==================== AUDIO ====================
    [Header("Audio")]
    [SerializeField] private AudioClip bladeHitSound;
    [SerializeField] [Range(0f, 1f)] private float bladeHitSoundVolume = 1f;
    
    // ==================== UPGRADE OBJECTS ====================
    [Header("Upgrade Objects")]
    [SerializeField] private GameObject[] extraBlades;      // Additional blades to enable
    [SerializeField] private Transform[] bladeVFXPoints;    // Points for bleed VFX attachment
    [SerializeField] private Transform[] allBlades;         // All blade transforms for scaling
    
    // ==================== REFERENCES (set via Setup) ====================
    private BladeTower tower;
    private DamageInfo damageInfo;
    private float attackRange;
    private LayerMask whatIsEnemy;
    private Animator characterAnimator;
    private string attackAnimationTrigger;
    private AudioSource audioSource;
    
    // ==================== BLEED UPGRADE STATE ====================
    private bool bleedChance = false;
    private float bleedChancePercent = 0.3f;
    private float bleedDamage = 3f;
    private float bleedDuration = 4f;
    private ElementType elementType;
    private GameObject bleedVFX;
    
    // ==================== OTHER UPGRADE STATE ====================
    private bool moreBlades = false;
    private bool extendedReach = false;
    private float extendedBladeScale = 1.5f;
    
    // ==================== RUNTIME STATE ====================
    private List<GameObject> activeBleedVFXList = new List<GameObject>();
    private Dictionary<EnemyBase, float> recentlyHitEnemies = new Dictionary<EnemyBase, float>();  // Hit cooldowns
    
    private Quaternion startRotation;
    private bool isActive = false;           // Enemies in range
    private float baseSpinSpeed;
    private bool isReturning = false;        // Returning to start position
    private float currentAngle = 0f;         // Track rotation for animation timing
    private bool hasTriggeredAnimation = false;  // Prevent multiple triggers per rotation
    
    /// <summary>
    /// Configures the blade apparatus with all necessary parameters.
    /// Called by BladeTower after instantiation.
    /// </summary>
    public void Setup(DamageInfo newDamageInfo, float range, LayerMask enemyLayer, BladeTower ownerTower, Animator animator, string animTrigger)
    {
        damageInfo = newDamageInfo;
        attackRange = range;
        whatIsEnemy = enemyLayer;
        tower = ownerTower;
        characterAnimator = animator;
        attackAnimationTrigger = animTrigger;
        
        baseSpinSpeed = spinSpeed;
        
        if (bladeHolder != null)
        {
            startRotation = bladeHolder.rotation;
        }
        
        // Setup audio source for blade sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;  // 3D sound
        }
    }
    
    /// <summary>
    /// Updates damage info when tower damage upgrades change.
    /// </summary>
    public void UpdateDamageInfo(DamageInfo newDamageInfo)
    {
        damageInfo = newDamageInfo;
    }
    
    private void Update()
    {
        // Don't spin if tower is disabled (stunned, etc.)
        if (tower != null && tower.IsDisabled()) return;
        
        CheckForEnemies();
        
        if (isActive)
        {
            // Enemies present - spin blades
            isReturning = false;
            RotateBlades();
        }
        else if (isReturning || !IsAtStartRotation())
        {
            // No enemies - return to start position
            isReturning = true;
            ReturnToStart();
        }
        
        // Clean up expired hit cooldowns
        CleanupHitList();
    }
    
    /// <summary>
    /// Checks if any targetable enemies are within range.
    /// </summary>
    private void CheckForEnemies()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, whatIsEnemy);
        
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i].GetComponent<EnemyBase>();
            if (enemy != null && enemy.IsTargetable())
            {
                isActive = true;
                return;
            }
        }
        
        isActive = false;
    }
    
    /// <summary>
    /// Checks if blades are at their starting rotation.
    /// </summary>
    private bool IsAtStartRotation()
    {
        return Quaternion.Angle(bladeHolder.rotation, startRotation) < 1f;
    }
    
    /// <summary>
    /// Rotates blades with momentum variation and triggers hammer animation.
    /// 
    /// Momentum system uses sine wave to vary speed during rotation:
    /// - Blade speeds up as it approaches hammer strike point
    /// - Slows down after the strike
    /// This syncs visual impact with hammer animation.
    /// </summary>
    private void RotateBlades()
    {
        if (bladeHolder == null) return;
        
        // Get slow multiplier from debuffs on tower
        float slowMultiplier = tower != null ? tower.GetSlowMultiplier() : 1f;
        
        // ===== MOMENTUM CALCULATION =====
        // Sine wave creates speed variation during rotation
        float adjustedAngle = currentAngle + momentumOffset;
        float momentumMultiplier = 1f + (Mathf.Sin(adjustedAngle * Mathf.Deg2Rad) * momentumStrength);
        
        float currentSpeed = spinSpeed * momentumMultiplier * slowMultiplier;
        float rotationThisFrame = currentSpeed * Time.deltaTime;
        
        currentAngle += rotationThisFrame;
        
        // ===== TRIGGER HAMMER ANIMATION =====
        // Trigger slightly before completing full rotation
        float triggerAngle = 360f - (spinSpeed * animationAnticipation);
        
        if (!hasTriggeredAnimation && currentAngle >= triggerAngle)
        {
            hasTriggeredAnimation = true;
            
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(attackAnimationTrigger);
            }
            
            StartCoroutine(DelayedHammerVFX());
        }
        
        // Reset angle tracking after full rotation
        if (currentAngle >= 360f)
        {
            currentAngle -= 360f;
            hasTriggeredAnimation = false;
        }
        
        // Apply rotation
        Vector3 axis = clockwise ? Vector3.right : Vector3.left;
        bladeHolder.Rotate(axis, rotationThisFrame);
    }
    
    /// <summary>
    /// Delays hammer VFX spawn to sync with animation.
    /// </summary>
    private IEnumerator DelayedHammerVFX()
    {
        yield return new WaitForSeconds(vfxDelay);
        SpawnHammerImpactVFX();
    }
    
    /// <summary>
    /// Spawns hammer impact VFX and plays sound via tower.
    /// </summary>
    public void SpawnHammerImpactVFX()
    {
        if (tower != null)
        {
            tower.PlayAttackSoundFromApparatus();
        }
        
        if (attackSpawnEffectPrefab != null && hammerImpactPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, hammerImpactPoint.position, Quaternion.identity, 2f);
        }
    }
    
    /// <summary>
    /// Smoothly returns blades to starting rotation when no enemies present.
    /// </summary>
    private void ReturnToStart()
    {
        if (bladeHolder == null) return;
        
        Vector3 axis = clockwise ? Vector3.right : Vector3.left;
        bladeHolder.Rotate(axis, returnSpeed * Time.deltaTime);
        
        if (IsAtStartRotation())
        {
            bladeHolder.rotation = startRotation;
            isReturning = false;
            currentAngle = 0f;
            hasTriggeredAnimation = false;
        }
    }
    
    /// <summary>
    /// Removes expired entries from the hit cooldown dictionary.
    /// </summary>
    private void CleanupHitList()
    {
        List<EnemyBase> toRemove = new List<EnemyBase>();
        
        foreach (var kvp in recentlyHitEnemies)
        {
            // Remove if enemy is null, inactive, or cooldown expired
            if (kvp.Key == null || !kvp.Key.gameObject.activeSelf || Time.time >= kvp.Value)
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var enemy in toRemove)
        {
            recentlyHitEnemies.Remove(enemy);
        }
    }
    
    /// <summary>
    /// Called by BladeDamage when blade collider hits an enemy.
    /// Handles damage, hit cooldown, and bleed application.
    /// </summary>
    public void OnBladeHit(EnemyBase enemy)
    {
        if (enemy == null) return;
        
        // Check hit cooldown - prevent rapid hits on same enemy
        if (recentlyHitEnemies.ContainsKey(enemy))
        {
            return;
        }
        
        // Deal damage
        IDamageable damageable = enemy.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageInfo);
        }
        
        PlayBladeHitSound();
        
        // Apply bleed if upgraded and roll succeeds
        if (bleedChance && Random.value <= bleedChancePercent)
        {
            DamageInfo bleedDamageInfo = new DamageInfo(bleedDamage, elementType, true);
            enemy.ApplyDoT(bleedDamageInfo, bleedDuration, 0.5f, false, 0f, default, DebuffType.Bleed);
        }
        
        // Add to cooldown tracking
        recentlyHitEnemies[enemy] = Time.time + damageCooldown;
    }
    
    private void PlayBladeHitSound()
    {
        if (bladeHitSound != null && SFXPlayer.instance != null)
        {
            SFXPlayer.instance.Play(bladeHitSound, transform.position, bladeHitSoundVolume);
        }
    }
    
    // ==================== UPGRADE METHODS ====================
    
    public void SetSpinSpeed(float newSpinSpeed)
    {
        spinSpeed = newSpinSpeed;
    }
    
    /// <summary>
    /// Enables bleed DoT effect on blade hits.
    /// </summary>
    public void SetBleedEffect(float chance, float damage, float duration, ElementType element, GameObject vfx)
    {
        bleedChance = true;
        bleedChancePercent = chance;
        bleedDamage = damage;
        bleedDuration = duration;
        elementType = element;
        bleedVFX = vfx;
        
        SpawnBleedVFX();
    }
    
    public void ClearBleedEffect()
    {
        bleedChance = false;
        ClearBleedVFX();
    }
    
    /// <summary>
    /// Spawns bleed VFX on all blade VFX points.
    /// Respects moreBlades state - skips inactive blade points.
    /// </summary>
    private void SpawnBleedVFX()
    {
        if (bleedVFX == null || bladeVFXPoints == null) return;
        
        foreach (Transform point in bladeVFXPoints)
        {
            // Skip VFX points on extra blades if not unlocked
            if (!moreBlades && point.parent != null && !point.parent.gameObject.activeSelf)
            {
                continue;
            }
            
            GameObject vfx = ObjectPooling.instance.GetVFXWithParent(bleedVFX, point, -1f);
            vfx.transform.localPosition = Vector3.zero;
            activeBleedVFXList.Add(vfx);
        }
    }
    
    private void ClearBleedVFX()
    {
        foreach (GameObject vfx in activeBleedVFXList)
        {
            if (vfx != null)
            {
                ObjectPooling.instance.Return(vfx);
            }
        }
        activeBleedVFXList.Clear();
    }
    
    /// <summary>
    /// Enables/disables extra blade GameObjects.
    /// </summary>
    public void SetMoreBlades(bool enabled)
    {
        moreBlades = enabled;
        
        if (extraBlades != null)
        {
            foreach (GameObject blade in extraBlades)
            {
                if (blade != null)
                {
                    blade.SetActive(enabled);
                }
            }
        }
        
        // Refresh bleed VFX to include/exclude extra blade points
        if (bleedChance)
        {
            ClearBleedVFX();
            SpawnBleedVFX();
        }
    }
    
    /// <summary>
    /// Scales blade length for extended reach upgrade.
    /// </summary>
    public void SetExtendedReach(bool enabled, float scale)
    {
        extendedReach = enabled;
        extendedBladeScale = scale;
        
        if (allBlades != null)
        {
            foreach (Transform blade in allBlades)
            {
                Vector3 currentScale = blade.localScale;
                float yScale = enabled ? extendedBladeScale : 1f;
                blade.localScale = new Vector3(currentScale.x, yScale, currentScale.z);
            }
        }
    }
    
    // ==================== PUBLIC GETTERS ====================
    public float GetAttackRange() => attackRange;
    public bool IsActive() => isActive;
}