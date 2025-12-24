using UnityEngine;

public class BladeDamage : MonoBehaviour
{
    private BladeTower tower;
    
    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    
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
            
            if (hitEffectPrefab != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                GameObject vfx = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
                Destroy(vfx, 1f);
            }
        }
    }
}