using UnityEngine;
using UnityEngine.UI;

public class EnemyDebuffDisplay : MonoBehaviour
{
    [Header("Debuff Sprites (Assign in Resources or Singleton)")]
    private static Sprite slowSprite;
    private static Sprite freezeSprite;
    private static Sprite burnSprite;
    private static Sprite poisonSprite;
    private static Sprite bleedSprite;
    private static Sprite frostbiteSprite;
    private static bool spritesLoaded = false;
    
    [Header("Icon Colors")]
    private Color slowColor = new Color(0.5f, 0.8f, 1f);
    private Color freezeColor = new Color(0f, 1f, 1f);
    private Color burnColor = new Color(1f, 0.5f, 0f);
    private Color poisonColor = new Color(0.2f, 0.8f, 0.2f);
    private Color bleedColor = new Color(0.8f, 0f, 0f);
    private Color frostbiteColor = new Color(0.6f, 0.9f, 1f);
    
    [Header("Settings")]
    private float heightOffset = 2.5f;
    private float iconSize = 30f;
    private float iconSpacing = 5f;
    
    private EnemyBase enemy;
    private Camera mainCamera;
    private RectTransform iconContainer;
    private Canvas canvas;
    
    // Active icons
    private GameObject slowIcon;
    private GameObject freezeIcon;
    private GameObject burnIcon;
    private GameObject poisonIcon;
    private GameObject bleedIcon;
    private GameObject frostbiteIcon;
    
    
    public static void LoadSprites(Sprite slow, Sprite freeze, Sprite burn, Sprite poison, Sprite bleed, Sprite frostbite)
    {
        slowSprite = slow;
        freezeSprite = freeze;
        burnSprite = burn;
        poisonSprite = poison;
        bleedSprite = bleed;
        frostbiteSprite = frostbite;
        spritesLoaded = true;
    }
    
    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        mainCamera = Camera.main;
        CreateUI();
    }
    
    private void CreateUI()
    {
        // Create canvas holder
        GameObject canvasObj = new GameObject("DebuffCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0, heightOffset, 0);
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    
        // Add canvas
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
    
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 50);
    
        // Create icon container
        GameObject containerObj = new GameObject("IconContainer");
        containerObj.transform.SetParent(canvasObj.transform);
        containerObj.transform.localPosition = Vector3.zero;
        containerObj.transform.localScale = Vector3.one;
    
        iconContainer = containerObj.AddComponent<RectTransform>();
        iconContainer.sizeDelta = new Vector2(100, 50);
        iconContainer.anchoredPosition = Vector2.zero;
    }
    
    private void LateUpdate()
    {
        if (enemy == null) return;
    
        // Billboard - only rotate on Y axis to stay level
        if (mainCamera != null && canvas != null)
        {
            Vector3 lookDir = mainCamera.transform.forward;
            lookDir.y = 0;  // Keep level
            if (lookDir.sqrMagnitude > 0.001f)
            {
                canvas.transform.forward = lookDir;
            }
        }
    
        UpdateIcons();
    }
    
    private void UpdateIcons()
    {
        if (!spritesLoaded) return;
    
        // Slow (ice only, not shown if frozen)
        SetIconActive(ref slowIcon, slowSprite, slowColor, enemy.HasIceSlow() && !enemy.IsFrozen());
    
        // Freeze
        SetIconActive(ref freezeIcon, freezeSprite, freezeColor, enemy.IsFrozen());
    
        // DoT types
        SetIconActive(ref burnIcon, burnSprite, burnColor, enemy.HasBurn());
        SetIconActive(ref poisonIcon, poisonSprite, poisonColor, enemy.HasPoison());
        SetIconActive(ref bleedIcon, bleedSprite, bleedColor, enemy.HasBleed());
        SetIconActive(ref frostbiteIcon, frostbiteSprite, frostbiteColor, enemy.HasFrostbite());  // ADD THIS
    
        RepositionIcons();
    }
    
    private void SetIconActive(ref GameObject icon, Sprite sprite, Color color, bool active)
    {
        if (active && icon == null && sprite != null)
        {
            icon = CreateIcon(sprite, color);
        }
        else if (!active && icon != null)
        {
            Destroy(icon);
            icon = null;
        }
    }
    
    private GameObject CreateIcon(Sprite sprite, Color color)
    {
        GameObject iconObj = new GameObject("DebuffIcon");
        iconObj.transform.SetParent(iconContainer);
        iconObj.transform.localPosition = Vector3.zero;
        iconObj.transform.localRotation = Quaternion.identity;
        iconObj.transform.localScale = Vector3.one;
    
        RectTransform rt = iconObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(iconSize, iconSize);
        rt.anchoredPosition = Vector2.zero;
        rt.localPosition = Vector3.zero;
    
        Image img = iconObj.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
    
        return iconObj;
    }
    
    private void RepositionIcons()
    {
        GameObject[] icons = new GameObject[] { slowIcon, freezeIcon, burnIcon, poisonIcon, bleedIcon, frostbiteIcon };
        
        int activeCount = 0;
        foreach (var icon in icons)
        {
            if (icon != null) activeCount++;
        }
        
        if (activeCount == 0) return;
        
        float totalWidth = (activeCount * iconSize) + ((activeCount - 1) * iconSpacing);
        float startX = -totalWidth / 2f + iconSize / 2f;
        
        int index = 0;
        foreach (var icon in icons)
        {
            if (icon != null)
            {
                RectTransform rt = icon.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float xPos = startX + (index * (iconSize + iconSpacing));
                    rt.anchoredPosition = new Vector2(xPos, 0);
                }
                index++;
            }
        }
    }
    
    private void OnDisable()
    {
        ClearAllIcons();
    }
    
    public void ClearAllIcons()
    {
        if (slowIcon != null) { Destroy(slowIcon); slowIcon = null; }
        if (freezeIcon != null) { Destroy(freezeIcon); freezeIcon = null; }
        if (burnIcon != null) { Destroy(burnIcon); burnIcon = null; }
        if (poisonIcon != null) { Destroy(poisonIcon); poisonIcon = null; }
        if (bleedIcon != null) { Destroy(bleedIcon); bleedIcon = null; }
        if (frostbiteIcon != null) { Destroy(frostbiteIcon); frostbiteIcon = null; }
    }
}