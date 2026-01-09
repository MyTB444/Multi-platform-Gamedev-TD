using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SpellAbility;
using Random = UnityEngine.Random;

/// <summary>
/// Handles damage application from spell VFX effects. Attached to VFX prefabs (flames, magic areas).
/// Uses particle collision to detect and damage enemies.
/// </summary>
public class VFXDamage : MonoBehaviour
{
    // Current spell type determines damage behavior on collision
    private SpellType spellType;
    private EnemyBase enemyBaseGameObjectRef;

    // Flag to prevent multiple flame VFX spawns on same enemy
    public bool stopFlames { get; set; } = false;
    
    // Tracks which enemies have already been affected to prevent duplicate damage/VFX
    private Dictionary<EnemyBase, GameObject> EnemyDictionary = new();

    public bool stopMagic { get; set; } = false;
    private List<SkinnedMeshRenderer> skinnedMeshRenderer = new();
    private ParticleSystem ps;

    // List of enemies currently being affected by magic lift effect
    private List<EnemyBase> enemiesList = new();

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject tinyFlamesPrefab;

    [Header("Settings")]
    [SerializeField] private float magicDisableTime = 5f;        // How long magic area stays active
    [SerializeField] private float tinyFlamesDisableTime = 5f;   // How long flames burn on enemy
    [SerializeField] private float DamageMultiplier = 2f;        // Scales all damage from this VFX


    private void OnEnable()
    {
        // Cache current spell type from singleton - determines collision behavior
        spellType = instance.currenSpellType;

        // Reset state for object pool reuse
        enemyBaseGameObjectRef = null;
        EnemyDictionary.Clear();    
        stopFlames = false;
        ps = gameObject.GetComponent<ParticleSystem>();

        // Magic VFX has a fixed lifetime before auto-disable
        if (gameObject.CompareTag("MagicVfx"))
        {
            StartCoroutine(DisableVFXGameObject(magicDisableTime));
        }
    }

    #region Flames

    /// <summary>
    /// Applies burning damage to enemy and spawns small flame VFX on their mesh.
    /// Called when main flame particles collide with an enemy.
    /// </summary>
    /// <param name="other">The enemy GameObject that was hit</param>
    /// <param name="timeToEnableDamage">Delay before damage starts (for visual sync)</param>
    public IEnumerator EnableFlameDamage(GameObject other, float timeToEnableDamage)
    {
        // Safety checks for destroyed objects
        if (other.gameObject != null && other.gameObject.activeInHierarchy && gameObject != null)
        {
            // Only main flame VFX spawns tiny flames - prevent recursive spawning
            if (!gameObject.CompareTag("TinyFlames"))
            {
                yield return new WaitForSeconds(timeToEnableDamage);

                if (other.gameObject != null)
                {
                    skinnedMeshRenderer = other.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();

                    GameObject tinyFlames;

                    for (int i = 0; i < skinnedMeshRenderer.Count; i++)
                    {
                        // Check if this enemy already has flames (prevent stacking)
                        if (!EnemyDictionary.ContainsKey(enemyBaseGameObjectRef))
                        {
                            if (!stopFlames)
                            {
                                if (skinnedMeshRenderer[i] != null && enemyBaseGameObjectRef.vfxContainer.gameObject != null)
                                {
                                    Debug.Log("found");
                                    
                                    // Spawn 5 tiny flame particles on the enemy's mesh
                                    for (int j = 0; j <= 4; j++)
                                    {
                                        tinyFlames = ObjectPooling.instance.Get(tinyFlamesPrefab);
                                        if (tinyFlames != null)
                                        {
                                            // Parent to enemy so flames follow their movement
                                            tinyFlames.transform.parent = enemyBaseGameObjectRef.vfxContainer.gameObject.transform;
                                            // Position randomly within mesh bounds
                                            tinyFlames.transform.localPosition = ReturnRandomPointOnMesh(skinnedMeshRenderer[i].localBounds) - enemyBaseGameObjectRef.vfxContainer.gameObject.transform.localPosition;
                                            tinyFlames.transform.rotation = Quaternion.Euler(-90, 0, 0);
                                            tinyFlames.SetActive(true);
                                        }
                                    }
                                }
                            }
                        }
                        
                        // Register enemy as affected to prevent duplicate VFX
                        if (!EnemyDictionary.ContainsKey(enemyBaseGameObjectRef))
                        {
                            EnemyDictionary.Add(enemyBaseGameObjectRef, other);
                        }
                    }
                }
                
                // Apply burning damage (very small amount per frame, accumulates over time)
                if (other.gameObject != null && other.gameObject.activeInHierarchy)
                {
                    // Cleanup if enemy died
                    if (enemyBaseGameObjectRef.isDeadProperty)
                    {
                        EnemyDictionary.Remove(enemyBaseGameObjectRef);
                    }

                    // Parameters: direct damage, DoT damage, isDoT flag
                    enemyBaseGameObjectRef.TakeDamage(0f, 0.00003f * DamageMultiplier, true);
                }
            }
        }
    }

    /// <summary>
    /// Handles damage from tiny flames (the ones attached to burning enemies).
    /// These deal more damage than the initial flame collision.
    /// </summary>
    private void DisableTinyFlames(GameObject other)
    {
        if (gameObject != null)
        {
            if (gameObject.CompareTag("TinyFlames"))
            {
                Debug.Log("Takingdamage");
                
                if (enemyBaseGameObjectRef.isDeadProperty)
                {
                    EnemyDictionary.Remove(enemyBaseGameObjectRef);
                }

                // Tiny flames deal more damage than main flames (0.0005 vs 0.00003)
                enemyBaseGameObjectRef.TakeDamage(0f, 0.0005f * DamageMultiplier, true);

                // Start timer to return this flame to pool
                if (other.gameObject != null && other.gameObject.activeInHierarchy)
                {
                    StartCoroutine(DisableTinyFlamesAfterDelay(tinyFlamesDisableTime));
                }
            }
        }
    }

