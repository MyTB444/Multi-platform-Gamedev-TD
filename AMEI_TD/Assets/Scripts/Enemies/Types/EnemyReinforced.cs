using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyReinforced : EnemyBase
{
    [Header("Shield Settings")]
    [SerializeField] private GameObject shieldEffectPrefab;
    [SerializeField] private float shieldAmount = 50f;
    [SerializeField] private float shieldRadius = 5f;
    [SerializeField] private float shieldCooldown = 10f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Reinforced Visual Effect")]
    [SerializeField] private Color armorTint = new Color(0.7f, 0.8f, 1f, 1f);
    [SerializeField] private Color pulseColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float metallicValue = 0.8f;
    [SerializeField] private float smoothnessValue = 0.9f;
    [SerializeField] private Renderer targetRenderer;

    private float nextShieldTime = 0f;
    private Material enemyMaterial;
    private Color originalColor;
    private float pulseTime;
    private float originalMetallic;
    private float originalSmoothness;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(InitializeAfterFrame());
    }

    private IEnumerator InitializeAfterFrame()
    {
        yield return null;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            enemyMaterial = targetRenderer.material;

            originalColor = enemyMaterial.color;

            if (enemyMaterial.HasProperty("_Metallic"))
            {
                originalMetallic = enemyMaterial.GetFloat("_Metallic");
            }
            if (enemyMaterial.HasProperty("_Glossiness") || enemyMaterial.HasProperty("_Smoothness"))
            {
                string smoothnessProperty = enemyMaterial.HasProperty("_Smoothness") ? "_Smoothness" : "_Glossiness";
                originalSmoothness = enemyMaterial.GetFloat(smoothnessProperty);
            }
            SetupArmoredMaterial(enemyMaterial);
        }
    }

    private void SetupArmoredMaterial(Material mat)
    {
        Color tintedColor = originalColor * armorTint;
        tintedColor.a = originalColor.a;
        mat.color = tintedColor;

        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", metallicValue);
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothnessValue);
        }
        else if (mat.HasProperty("_Glossiness"))
        {
            mat.SetFloat("_Glossiness", smoothnessValue);
        }
    }

    protected override void Update()
    {
        base.Update();
        ApplyArmorPulse();
        TryApplyShieldToNearbyEnemies();
    }

    private void ApplyArmorPulse()
    {
        if (enemyMaterial == null) return;

        pulseTime += Time.deltaTime * pulseSpeed;

        float t = (Mathf.Sin(pulseTime) + 1f) / 2f;

        Color baseColor = originalColor * armorTint;
        Color targetColor = originalColor * pulseColor;

        Color currentColor = Color.Lerp(baseColor, targetColor, t * 0.5f);
        currentColor.a = originalColor.a;

        enemyMaterial.color = currentColor;

        if (enemyMaterial.HasProperty("_Metallic"))
        {
            float metallicPulse = Mathf.Lerp(metallicValue * 0.7f, metallicValue, t);
            enemyMaterial.SetFloat("_Metallic", metallicPulse);
        }
    }

    private void TryApplyShieldToNearbyEnemies()
    {
        if (Time.time < nextShieldTime) return;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, shieldRadius, enemyLayer);
        bool shieldApplied = false;

        foreach (Collider col in nearbyColliders)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.HasShield())
            {
                enemy.ApplyShield(shieldAmount, shieldEffectPrefab);
                shieldApplied = true;
            }
        }

        if (shieldApplied)
        {
            nextShieldTime = Time.time + shieldCooldown;
            Debug.Log($"[EnemyReinforced] Applied shields to nearby enemies. Next shield in {shieldCooldown}s");
        }
    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();

        // Reset shield cooldown to allow shielding after spawn
        nextShieldTime = 0f;
        pulseTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shieldRadius);
    }

    private void OnDestroy()
    {
        if (enemyMaterial != null)
        {
            enemyMaterial.color = originalColor;

            if (enemyMaterial.HasProperty("_Metallic"))
            {
                enemyMaterial.SetFloat("_Metallic", originalMetallic);
            }

            if (enemyMaterial.HasProperty("_Smoothness"))
            {
                enemyMaterial.SetFloat("_Smoothness", originalSmoothness);
            }
            else if (enemyMaterial.HasProperty("_Glossiness"))
            {
                enemyMaterial.SetFloat("_Glossiness", originalSmoothness);
            }
        }
    }

    private void OnValidate()
    {
        if (targetRenderer == null && Application.isPlaying == false)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }
    }
}
