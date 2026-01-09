using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger collider component attached to each blade.
/// Detects enemy collisions and forwards them to BladeApparatus for damage handling.
/// Spawns hit VFX at blade contact point.
/// </summary>
public class BladeDamage : MonoBehaviour
{
    private BladeApparatus apparatus;  // Parent apparatus handles damage logic
    
    // ==================== VFX ====================
    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Transform hitPoint;                          // VFX spawn location
    [SerializeField] private Vector3 rotationOffset = new Vector3(90f, 0f, 0f);  // VFX orientation
    
    [Header("VFX Timing")]
    [SerializeField] private float skipStart = 0f;    // Delay before spawning VFX
    [SerializeField] private float vfxDuration = 1f;  // How long VFX stays active
    
    private void Start()
    {
        // Find parent apparatus for damage callback
        apparatus = GetComponentInParent<BladeApparatus>();
    }
    
    /// <summary>
    /// Detects enemy contact and forwards to apparatus.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        
        if (enemy != null && apparatus != null)
        {
            // Forward hit to apparatus for damage + cooldown handling
            apparatus.OnBladeHit(enemy);
            
            // Spawn hit VFX
            if (hitEffectPrefab != null && hitPoint != null)
            {
                StartCoroutine(SpawnVFX());
            }
        }
    }
    
    /// <summary>
    /// Spawns hit VFX with optional delay and custom rotation.
    /// </summary>
    private IEnumerator SpawnVFX()
    {
        // Optional delay before VFX
        if (skipStart > 0)
        {
            yield return new WaitForSeconds(skipStart);
        }

        // Spawn VFX parented to hit point, with rotation offset
        GameObject vfx = ObjectPooling.instance.GetVFXWithParent(hitEffectPrefab, hitPoint, vfxDuration);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.Euler(rotationOffset);
    }
}