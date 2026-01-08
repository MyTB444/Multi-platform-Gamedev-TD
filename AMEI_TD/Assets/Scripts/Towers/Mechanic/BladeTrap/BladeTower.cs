using UnityEngine;

public class BladeTower : TowerBase
{
    [Header("Blade Spawning")]
    [SerializeField] private GameObject bladeApparatusPrefab;
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask roadLayer;
    [SerializeField] private float downwardAngle = 45f;
    [SerializeField] private float baseSpinSpeed = 200f;
    [SerializeField] private float apparatusRotationOffset = 90f;
    [SerializeField] private float heightOffset = 1.42f;
    [SerializeField] private float apparatusScale = 0.6666667f;
    
    [Header("VFX")]
    [SerializeField] private Transform hammerImpactPoint;
    
    [Header("Road Centering")]
    [SerializeField] private bool autoCenter = true;
    [SerializeField] private bool debugDrawRays = false;
    
    [Header("Blade Upgrades")]
    [SerializeField] [Range(0f, 0.5f)] private float spinSpeedBoostPercent = 0.2f;
    [Space]
    [SerializeField] private bool bleedChance = false;
    [SerializeField] [Range(0f, 1f)] private float bleedChancePercent = 0.45f;
    [SerializeField] private float bleedDamage = 15f;
    [SerializeField] private float bleedDuration = 4f;
    [SerializeField] private GameObject bleedVFX;
    [Space]
    [SerializeField] private bool moreBlades = false;
    [Space]
    [SerializeField] private bool extendedReach = false;
    [SerializeField] private float extendedBladeScale = 1.5f;
    
    private BladeApparatus bladeApparatus;
    private bool spinSpeedBoosted = false;
    
    protected override void Awake()
    {
        base.Awake();
        SpawnBladeOnRoad();
    }
    
    private void SpawnBladeOnRoad()
    {
        Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, transform.right) * transform.forward;
        
        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, raycastDistance, roadLayer))
        {
            Vector3 spawnPosition;
            
            if (autoCenter)
            {
                spawnPosition = GetTileCenterPosition(hit.point, hit.collider);
            }
            else
            {
                spawnPosition = hit.point;
            }
            
            Quaternion spawnRotation = GetBladeRotation();
            
            GameObject apparatus = Instantiate(bladeApparatusPrefab, spawnPosition, spawnRotation);
            Vector3 originalScale = apparatus.transform.localScale;

            bladeApparatus = apparatus.GetComponent<BladeApparatus>();
            bladeApparatus.Setup(CreateDamageInfo(), attackRange, whatIsEnemy, this, characterAnimator, attackAnimationTrigger);
            
            // Apply current upgrades
            if (spinSpeedBoosted)
            {
                bladeApparatus.SetSpinSpeed(baseSpinSpeed * (1f + spinSpeedBoostPercent));
            }
            
            if (bleedChance)
            {
                bladeApparatus.SetBleedEffect(bleedChancePercent, bleedDamage, bleedDuration, elementType, bleedVFX);
            }
            
            if (moreBlades)
            {
                bladeApparatus.SetMoreBlades(true);
            }
            
            if (extendedReach)
            {
                bladeApparatus.SetExtendedReach(true, extendedBladeScale);
            }
            
            apparatus.transform.SetParent(transform);
            apparatus.transform.localScale = originalScale;
        }
        else
        {
            Debug.LogWarning("BladeTower: No road found!");
        }
    }
    
    public void PlayHammerEffect()
    {
        PlayAttackSound();
    
        if (attackSpawnEffectPrefab != null && hammerImpactPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, hammerImpactPoint.position, Quaternion.identity, 2f);
        }
    }
    
    private Vector3 GetTileCenterPosition(Vector3 hitPoint, Collider roadCollider)
    {
        Vector3 tileCenter = roadCollider.transform.position;
    
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
    
        Vector3 spawnPosition;
    
        if (Mathf.Abs(forward.z) > Mathf.Abs(forward.x))
        {
            spawnPosition = new Vector3(hitPoint.x, hitPoint.y + heightOffset, tileCenter.z);
        }
        else
        {
            spawnPosition = new Vector3(tileCenter.x, hitPoint.y + heightOffset, hitPoint.z);
        }
    
        return spawnPosition;
    }
    
    private Quaternion GetBladeRotation()
    {
        Vector3 roadDirection = transform.forward;
        roadDirection.y = 0;
        roadDirection.Normalize();
    
        Quaternion baseRotation = Quaternion.LookRotation(roadDirection, Vector3.up);
        return baseRotation * Quaternion.Euler(0f, apparatusRotationOffset, 0f);
    }
    
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
        
        switch (upgradeType)
        {
            case TowerUpgradeType.BladeSpinSpeed:
                spinSpeedBoosted = enabled;
                if (bladeApparatus != null)
                {
                    float newSpeed = enabled ? baseSpinSpeed * (1f + spinSpeedBoostPercent) : baseSpinSpeed;
                    bladeApparatus.SetSpinSpeed(newSpeed);
                }
                break;
                
            case TowerUpgradeType.BleedChance:
                bleedChance = enabled;
                if (bladeApparatus != null)
                {
                    if (enabled)
                    {
                        bladeApparatus.SetBleedEffect(bleedChancePercent, bleedDamage, bleedDuration, elementType, bleedVFX);
                    }
                    else
                    {
                        bladeApparatus.ClearBleedEffect();
                    }
                }
                break;
                
            case TowerUpgradeType.MoreBlades:
                moreBlades = enabled;
                if (bladeApparatus != null)
                {
                    bladeApparatus.SetMoreBlades(enabled);
                }
                break;
                
            case TowerUpgradeType.ExtendedReach:
                extendedReach = enabled;
                if (bladeApparatus != null)
                {
                    bladeApparatus.SetExtendedReach(enabled, extendedBladeScale);
                }
                break;
        }
    }
    
    public void PlayAttackSoundFromApparatus()
    {
        PlayAttackSound();
    }
    
    protected override void HandleRotation() { }
    protected override void Attack() { }
    protected override bool CanAttack() { return false; }
    
    protected override void OnDrawGizmos()
    {
        // Draw range from apparatus position if it exists, otherwise from tower
        Vector3 rangeCenter = (bladeApparatus != null) ? bladeApparatus.transform.position : transform.position;
        bool isActive = bladeApparatus != null && bladeApparatus.IsActive();
    
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(rangeCenter, attackRange);
    
        // Draw raycast
        Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, transform.right) * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, rayDirection * raycastDistance);
    
        if (debugDrawRays && Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, raycastDistance, roadLayer))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(hit.point, 0.2f);
        
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(GetTileCenterPosition(hit.point, hit.collider), 0.35f);
        }
    }
}