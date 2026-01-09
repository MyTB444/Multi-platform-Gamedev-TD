using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyInvisible : EnemyBase
{
    [Header("Invisibility Effect")]
    [SerializeField] private float flickerSpeed = 3f;
    [SerializeField] private float minAlpha = 0.1f;
    [SerializeField] private float maxAlpha = 0.5f;
    [SerializeField] private Renderer targetRenderer;

    private Material enemyMaterial;
    private float flickerTime;

    protected override void OnEnable()
    {
        base.OnEnable();
     
        isInvisible = true;
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(InitializeAfterFrame());
    }

    private IEnumerator InitializeAfterFrame()
    {
        // Wait one frame to ensure renderer is fully initialized before setting up transparency
        yield return null;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            enemyMaterial = targetRenderer.material;

            SetupTransparentMaterial(enemyMaterial);
        }
    }

    private void SetupTransparentMaterial(Material mat)
    {
        // Configure material for transparent rendering mode
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    protected override void Update()
    {
        base.Update();
        ApplyFlickerEffect();
    }

    private void ApplyFlickerEffect()
    {
        // Animate alpha between min and max using sine wave for subtle pulsing
        if (enemyMaterial == null) return;
        if (isInvisible)
        {
            flickerTime += Time.deltaTime * flickerSpeed;

            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(flickerTime) + 1f) / 2f);

            Color currentColor = enemyMaterial.color;
            currentColor.a = alpha;
            enemyMaterial.color = currentColor;
        }
    }

    private void OnValidate()
    {
        if (targetRenderer == null && Application.isPlaying == false)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }
    }

    public void DisableInvisible()
    {

    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();
        flickerTime = 0f;
    }
}