    /// <summary>
    /// Returns tiny flame VFX to object pool after burn duration expires.
    /// </summary>
    private IEnumerator DisableTinyFlamesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            ObjectPooling.instance.Return(gameObject);
        }
    }

    #endregion

    #region ParticleCollision

    /// <summary>
    /// Unity callback when this particle system's particles collide with another collider.
    /// Routes to appropriate damage handler based on current spell type.
    /// </summary>
    private void OnParticleCollision(GameObject other)
    {
        // Only process collisions with enemies
        if (other.gameObject.CompareTag("Enemy"))
        {
            enemyBaseGameObjectRef = other.gameObject.GetComponent<EnemyBase>();
            
            // Give enemy a reference back to this VFX for coordination
            enemyBaseGameObjectRef.GetRefOfVfxDamageScript(this);

            // Invisible enemies are immune to visible spell effects
            if (!enemyBaseGameObjectRef.isInvisible)
            {
                // Different spell types have different damage behaviors
                switch (spellType)
                {
                    case SpellType.Magic:
                        // Magic lifts enemies and deals damage over time while lifted
                        if (gameObject != null)
                        {
                            StartCoroutine(EnableLiftDamage(enemyBaseGameObjectRef));
                        }
                        break;

                    case SpellType.Physical:
                        // Fire burns enemies with flames
                        if (gameObject != null)
                        {
                            StartCoroutine(EnableFlameDamage(other, 1));
                        }
                        break;
                }
            }
            
            // Always process tiny flame damage regardless of spell type
            // (tiny flames are already attached to the enemy)
            DisableTinyFlames(other);
        }
    }

    #endregion

    #region ReturnPointOnMesh
    
    /// <summary>
    /// Returns a random point within mesh bounds for VFX positioning.
    /// Biased towards upper portion and center for better visual placement.
    /// </summary>
    private Vector3 ReturnRandomPointOnMesh(Bounds bounds)
    {
        return new Vector3(
            bounds.center.x,
            Random.Range(bounds.extents.y, bounds.extents.y / 3f),        // Upper third
            Random.Range(-bounds.extents.z / 10f, bounds.extents.z / 8f)  // Slight z variance
        );
    }
    #endregion

    #region Magic
    
    /// <summary>
    /// Magic spell damage: lifts enemy into the air and applies damage over time
    /// while they're suspended. Enemy returns to normal after duration expires.
    /// </summary>
    public IEnumerator EnableLiftDamage(EnemyBase enemyBase)
    {
        // Safety checks for destroyed objects
        if (enemyBase.gameObject != null && enemyBase.gameObject.activeInHierarchy && enemyBase != null)
        {
            // Enable lift visual effects (but not physics-based lift like mechanic spell)
            enemyBase.LiftEffectFunction(true, false, enemyBase);
            enemiesList.Add(enemyBaseGameObjectRef);

            // Calculate lift target position (2 units above current)
            Vector3 startPos = enemyBase.transform.position;
            Vector3 targetPos = new Vector3(startPos.x, enemyBase.transform.position.y + 2, startPos.z);
        
            float duration = 1f;
            float elapsed = 0f;

            // ===== PHASE 1: Lift enemy to target height =====
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Only continue lifting if below target
                if (enemyBase.transform.position.y <= targetPos.y)
                {
                    enemyBase.transform.position = Vector3.Lerp(startPos, targetPos, t);
                }
                else
                {
                    break;  // Already at or above target height
                }
                yield return null; 
            }
            enemyBase.transform.position = targetPos;
            
            // ===== PHASE 2: Hold enemy in air and deal damage over time =====
            duration = 2f;
            elapsed = 0f;
            
            if (enemyBase.transform.position.y >= targetPos.y)
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    
                    if (enemyBase != null)
                    {
                        if (!enemyBase.isDeadProperty)
                        {
                            Debug.Log("123");
                            // Apply continuous damage while suspended
                            enemyBase.TakeDamage(0f, 0.001f * DamageMultiplier, true);
                        }
                    }
                    
                    // Stop if enemy died
                    if (enemyBase.isDeadProperty)
                    { 
                        enemyBase = null;
                        enemiesList.Remove(enemyBase);
                        break;
                    }
                    yield return null;  
                }
            }
        }
    }
    #endregion

    #region DisableVFX

    /// <summary>
    /// Disables this VFX after its duration expires. Cleans up any affected enemies
    /// by removing lift effects before returning to object pool.
    /// </summary>
    private IEnumerator DisableVFXGameObject(float disableTime)
    {
        yield return new WaitForSeconds(disableTime);
        
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            // Release all enemies that were being lifted by this magic area
            foreach (EnemyBase o in enemiesList)
            {
                StopCoroutine(EnableLiftDamage(o));
                o.LiftEffectFunction(false, false, o);  // Disable lift effects
            }
            enemiesList.Clear();
           
            ObjectPooling.instance.Return(gameObject);
        }
    }
    #endregion

    #region Getters
    public float GetDamageMultilpier() => DamageMultiplier;
    public List<EnemyBase> GetAffectedEnemyList() => enemiesList;
    #endregion
}