using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAdaptive : EnemyBase
{
    [Header("Adaptation Settings")]
    [SerializeField] private ElementType defaultElementType = ElementType.Physical;

    [Header("Visual Settings")]
    [SerializeField] private GameObject visualObject;
    [SerializeField] private float colorTransitionSpeed = 3f;

    private ElementType currentAdaptation;
    private FieldInfo elementTypeField;
    private Renderer enemyRenderer;
    private Color targetColor;
    private Color currentColor;

    private readonly Dictionary<ElementType, Color> elementColors = new Dictionary<ElementType, Color>()
    {
        { ElementType.Physical, new Color(0.6f, 0.6f, 0.6f) },      // Gray
        { ElementType.Magic, new Color(0.7f, 0.3f, 1f) },           // Purple
        { ElementType.Mechanic, new Color(1f, 0.7f, 0.2f) },        // Orange
        { ElementType.Imaginary, new Color(0.2f, 0.9f, 0.9f) }      // Cyan
    };

    protected override void Start()
    {
        base.Start();
        InitializeAdaptation();
    }

    private void InitializeAdaptation()
    {
        elementTypeField = typeof(EnemyBase).GetField("elementType", BindingFlags.NonPublic | BindingFlags.Instance);

        if (elementTypeField != null)
        {
            elementTypeField.SetValue(this, defaultElementType);
        }

        currentAdaptation = defaultElementType;

        if (visualObject != null)
        {
            enemyRenderer = visualObject.GetComponent<Renderer>();
            if (enemyRenderer != null && enemyRenderer.material != null)
            {
                currentColor = enemyRenderer.material.color;
                targetColor = elementColors.ContainsKey(defaultElementType)
                    ? elementColors[defaultElementType]
                    : currentColor;
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        UpdateColorTransition();
    }

    public override void TakeDamage(DamageInfo damageInfo,float vfxDamage = 0,bool spellDamageEnabled = false)
    {
        AdaptToElement(damageInfo.elementType);
        base.TakeDamage(damageInfo);
    }

    private void AdaptToElement(ElementType incomingElement)
    {
        bool isNewAdaptation = currentAdaptation != incomingElement;

        if (isNewAdaptation)
        {
            Debug.Log($"[EnemyAdaptive] Switching from {currentAdaptation} to {incomingElement}");

            currentAdaptation = incomingElement;

            if (elementTypeField != null)
            {
                elementTypeField.SetValue(this, incomingElement);
            }

            if (elementColors.ContainsKey(incomingElement))
            {
                targetColor = elementColors[incomingElement];
            }
        }
    }

    private void UpdateColorTransition()
    {
        if (enemyRenderer == null || enemyRenderer.material == null) return;

        if (Vector4.Distance(currentColor, targetColor) > 0.01f)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
            enemyRenderer.material.color = currentColor;
        }
    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();

        // Reset element type to default
        if (elementTypeField == null)
        {
            elementTypeField = typeof(EnemyBase).GetField("elementType", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    
        if (elementTypeField != null)
        {
            elementTypeField.SetValue(this, defaultElementType);
        }

        currentAdaptation = defaultElementType;

        // Reset color to default element color
        if (elementColors.ContainsKey(defaultElementType))
        {
            targetColor = elementColors[defaultElementType];
            if (enemyRenderer != null && enemyRenderer.material != null)
            {
                currentColor = targetColor;
                enemyRenderer.material.color = currentColor;
            }
        }
    }
}
