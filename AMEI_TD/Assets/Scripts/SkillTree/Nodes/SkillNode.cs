using System;
using System.Collections.Generic;
using UnityEngine;

public enum SwapType
{
    Null,
    Spear,
    Knight,
    IceMage,
    BladeTower
}

public enum SpellEnableType
{
    Null,
    Physical,
    Magical,
    Mechanical,
    Imaginary
}

/// <summary>
/// ScriptableObject representing a skill tree node. Contains unlock cost, prerequisites,
/// and effects to apply (tower upgrades, tower swaps, spell unlocks, etc.)
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill Tree/Skill Node")]
public class SkillNode : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public string skillCost;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public bool irreversible;

    [Header("Requirements")]
    public int skillPointCost;
    public List<SkillNode> prerequisites = new List<SkillNode>();

    [Header("Functionalities")]
    public TowerUpgradeType function;
    public TowerUpgradeType function2;
    public SwapType swapType;
    public SpellEnableType spellEnableType;
    public bool noah;
    
    [Header("Replacement System - Default Tower Path")]
    public TowerUpgradeType disablesUpgrade;
    public TowerUpgradeType restoresUpgrade;
    
    [Header("Replacement System - Swapped Tower Path")]
    public TowerUpgradeType disablesUpgrade2;
    public TowerUpgradeType restoresUpgrade2;
    
    [Header("Skill Loss System")]
    public ElementType skillElementType;
    public bool isGlobeNode;
    public bool isGuardianNode;
    
    public event Action<SwapType> EventRaised;
    
    public void ApplyEffect()
    {
        bool tookSwapPath = swapType != SwapType.Null 
            ? false
            : TowerUpgradeManager.instance.IsSwapUnlocked(GetRelatedSwapType());
        
        if (tookSwapPath)
        {
            if (disablesUpgrade2 != TowerUpgradeType.Null)
            {
                TowerUpgradeManager.instance.LockUpgrade(disablesUpgrade2);
            }
        }
        else
        {
            if (disablesUpgrade != TowerUpgradeType.Null)
            {
                TowerUpgradeManager.instance.LockUpgrade(disablesUpgrade);
            }
        }
        
        TowerUpgradeManager.instance.UnlockUpgrade(function);
        if (function2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.UnlockUpgrade(function2);
        }
        if (swapType != SwapType.Null)
            Raise(swapType);
        if (spellEnableType != SpellEnableType.Null)
            EnableSpell(spellEnableType);
        if (noah)
            EnableNoah();
    }
    
    public void RemoveEffect()
    {
        TowerUpgradeManager.instance.LockUpgrade(function);
        if (function2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.LockUpgrade(function2);
        }
        
        bool tookSwapPath = TowerUpgradeManager.instance.IsSwapUnlocked(GetRelatedSwapType());
        
        if (tookSwapPath)
        {
            if (restoresUpgrade2 != TowerUpgradeType.Null)
            {
                TowerUpgradeManager.instance.UnlockUpgrade(restoresUpgrade2);
            }
        }
        else
        {
            if (restoresUpgrade != TowerUpgradeType.Null)
            {
                TowerUpgradeManager.instance.UnlockUpgrade(restoresUpgrade);
            }
        }
    }
    
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
    
    public void Raise(SwapType type)
    {
        Debug.Log($"Raise called with SwapType: {type}");
        EventRaised?.Invoke(type);
        TowerUpgradeManager.instance.SwapTowers(type);
    }
    
    public void EnableSpell(SpellEnableType type)
    {
        SpellFunctionality.instance.EnableButton(type);
    }
    
    public void EnableNoah()
    {
        PlayerCastle.instance.SpawnGuardianTower();
    }
}