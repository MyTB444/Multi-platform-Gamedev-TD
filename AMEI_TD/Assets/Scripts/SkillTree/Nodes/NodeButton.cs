using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// UI component for a skill tree node button. Handles visual states (locked/available/unlocked),
/// click interactions, and event subscriptions to keep visuals in sync with skill state.
/// 
/// Attach to a UI Button that represents a skill in the skill tree.
/// </summary>
public class NodeButton : MonoBehaviour, IPointerEnterHandler
{
    [Header("Main")]
    [Tooltip("The SkillNode ScriptableObject this button represents")]
    public SkillNode skill;

    [Tooltip("Reference to the SkillTreeManager")]
    public SkillTreeManager manager;

    [Header("UI References")]
    public Button button;
    public Image[] backgroundImage;        // [0] = icon image, [1] = background/border
    public TextMeshProUGUI descriptionText;
    public GameObject lockedOverlay;       // Shown when skill is locked and unavailable
    public GameObject unlockedIndicator;   // Shown when skill is unlocked

    [Header("Colors")]
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f);      // Grey - can't unlock yet
    public Color availableColor = new Color(1f, 1f, 1f);          // White - can unlock
    public Color unlockedColor = new Color(1f, 0.2196f, 0f);      // Orange - already unlocked

    private void Start()
    {
        // Auto-find Button component if not assigned
        if (button == null)
            button = GetComponent<Button>();
        
        // Get all Image components (icon and background)
        backgroundImage = GetComponentsInChildren<Image>();
        
        // Initialize visuals
        SetupVisuals();
        UpdateVisuals();

        // Register click handler
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        // ===== EVENT SUBSCRIPTIONS =====
        // Subscribe to manager events so we update when ANY skill changes
        // (other skills unlocking may affect our prerequisites/availability)
        if (manager != null)
        {
            manager.OnSkillUnlocked.AddListener(OnAnySkillChanged);
            manager.OnSkillLocked.AddListener(OnAnySkillChanged);
            manager.OnTreeReset.AddListener(OnTreeReset);
            manager.GainedPoints.AddListener(UpdateVisuals);  // Points change may make us affordable
        }
    }
    
    /// <summary>
    /// Sets up static visuals (icon) from the SkillNode data.
    /// </summary>
    private void SetupVisuals()
    {
        if (skill == null) return;

        // Set the skill icon
        if (backgroundImage != null && skill.icon != null)
        {
            backgroundImage[0].sprite = skill.icon;
        }
    }

    /// <summary>
    /// Updates all dynamic visuals based on current skill state.
    /// Called on start and whenever skills/points change.
    /// </summary>
    public void UpdateVisuals()
    {
        if (skill == null || manager == null) return;

        bool isUnlocked = manager.IsSkillUnlocked(skill);
        bool canUnlock = manager.CanUnlockSkill(skill);

        // ===== BUTTON INTERACTABILITY =====
        // Can interact if: already unlocked (to lock it) OR can unlock it
        if (button != null)
        {
            button.interactable = isUnlocked || canUnlock;
        }

        // ===== COLOR STATES =====
        if (backgroundImage != null)
        {
            if (isUnlocked)
            {
                // Orange - skill is active
                backgroundImage[0].color = unlockedColor;
                backgroundImage[1].color = unlockedColor;
            }
            else if (canUnlock)
            {
                // White - skill is available to purchase
                backgroundImage[0].color = availableColor;
                backgroundImage[1].color = Color.white;
            }
            else
            {
                // Grey - skill is locked (missing prereqs or points)
                backgroundImage[0].color = lockedColor;
                backgroundImage[1].color = Color.white;
            }
        }

        // ===== OVERLAY STATES =====
        // Locked overlay shows when skill cannot be interacted with
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked && !canUnlock);
        }

        // Unlocked indicator shows when skill is active
        if (unlockedIndicator != null)
        {
            unlockedIndicator.SetActive(isUnlocked);
        }
    }
    
    /// <summary>
    /// Called when the skill tree is reset.
    /// </summary>
    private void OnTreeReset()
    {
        UpdateVisuals();
    }
    
    /// <summary>
    /// Button click handler.
    /// </summary>
    private void OnButtonClick()
    {
        ButtonEvent();
    }
    
    /// <summary>
    /// Handles the button press logic - toggles skill unlock/lock state.
    /// </summary>
    public void ButtonEvent()
    {
        if (manager == null || skill == null) return;

        // If not unlocked, try to unlock
        if (!manager.IsSkillUnlocked(skill))
        {
            manager.TryUnlockSkill(skill);
        }
        // If already unlocked, try to lock (refund)
        else
        {
            manager.TryLockSkill(skill);
        }
    }
    
    /// <summary>
    /// Called when ANY skill is unlocked or locked.
    /// Updates our visuals in case our availability changed.
    /// </summary>
    private void OnAnySkillChanged(SkillNode changedSkill)
    {
        UpdateVisuals();
    }
    
    /// <summary>
    /// IPointerEnterHandler - called when mouse hovers over this button.
    /// Updates the skill description panel with this skill's info.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        SkillDescriptionUpdater.instance.UpdateTexts(skill);
    }

    /// <summary>
    /// Cleanup event subscriptions when destroyed.
    /// Prevents memory leaks and null reference errors.
    /// </summary>
    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.OnSkillUnlocked.RemoveListener(OnAnySkillChanged);
            manager.OnSkillLocked.RemoveListener(OnAnySkillChanged);
            manager.OnTreeReset.RemoveListener(OnTreeReset);
        }
    }
    
    /// <summary>
    /// Returns the SkillNode this button represents.
    /// Used by CircleNode to check which skill was chosen.
    /// </summary>
    public SkillNode GetSkill()
    {
        return skill;
    }
}