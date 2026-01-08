using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BladeApparatus : MonoBehaviour
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
    [SerializeField] private GameObject attackSpawnEffectPrefab;
    [SerializeField] private float vfxDelay = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip bladeHitSound;
    [SerializeField] [Range(0f, 1f)] private float bladeHitSoundVolume = 1f;
    
    [Header("Upgrade Objects")]
    [SerializeField] private GameObject[] extraBlades;
    [SerializeField] private Transform[] bladeVFXPoints;
    [SerializeField] private Transform[] allBlades;
    
    private BladeTower tower;
    private DamageInfo damageInfo;
    private float attackRange;
    private LayerMask whatIsEnemy;
    private Animator characterAnimator;
    private string attackAnimationTrigger;
    private AudioSource audioSource;
    
    // Upgrade states
    private bool bleedChance = false;
    private float bleedChancePercent = 0.3f;
    private float bleedDamage = 3f;
    private float bleedDuration = 4f;
    private ElementType elementType;
    private GameObject bleedVFX;
    
    private bool moreBlades = false;
    private bool extendedReach = false;
    private float extendedBladeScale = 1.5f;
    
    private List<GameObject> activeBleedVFXList = new List<GameObject>();
    private Dictionary<EnemyBase, float> recentlyHitEnemies = new Dictionary<EnemyBase, float>();
    
    private Quaternion startRotation;
    private bool isActive = false;
    private float baseSpinSpeed;
    private bool isReturning = false;
    private float currentAngle = 0f;
    private bool hasTriggeredAnimation = false;
    
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
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }
    
    public void UpdateDamageInfo(DamageInfo newDamageInfo)
    {
        damageInfo = newDamageInfo;
    }
    
    private void Update()
    {
        if (tower != null && tower.IsDisabled()) return;
        
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
    
    private bool IsAtStartRotation()
    {
        return Quaternion.Angle(bladeHolder.rotation, startRotation) < 1f;
    }
    
    private void RotateBlades()
    {
        if (bladeHolder == null) return;
        
        float slowMultiplier = tower != null ? tower.GetSlowMultiplier() : 1f;
        
        float adjustedAngle = currentAngle + momentumOffset;
        float momentumMultiplier = 1f + (Mathf.Sin(adjustedAngle * Mathf.Deg2Rad) * momentumStrength);
        
        float currentSpeed = spinSpeed * momentumMultiplier * slowMultiplier;
        float rotationThisFrame = currentSpeed * Time.deltaTime;
        
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
        if (tower != null)
        {
            tower.PlayAttackSoundFromApparatus();
        }
        
        if (attackSpawnEffectPrefab != null && hammerImpactPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, hammerImpactPoint.position, Quaternion.identity, 2f);
        }
    }
    
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
            damageable.TakeDamage(damageInfo);
        }
        
        PlayBladeHitSound();
        
        if (bleedChance && Random.value <= bleedChancePercent)
        {
            DamageInfo bleedDamageInfo = new DamageInfo(bleedDamage, elementType, true);
            enemy.ApplyDoT(bleedDamageInfo, bleedDuration, 0.5f, false, 0f, default, DebuffType.Bleed);
        }
        
        recentlyHitEnemies[enemy] = Time.time + damageCooldown;
    }
    
    private void PlayBladeHitSound()
    {
        if (bladeHitSound != null && audioSource != null)
        {
            audioSource.clip = bladeHitSound;
            audioSource.volume = bladeHitSoundVolume;
            audioSource.Play();
        }
    }
    
    // Upgrade methods (like SpikeTrapDamage)
    
    public void SetSpinSpeed(float newSpinSpeed)
    {
        spinSpeed = newSpinSpeed;
    }
    
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
    
    private void SpawnBleedVFX()
    {
        if (bleedVFX == null || bladeVFXPoints == null) return;
        
        foreach (Transform point in bladeVFXPoints)
        {
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
        
        // Refresh bleed VFX if active
        if (bleedChance)
        {
            ClearBleedVFX();
            SpawnBleedVFX();
        }
    }
    
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
    
    public float GetAttackRange() => attackRange;
    public bool IsActive() => isActive;
}