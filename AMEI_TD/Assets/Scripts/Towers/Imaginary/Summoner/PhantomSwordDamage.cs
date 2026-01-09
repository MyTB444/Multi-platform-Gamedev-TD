using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles the sword collider damage for PhantomKnight.
/// Tracks which enemies have been hit to prevent double damage.
/// Spawns slash VFX and optionally applies slow debuff.
/// </summary>
public class PhantomSwordDamage : MonoBehaviour
{
    // ==================== COMBAT DATA ====================
    private DamageInfo damageInfo;
    private LayerMask enemyLayer;
    private HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();  // Prevents hitting same enemy twice per swing
    private bool isAttacking = false;  // Only deal damage when enabled
    
    // ==================== VFX ====================
    private GameObject slashVFXPrefab;
    private Vector3 vfxRotationOffset;
    private float vfxStartDelay;
    private float vfxDuration;

    // ==================== SLOW DEBUFF ====================
    private bool applySlow = false;
    private float slowPercent;
    private float slowDuration;

    // ==================== AUDIO ====================
    private AudioClip slashSound;
    private AudioSource audioSource;
    private float slashSoundVolume;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;  // 3D sound
    }
    
    /// <summary>
    /// Configures sword damage with all combat parameters.
    /// Called by PhantomKnight during Setup.
    /// </summary>
    public void Setup(DamageInfo newDamageInfo, LayerMask newEnemyLayer, GameObject vfxPrefab, Vector3 rotationOffset, float startDelay = 0f, float duration = 0.5f, bool applySlow = false, float slowPercent = 0f, float slowDuration = 0f, AudioClip slashSound = null, float slashSoundVolume = 1f)
    {
        damageInfo = newDamageInfo;
        enemyLayer = newEnemyLayer;
        slashVFXPrefab = vfxPrefab;
        vfxRotationOffset = rotationOffset;
        vfxStartDelay = startDelay;
        vfxDuration = duration;
        this.applySlow = applySlow;
        this.slowPercent = slowPercent;
        this.slowDuration = slowDuration;
        this.slashSound = slashSound;
        this.slashSoundVolume = slashSoundVolume;
        hitEnemies.Clear();
        isAttacking = false;
    }
    
    /// <summary>
    /// Enables sword damage. Called by PhantomKnight at start of attack window.
    /// </summary>
    public void EnableDamage()
    {
        isAttacking = true;
        hitEnemies.Clear();  // Reset hit tracking for new swing
        Debug.Log("Sword damage ENABLED");
    }
    
    /// <summary>
    /// Disables sword damage. Called by PhantomKnight at end of attack window.
    /// </summary>
    public void DisableDamage()
    {
        isAttacking = false;
        Debug.Log("Sword damage DISABLED");
    }
    
    /// <summary>
    /// Trigger collision - initial contact.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other);
    }
    
    /// <summary>
    /// Trigger stay - catches enemies that enter during swing.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        TryDealDamage(other);
    }
    
    /// <summary>
    /// Core damage logic - validates target, deals damage, spawns VFX, applies slow.
    /// </summary>
    private void TryDealDamage(Collider other)
    {
        // Only deal damage during active window
        if (!isAttacking) return;
    
        // Check if collider is on enemy layer (bitwise layer check)
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
    
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
    
        // Prevent hitting same enemy twice in one swing
        if (hitEnemies.Contains(enemy)) return;
    
        hitEnemies.Add(enemy);

        // Get closest point for VFX spawn location
        Vector3 hitPoint = other.ClosestPoint(transform.position);

        // ===== SPAWN SLASH VFX =====
        if (slashVFXPrefab != null)
        {
            // Orient VFX to face the enemy
            Vector3 directionToEnemy = (other.transform.position - transform.position).normalized;
            directionToEnemy.y = 0;

            Quaternion rotation = Quaternion.LookRotation(directionToEnemy) * Quaternion.Euler(vfxRotationOffset);

            StartCoroutine(SpawnVFX(hitPoint, rotation));
        }

        // ===== PLAY SOUND =====
        PlaySlashSound();

        // ===== DEAL DAMAGE =====
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageInfo);
        }
    
        // ===== APPLY SLOW (Spectral Chains upgrade) =====
        if (applySlow)
        {
            enemy.ApplySlow(slowPercent, slowDuration, false);
        }
    }
    
    /// <summary>
    /// Spawns slash VFX with optional delay.
    /// </summary>
    private IEnumerator SpawnVFX(Vector3 position, Quaternion rotation)
    {
        if (vfxStartDelay > 0)
        {
            yield return new WaitForSeconds(vfxStartDelay);
        }

        ObjectPooling.instance.GetVFX(slashVFXPrefab, position, rotation, vfxDuration);
    }

    /// <summary>
    /// Plays slash sound at sword position using SFXPlayer singleton.
    /// </summary>
    private void PlaySlashSound()
    {
        {
            if (slashSound != null && SFXPlayer.instance != null)
            {
                SFXPlayer.instance.Play(slashSound, transform.position, slashSoundVolume);
            }
        }
    }
}