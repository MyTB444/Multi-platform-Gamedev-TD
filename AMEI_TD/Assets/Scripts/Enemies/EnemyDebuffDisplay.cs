using UnityEngine;
using UnityEngine.UI;

public class EnemyDebuffDisplay : MonoBehaviour
{
    [Header("Debuff Sprites")]
    private static Sprite slowSprite;
    private static Sprite freezeSprite;
    private static Sprite burnSprite;
    private static Sprite poisonSprite;
    private static Sprite bleedSprite;
    private static Sprite frostbiteSprite;
    private static Sprite ringSprite;  // Circular ring sprite
    private static bool spritesLoaded = false;
    
    [Header("Icon Colors")]
    private Color slowColor = new Color(0.5f, 0.8f, 1f);
    private Color freezeColor = new Color(0f, 1f, 1f);
    private Color burnColor = new Color(1f, 0.5f, 0f);
    private Color poisonColor = new Color(0.2f, 0.8f, 0.2f);
    private Color bleedColor = new Color(0.8f, 0f, 0f);
    private Color frostbiteColor = new Color(0.6f, 0.9f, 1f);
    private Color ringBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    
    [Header("Settings")]
    private float heightOffset = 2.5f;
    private float iconSize = 36f;
    private float ringSize = 48f;
    private float iconSpacing = 8f;
    
    private EnemyBase enemy;
    private Camera mainCamera;
    private RectTransform iconContainer;
    private Canvas canvas;
    
    // Active icons
    private DebuffIcon slowIcon;
    private DebuffIcon freezeIcon;
    private DebuffIcon burnIcon;
    private DebuffIcon poisonIcon;
    private DebuffIcon bleedIcon;
    private DebuffIcon frostbiteIcon;
    
    private class DebuffIcon
    {
        public GameObject root;
        public Image iconImage;
        public Image ringBackground;
        public Image ringFill;
    }
    
    public static void LoadSprites(Sprite slow, Sprite freeze, Sprite burn, Sprite poison, Sprite bleed, Sprite frostbite)
    {
        slowSprite = slow;
        freezeSprite = freeze;
        burnSprite = burn;
        poisonSprite = poison;
        bleedSprite = bleed;
        frostbiteSprite = frostbite;
        
        // Create ring sprite programmatically
        ringSprite = CreateRingSprite();
        
        spritesLoaded = true;
    }
    
