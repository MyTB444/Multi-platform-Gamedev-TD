using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tower swap types - determines which alternative tower replaces the default.
/// </summary>
public enum SwapType
{
    Null,       // No swap
    Spear,      // Physical element swap tower
    Knight,     // Imaginary element swap tower
    IceMage,    // Magic element swap tower
    BladeTower  // Mechanic element swap tower
}

/// <summary>
/// Spell unlock types - which element's spell button to enable.
/// </summary>
public enum SpellEnableType
{
    Null,       // No spell unlock
    Physical,   // Fire spell
    Magical,    // Magic spell
    Mechanical, // Mechanic/bomb spell
    Imaginary   // Reveal spell
}

/// <summary>
/// ScriptableObject representing a skill tree node. Contains unlock cost, prerequisites,
/// and effects to apply (tower upgrades, tower swaps, spell unlocks, etc.)
/// 
/// Create via: Assets > Create > Skill Tree > Skill Node
/// 
/// Each skill can:
/// - Unlock tower upgrades (function, function2)
/// - Swap tower types (swapType)
/// - Enable spells (spellEnableType)
/// - Spawn the Guardian (noah)
/// - Disable/restore other upgrades based on path taken
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill Tree/Skill Node")]
public class SkillNode : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public string skillCost;           // Display string for UI (e.g., "50 SP")
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public bool irreversible;          // If true, cannot be manually locked/refunded

    [Header("Requirements")]
    public int skillPointCost;         // Actual cost in skill points
    public List<SkillNode> prerequisites = new List<SkillNode>();  // Must unlock these first

    [Header("Functionalities")]
    public TowerUpgradeType function;      // Primary tower upgrade to unlock
    public TowerUpgradeType function2;     // Secondary upgrade (for skills that grant two)
    public SwapType swapType;              // Tower swap to perform (if any)
    public SpellEnableType spellEnableType; // Spell to unlock (if any)
    public bool noah;                       // If true, spawns the Guardian tower
    
    // ==================== REPLACEMENT SYSTEM ====================
    // Some skills disable other upgrades when unlocked.
    // This creates mutually exclusive upgrade paths.
    // "Default path" = player didn't take the swap tower
    // "Swapped path" = player took the swap tower for this element
    
    [Header("Replacement System - Default Tower Path")]
    [Tooltip("Upgrade to DISABLE when this skill is unlocked (default tower path)")]
    public TowerUpgradeType disablesUpgrade;
    [Tooltip("Upgrade to RE-ENABLE when this skill is locked/lost (default tower path)")]
    public TowerUpgradeType restoresUpgrade;
    
    [Header("Replacement System - Swapped Tower Path")]
    [Tooltip("Upgrade to DISABLE when this skill is unlocked (swapped tower path)")]
    public TowerUpgradeType disablesUpgrade2;
    [Tooltip("Upgrade to RE-ENABLE when this skill is locked/lost (swapped tower path)")]
    public TowerUpgradeType restoresUpgrade2;
    
    // ==================== SKILL LOSS SYSTEM ====================
    [Header("Skill Loss System")]
    [Tooltip("Which element branch this skill belongs to (for targeted skill loss)")]
    public ElementType skillElementType;
    [Tooltip("Globe nodes protect all skills below them from being lost")]
    public bool isGlobeNode;
    [Tooltip("Guardian node protects the ENTIRE tree from skill loss")]
    public bool isGuardianNode;
    
    /// <summary>
    /// Event fired when a swap is triggered. Listeners can react to tower swaps.
    /// </summary>
    public event Action<SwapType> EventRaised;
    
    /// <summary>
    /// Called when this skill is unlocked. Applies all effects.
    /// Handles the replacement system based on whether player took swap path.
    /// </summary>
    public void ApplyEffect()
    {
        // ===== DETERMINE WHICH PATH PLAYER IS ON =====
        // If this skill IS a swap, we're not on swap path yet (we're creating it)
        // If this skill is NOT a swap, check if the related swap was already unlocked
        bool tookSwapPath = swapType != SwapType.Null 
            ? false  // This IS the swap skill, so we're not "on" swap path yet
            : TowerUpgradeManager.instance.IsSwapUnlocked(GetRelatedSwapType());
        
        // ===== HANDLE UPGRADE DISABLING =====
        // Different upgrades get disabled based on which path was taken
        if (tookSwapPath)
        {
            // Player has the swapped tower - use swap path disables
            if (disablesUpgrade2 != TowerUpgradeType.Null)
            {
                TowerUpgradeManager.instance.LockUpgrade(disablesUpgrade2);
            }
        }
        else
        {
            // Player has default tower - use default path disables
            if (disablesUpgrade != TowerUpgradeType.Null)
            {
                TowerUpgradeManager.instance.LockUpgrade(disablesUpgrade);
            }
        }
        
        // ===== APPLY PRIMARY EFFECTS =====
        // Unlock the main tower upgrade
        TowerUpgradeManager.instance.UnlockUpgrade(function);
        
        // Unlock secondary upgrade if specified
        if (function2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.UnlockUpgrade(function2);
        }
        
        // Trigger tower swap if this is a swap skill
        if (swapType != SwapType.Null)
            Raise(swapType);
            
        // Enable spell button if this unlocks a spell
        if (spellEnableType != SpellEnableType.Null)
            EnableSpell(spellEnableType);
            
        // Spawn Guardian tower if this is the guardian skill
        if (noah)
            EnableNoah();
    }
    
    /// <summary>
    /// Called when this skill is locked/lost. Removes effects and restores disabled upgrades.
    /// </summary>
    public void RemoveEffect()
    {
        // Lock the upgrades this skill provided
        TowerUpgradeManager.instance.LockUpgrade(function);
        
        if (function2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.LockUpgrade(function2);
        }
        
        // ===== RESTORE DISABLED UPGRADES =====
        // Re-enable whatever this skill was blocking
        bool tookSwapPath = TowerUpgradeManager.instance.IsSwapUnlocked(GetRelatedSwapType());
        
        if (tookSwapPath)
        {
            // Restore swap path upgrade
            if (restoresUpgrade2 != TowerUpgradeType.Null)
            {
                TowerUpgradeManager.instance.UnlockUpgrade(restoresUpgrade2);
            }
        }
        else
        {
            // Restore default path upgrade
            if (restoresUpgrade != TowerUpgradeType.Null)
            {
                TowerUpgradeManager.instance.UnlockUpgrade(restoresUpgrade);
            }
        }
    }
    
    /// <summary>
    /// Maps element types to their corresponding swap types.
    /// Used to determine which tower path the player is on.
    /// </summary>
    private SwapType GetRelatedSwapType()
    {
        switch (skillElementType)
        {
            case ElementType.Physical: return SwapType.Spear;
            case ElementType.Magic: return SwapType.IceMage;
            case ElementType.Mechanic: return SwapType.BladeTower;
            case ElementType.Imaginary: return SwapType.Knight;
            default: return SwapType.Null;
        }
    }
    
    /// <summary>
    /// Triggers a tower swap. Fires event and tells TowerUpgradeManager to perform swap.
    /// </summary>
    public void Raise(SwapType type)
    {
        Debug.Log($"Raise called with SwapType: {type}");
        EventRaised?.Invoke(type);
        TowerUpgradeManager.instance.SwapTowers(type);
    }
    
    /// <summary>
    /// Enables a spell button in the UI.
    /// </summary>
    public void EnableSpell(SpellEnableType type)
    {
        SpellFunctionality.instance.EnableButton(type);
    }
    
    /// <summary>
    /// Spawns the Guardian tower at the player's castle.
    /// This also triggers the final Mega Wave.
    /// </summary>
    public void EnableNoah()
    {
        PlayerCastle.instance.SpawnGuardianTower();
    }
}