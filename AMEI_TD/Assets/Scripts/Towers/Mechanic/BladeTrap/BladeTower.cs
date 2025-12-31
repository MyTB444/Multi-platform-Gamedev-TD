using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BladeTower : TowerBase
{
    [Header("Blade Setup")]
    [SerializeField] private Transform bladeHolder;
    
    [Header("Spin Settings")]
    [SerializeField] private float spinSpeed = 360f;
    [SerializeField] private float returnSpeed = 180f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] [Range(0f, 0.9f)] private float momentumStrength = 0.5f;
    [SerializeField] private float momentumOffset = 90f;
    
    [Header("Damage Settings")]
    [SerializeField] private float damageCooldown = 0.5f;
    
    [Header("Animation")]
    [SerializeField] private float animationAnticipation = 0.5f;
    
    [Header("VFX")]
    [SerializeField] private Transform hammerImpactPoint;
    [SerializeField] private float vfxDelay = 0.3f;
    
    [Header("Blade Upgrades")]
    [SerializeField] [Range(0f, 0.5f)] private float spinSpeedBoostPercent = 0.25f;
    [Space]
    [SerializeField] private bool bleedChance = false;
    [SerializeField] [Range(0f, 1f)] private float bleedChancePercent = 0.3f;
    [SerializeField] private float bleedDamage = 3f;
    [SerializeField] private float bleedDuration = 4f;
    [SerializeField] private GameObject bleedVFX;
    [Space]
    [SerializeField] private bool moreBlades = false;
    [SerializeField] private GameObject[] extraBlades;
    [SerializeField] private Transform[] bladeVFXPoints;
    [Space]
    [SerializeField] private bool extendedReach = false;
    [SerializeField] private float extendedBladeScale = 1.5f;
    [SerializeField] private Transform[] allBlades;

    private List<GameObject> activeBleedVFXList = new List<GameObject>();

    private GameObject activeBleedVFX;
    
    private Dictionary<EnemyBase, float> recentlyHitEnemies = new Dictionary<EnemyBase, float>();
    private Quaternion startRotation;
    private bool isActive = false;
    private float baseSpinSpeed;
    private bool spinSpeedBoosted = false;
    private bool isReturning = false;
    private float currentAngle = 0f;
    private bool hasTriggeredAnimation = false;
    
    protected override void Start()
    {
        base.Start();

        baseSpinSpeed = spinSpeed;

        if (bladeHolder != null)
        {
            startRotation = bladeHolder.rotation;
        }

        ApplyUpgrades();
    }
    
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.BladeSpinSpeed:
                spinSpeedBoosted = enabled;
                if (enabled)
                {
                    spinSpeed = baseSpinSpeed * (1f + spinSpeedBoostPercent);
                }
                else
                {
                    spinSpeed = baseSpinSpeed;
                }
                break;
            case TowerUpgradeType.BleedChance:
                bleedChance = enabled;
                ClearBleedVFX();
                ApplyUpgrades();
                break;
            case TowerUpgradeType.MoreBlades:
                moreBlades = enabled;
                ClearBleedVFX();
                ApplyUpgrades();
                break;
            case TowerUpgradeType.ExtendedReach:
                extendedReach = enabled;
                ApplyUpgrades();
                break;
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

    private void ApplyUpgrades()
    {
        // Enable extra blades
        if (extraBlades != null)
        {
            foreach (GameObject blade in extraBlades)
            {
                if (blade != null)
                {
                    blade.SetActive(moreBlades);
                }
            }
        }
    
        // Spawn bleed VFX on blades
        if (bleedChance && bleedVFX != null && bladeVFXPoints != null)
        {
            foreach (Transform point in bladeVFXPoints)
            {
                // Skip VFX on extra blades if not enabled
                if (!moreBlades && point.parent != null && !point.parent.gameObject.activeSelf)
                {
                    continue;
                }

                GameObject vfx = ObjectPooling.instance.GetVFXWithParent(bleedVFX, point, -1f);
                vfx.transform.localPosition = Vector3.zero;
                activeBleedVFXList.Add(vfx);
            }
        }
        
        // Extended reach - scale blade Y to 1.5
        if (extendedReach && allBlades != null)
        {
            foreach (Transform blade in allBlades)
            {
                Vector3 currentScale = blade.localScale;
                blade.localScale = new Vector3(currentScale.x, extendedBladeScale, currentScale.z);
            }
        }
    }
    
    protected override void FixedUpdate()
    {
        UpdateDebuffs();
        UpdateDisabledVisual();
        
        if (isDisabled) return;
    
        CheckForEnemies();
    
        if (isActive)
        {
            isReturning = false;
            RotateBlades();
        }
        else if (isReturning || !IsAtStartRotation())
        {
            isReturning = true;
            ReturnToStart();
        }
    
        CleanupHitList();
    }
    
    private void CheckForEnemies()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, whatIsEnemy);
        isActive = enemies.Length > 0;
    }
    
    private bool IsAtStartRotation()
    {
        return Quaternion.Angle(bladeHolder.rotation, startRotation) < 1f;
    }
    
    private void RotateBlades()
    {
        if (bladeHolder == null) return;
    
        float adjustedAngle = currentAngle + momentumOffset;
        float momentumMultiplier = 1f + (Mathf.Sin(adjustedAngle * Mathf.Deg2Rad) * momentumStrength);
        
        float currentSpeed = spinSpeed * momentumMultiplier * slowMultiplier;
        float rotationThisFrame = currentSpeed * Time.fixedDeltaTime;
        
        currentAngle += rotationThisFrame;
        
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
        
        if (currentAngle >= 360f)
        {
            currentAngle -= 360f;
            hasTriggeredAnimation = false;
        }
        
        Vector3 axis = clockwise ? Vector3.right : Vector3.left;
        bladeHolder.Rotate(axis, rotationThisFrame);
    }
    
    private IEnumerator DelayedHammerVFX()
    {
        yield return new WaitForSeconds(vfxDelay);
        
        SpawnHammerImpactVFX();
    }
    
    public void SpawnHammerImpactVFX()
    {
        if (attackSpawnEffectPrefab != null && hammerImpactPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, hammerImpactPoint.position, Quaternion.identity, 2f);
        }
    }
    
    private void ReturnToStart()
    {
        if (bladeHolder == null) return;
        
        Vector3 axis = clockwise ? Vector3.right : Vector3.left;
        bladeHolder.Rotate(axis, returnSpeed * Time.fixedDeltaTime);
        
        if (IsAtStartRotation())
        {
            bladeHolder.rotation = startRotation;
            isReturning = false;
            currentAngle = 0f;
            hasTriggeredAnimation = false;
        }
    }
    
    private void CleanupHitList()
    {
        List<EnemyBase> toRemove = new List<EnemyBase>();
        
        foreach (var kvp in recentlyHitEnemies)
        {
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
    
    public void OnBladeHit(EnemyBase enemy)
    {
        if (enemy == null) return;

        if (recentlyHitEnemies.ContainsKey(enemy))
        {
            return;
        }

        IDamageable damageable = enemy.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(CreateDamageInfo());
        }
    
        // Bleed chance
        if (bleedChance && Random.value <= bleedChancePercent)
        {
            DamageInfo bleedDamageInfo = new DamageInfo(bleedDamage, elementType, true);
            enemy.ApplyDoT(bleedDamageInfo, bleedDuration, 0.5f, false, 0f, default, DebuffType.Bleed);
        }

        recentlyHitEnemies[enemy] = Time.time + damageCooldown;
    }
    
    protected override void HandleRotation() { }
    protected override void Attack() { }
    protected override bool CanAttack() { return false; }
    
    protected override void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}