    private static Sprite CreateRingSprite()
    {
        int size = 64;
        int thickness = 6;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
        
        // Fill with transparent
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                tex.SetPixel(x, y, transparent);
            }
        }
        
        // Draw ring
        float center = size / 2f;
        float outerRadius = size / 2f - 1;
        float innerRadius = outerRadius - thickness;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                
                if (dist <= outerRadius && dist >= innerRadius)
                {
                    // Smooth edges
                    float alpha = 1f;
                    if (dist > outerRadius - 1) alpha = outerRadius - dist;
                    if (dist < innerRadius + 1) alpha = Mathf.Min(alpha, dist - innerRadius);
                    alpha = Mathf.Clamp01(alpha);
                    
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
        }
        
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    
    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        mainCamera = Camera.main;
        CreateUI();
    }
    
    private void CreateUI()
    {
        GameObject canvasObj = new GameObject("DebuffCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0, heightOffset, 0);
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 50);
        
        GameObject containerObj = new GameObject("IconContainer");
        containerObj.transform.SetParent(canvasObj.transform);
        containerObj.transform.localPosition = Vector3.zero;
        containerObj.transform.localScale = Vector3.one;
        
        iconContainer = containerObj.AddComponent<RectTransform>();
        iconContainer.sizeDelta = new Vector2(200, 50);
        iconContainer.anchoredPosition = Vector2.zero;
    }
    
    private void LateUpdate()
    {
        if (enemy == null) return;
        
        if (mainCamera != null && canvas != null)
        {
            Vector3 lookDir = mainCamera.transform.forward;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                canvas.transform.forward = lookDir;
            }
        }
        
        UpdateIcons();
        UpdateRingFill();
    }
    
    private void UpdateIcons()
    {
        if (!spritesLoaded) return;
        
        SetIconActive(ref slowIcon, slowSprite, slowColor, enemy.HasIceSlow() && !enemy.IsFrozen());
        SetIconActive(ref freezeIcon, freezeSprite, freezeColor, enemy.IsFrozen());
        SetIconActive(ref burnIcon, burnSprite, burnColor, enemy.HasBurn());
        SetIconActive(ref poisonIcon, poisonSprite, poisonColor, enemy.HasPoison());
        SetIconActive(ref bleedIcon, bleedSprite, bleedColor, enemy.HasBleed());
        SetIconActive(ref frostbiteIcon, frostbiteSprite, frostbiteColor, enemy.HasFrostbite());
        
        RepositionIcons();
    }
    
    private void UpdateRingFill()
    {
        if (slowIcon != null) slowIcon.ringFill.fillAmount = enemy.GetSlowDurationNormalized();
        if (freezeIcon != null) freezeIcon.ringFill.fillAmount = enemy.GetFreezeDurationNormalized();
        if (burnIcon != null) burnIcon.ringFill.fillAmount = enemy.GetBurnDurationNormalized();
        if (poisonIcon != null) poisonIcon.ringFill.fillAmount = enemy.GetPoisonDurationNormalized();
        if (bleedIcon != null) bleedIcon.ringFill.fillAmount = enemy.GetBleedDurationNormalized();
        if (frostbiteIcon != null) frostbiteIcon.ringFill.fillAmount = enemy.GetFrostbiteDurationNormalized();
    }
    
    private void SetIconActive(ref DebuffIcon icon, Sprite sprite, Color color, bool active)
    {
        if (active && icon == null && sprite != null)
        {
            icon = CreateIcon(sprite, color);
        }
        else if (!active && icon != null)
        {
            Destroy(icon.root);
            icon = null;
        }
    }
    
    private DebuffIcon CreateIcon(Sprite sprite, Color color)
    {
        DebuffIcon icon = new DebuffIcon();
        
        // Root object
        icon.root = new GameObject("DebuffIcon");
        icon.root.transform.SetParent(iconContainer);
        icon.root.transform.localPosition = Vector3.zero;
        icon.root.transform.localRotation = Quaternion.identity;
        icon.root.transform.localScale = Vector3.one;
        
        RectTransform rootRT = icon.root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(ringSize, ringSize);
        
        // Ring background (dark)
        GameObject ringBgObj = new GameObject("RingBackground");
        ringBgObj.transform.SetParent(icon.root.transform);
        ringBgObj.transform.localPosition = Vector3.zero;
        ringBgObj.transform.localRotation = Quaternion.identity;
        ringBgObj.transform.localScale = Vector3.one;
        
        RectTransform ringBgRT = ringBgObj.AddComponent<RectTransform>();
        ringBgRT.sizeDelta = new Vector2(ringSize, ringSize);
        ringBgRT.anchoredPosition = Vector2.zero;
        
        icon.ringBackground = ringBgObj.AddComponent<Image>();
        icon.ringBackground.sprite = ringSprite;
        icon.ringBackground.color = ringBackgroundColor;
        icon.ringBackground.raycastTarget = false;
        
        // Ring fill (colored, drains)
        GameObject ringFillObj = new GameObject("RingFill");
        ringFillObj.transform.SetParent(icon.root.transform);
        ringFillObj.transform.localPosition = Vector3.zero;
        ringFillObj.transform.localRotation = Quaternion.identity;
        ringFillObj.transform.localScale = Vector3.one;
        
        RectTransform ringFillRT = ringFillObj.AddComponent<RectTransform>();
        ringFillRT.sizeDelta = new Vector2(ringSize, ringSize);
        ringFillRT.anchoredPosition = Vector2.zero;
        
        icon.ringFill = ringFillObj.AddComponent<Image>();
        icon.ringFill.sprite = ringSprite;
        icon.ringFill.color = color;
        icon.ringFill.raycastTarget = false;
        icon.ringFill.type = Image.Type.Filled;
        icon.ringFill.fillMethod = Image.FillMethod.Radial360;
        icon.ringFill.fillOrigin = 2; // Top
        icon.ringFill.fillClockwise = true;
        icon.ringFill.fillAmount = 1f;
        
        // Center icon (full color)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(icon.root.transform);
        iconObj.transform.localPosition = Vector3.zero;
        iconObj.transform.localRotation = Quaternion.identity;
        iconObj.transform.localScale = Vector3.one;
        
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(iconSize, iconSize);
        iconRT.anchoredPosition = Vector2.zero;
        
        icon.iconImage = iconObj.AddComponent<Image>();
        icon.iconImage.sprite = sprite;
        icon.iconImage.color = color;
        icon.iconImage.raycastTarget = false;
        
        return icon;
    }
    
    private void RepositionIcons()
    {
        DebuffIcon[] icons = new DebuffIcon[] { slowIcon, freezeIcon, burnIcon, poisonIcon, bleedIcon, frostbiteIcon };
        
        int activeCount = 0;
        foreach (var icon in icons)
        {
            if (icon != null) activeCount++;
        }
        
        if (activeCount == 0) return;
        
        float totalWidth = (activeCount * ringSize) + ((activeCount - 1) * iconSpacing);
        float startX = -totalWidth / 2f + ringSize / 2f;
        
        int index = 0;
        foreach (var icon in icons)
        {
            if (icon != null)
            {
                RectTransform rt = icon.root.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float xPos = startX + (index * (ringSize + iconSpacing));
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
        if (slowIcon != null) { Destroy(slowIcon.root); slowIcon = null; }
        if (freezeIcon != null) { Destroy(freezeIcon.root); freezeIcon = null; }
        if (burnIcon != null) { Destroy(burnIcon.root); burnIcon = null; }
        if (poisonIcon != null) { Destroy(poisonIcon.root); poisonIcon = null; }
        if (bleedIcon != null) { Destroy(bleedIcon.root); bleedIcon = null; }
        if (frostbiteIcon != null) { Destroy(frostbiteIcon.root); frostbiteIcon = null; }
    }
}