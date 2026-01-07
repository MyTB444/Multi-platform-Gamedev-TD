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
    
    [Header("Skill Loss System")]
    public ElementType skillElementType;
    public bool isGlobeNode;
    public bool isGuardianNode;
    
    public event Action<SwapType> EventRaised;
    
    public void ApplyEffect()
    {
        TowerUpgradeManager.instance.UnlockUpgrade(function);
        if(function2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.UnlockUpgrade(function2);
        }
        if(swapType != SwapType.Null)
            Raise(swapType);
        if(spellEnableType != SpellEnableType.Null)
            EnableSpell(spellEnableType);
        if(noah)
            EnableNoah();
    }
    
    public void RemoveEffect()
    {
        TowerUpgradeManager.instance.LockUpgrade(function);
        if (function2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.LockUpgrade(function2);
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