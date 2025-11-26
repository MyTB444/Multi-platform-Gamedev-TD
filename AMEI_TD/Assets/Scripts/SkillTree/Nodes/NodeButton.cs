using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class NodeButton : MonoBehaviour, IPointerEnterHandler
{
    [Header("Main")]
    [Tooltip("ScriptableObject")]
    public SkillNode skill;

    [Tooltip("Manager")]
    public SkillTreeManager manager;

    [Header("UI References")]
    public Button button;
    public Image backgroundImage;
    public TextMeshProUGUI descriptionText;
    public GameObject lockedOverlay;
    public GameObject unlockedIndicator;

    [Header("Colors")]
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f);
    public Color availableColor = new Color(1f, 1f, 1f);
    public Color unlockedColor = new Color(0.3f, 1f, 0.3f);

    private void Start()
    {
        // Auto-find components if not assigned
        if (button == null)
            button = GetComponent<Button>();
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        // Setup initial state
        SetupVisuals();
        UpdateVisuals();

        // Add click listener
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        // Subscribe to manager events
        if (manager != null)
        {
            manager.OnSkillUnlocked.AddListener(OnAnySkillChanged);
            manager.OnSkillLocked.AddListener(OnAnySkillChanged);
            manager.OnTreeReset.AddListener(OnTreeReset);

        }
    }
    private void SetupVisuals()
    {
        if (skill == null) return;

        // Set icon
        if (backgroundImage != null && skill.icon != null)
        {
            backgroundImage.sprite = skill.icon;
        }
    }

    public void UpdateVisuals()
    {
        if (skill == null || manager == null) return;

        bool isUnlocked = manager.IsSkillUnlocked(skill);
        bool canUnlock = manager.CanUnlockSkill(skill);

        // Update button interactability
        if (button != null)
        {
            button.interactable = isUnlocked || canUnlock;
        }

        // Update background color
        if (backgroundImage != null)
        {
            if (isUnlocked)
                backgroundImage.color = unlockedColor;
            else if (canUnlock)
                backgroundImage.color = availableColor;
            else
                backgroundImage.color = lockedColor;
        }

        // Update overlays
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked && !canUnlock);
        }

        if (unlockedIndicator != null)
        {
            unlockedIndicator.SetActive(isUnlocked);
        }
    }
    private void OnTreeReset()
    {
        UpdateVisuals();
    }
    private void OnButtonClick()
    {
        if (manager == null || skill == null) return;

        // Try to unlock
        if (!manager.IsSkillUnlocked(skill))
        {
            manager.TryUnlockSkill(skill);
        }
        // or lock
        else
        {
            manager.TryLockSkill(skill);
        }
    }
    private void OnAnySkillChanged(SkillNode changedSkill)
    {
        UpdateVisuals();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        SkillDescriptionUpdater.instance.UpdateTexts(skill);
    }
    // public void OnPointerExit(PointerEventData eventData)
    //{
    //}

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (manager != null)
        {
            manager.OnSkillUnlocked.RemoveListener(OnAnySkillChanged);
            manager.OnSkillLocked.RemoveListener(OnAnySkillChanged);
            manager.OnTreeReset.RemoveListener(OnTreeReset);
        }
    }
}