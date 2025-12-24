using System.Collections;
using UnityEngine;

public class BladeDamage : MonoBehaviour
{
    private BladeTower tower;
    
    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Transform hitPoint;
    [SerializeField] private Vector3 rotationOffset = new Vector3(90f, 0f, 0f);
    
    [Header("VFX Timing")]
    [SerializeField] private float skipStart = 0f;
    [SerializeField] private float vfxDuration = 1f;
    
    private void Start()
    {
        tower = GetComponentInParent<BladeTower>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        
        if (enemy != null && tower != null)
        {
            tower.OnBladeHit(enemy);
            
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
        
        GameObject vfx = Instantiate(hitEffectPrefab, hitPoint);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.Euler(rotationOffset);
        
        Destroy(vfx, vfxDuration);
    }
}