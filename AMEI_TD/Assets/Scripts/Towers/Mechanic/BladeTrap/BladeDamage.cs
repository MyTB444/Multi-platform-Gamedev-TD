using System.Collections;
using UnityEngine;

public class BladeDamage : MonoBehaviour
{
    private BladeApparatus apparatus;
    
    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Transform hitPoint;
    [SerializeField] private Vector3 rotationOffset = new Vector3(90f, 0f, 0f);
    
    [Header("VFX Timing")]
    [SerializeField] private float skipStart = 0f;
    [SerializeField] private float vfxDuration = 1f;
    
    private void Start()
    {
        apparatus = GetComponentInParent<BladeApparatus>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        
        if (enemy != null && apparatus != null)
        {
            apparatus.OnBladeHit(enemy);
            
            if (hitEffectPrefab != null && hitPoint != null)
            {
                StartCoroutine(SpawnVFX());
            }
        }
    }
    
    private IEnumerator SpawnVFX()
    {
        if (skipStart > 0)
        {
            yield return new WaitForSeconds(skipStart);
        }

        GameObject vfx = ObjectPooling.instance.GetVFXWithParent(hitEffectPrefab, hitPoint, vfxDuration);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.Euler(rotationOffset);
    }
}